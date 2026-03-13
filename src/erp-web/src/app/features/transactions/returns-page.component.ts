import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { returnStatusOptions, returnTypeOptions } from '../../core/models/erp.enums';
import { InvoiceListItemDto, ReturnListItemDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

@Component({
  selector: 'erp-returns-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, PagerComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Returns"
        kicker="Inventory & Finance"
        subtitle="Create sales and purchase returns against original invoices so balances and stock are reversed correctly.">
      </erp-page-header>

      <section class="erp-split">
        <article class="erp-card">
          <div class="erp-section">
            <form [formGroup]="form" (ngSubmit)="save()" class="erp-grid">
              <div class="erp-form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Branch</mat-label>
                  <mat-select formControlName="branchId">
                    <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Return type</mat-label>
                  <mat-select formControlName="type" (selectionChange)="refreshInvoices()">
                    <mat-option *ngFor="let type of returnTypeOptions" [value]="type.value">{{ type.label }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Return date</mat-label>
                  <input matInput type="date" formControlName="returnDateUtc" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Source invoice</mat-label>
                  <mat-select formControlName="invoiceId" (selectionChange)="loadInvoiceLines($event.value)">
                    <mat-option *ngFor="let invoice of invoiceOptions" [value]="invoice.id">
                      {{ invoice.number }} - {{ invoice.counterpartyName }}
                    </mat-option>
                  </mat-select>
                </mat-form-field>
              </div>

              <mat-form-field appearance="outline">
                <mat-label>Reason</mat-label>
                <textarea matInput rows="3" formControlName="reason"></textarea>
              </mat-form-field>

              <div>
                <div class="erp-toolbar">
                  <strong>Return lines</strong>
                  <span class="spacer"></span>
                  <button mat-stroked-button type="button" (click)="addLine()">Add line</button>
                </div>

                <div formArrayName="lines" class="erp-grid">
                  <div class="erp-card erp-section" *ngFor="let line of lines.controls; let index = index" [formGroupName]="index">
                    <div class="erp-line-item-grid">
                      <mat-form-field appearance="outline">
                        <mat-label>Product</mat-label>
                        <mat-select formControlName="productId">
                          <mat-option *ngFor="let product of products" [value]="product.id">{{ product.name }}</mat-option>
                        </mat-select>
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Qty</mat-label>
                        <input matInput type="number" formControlName="quantity" />
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Unit price</mat-label>
                        <input matInput type="number" formControlName="unitPrice" />
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Reason</mat-label>
                        <input matInput formControlName="reason" />
                      </mat-form-field>

                      <div class="erp-pill">{{ lineTotal(index) | currency:'USD':'symbol':'1.0-2' }}</div>
                      <button mat-button type="button" (click)="removeLine(index)">Remove</button>
                    </div>
                  </div>
                </div>
              </div>

              <div class="erp-toolbar">
                <span class="erp-pill success">Return total {{ returnTotal | currency:'USD':'symbol':'1.0-2' }}</span>
                <span class="spacer"></span>
                <button mat-flat-button color="primary" type="submit">Post return</button>
              </div>
            </form>
          </div>
        </article>

        <article class="erp-card">
          <div class="erp-section">
            <table class="erp-table" *ngIf="paged.items.length; else emptyState">
              <thead>
                <tr>
                  <th>Number</th>
                  <th>Branch</th>
                  <th>Type</th>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of paged.items">
                  <td>{{ item.number }}</td>
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.type | optionLabel:returnTypeOptions }}</td>
                  <td>{{ item.returnDateUtc | date:'mediumDate' }}</td>
                  <td>{{ item.status | optionLabel:returnStatusOptions }}</td>
                  <td>{{ item.totalAmount | currency:'USD':'symbol':'1.0-0' }}</td>
                </tr>
              </tbody>
            </table>

            <ng-template #emptyState>
              <div class="erp-empty">No returns have been posted yet.</div>
            </ng-template>

            <erp-pager
              [pageNumber]="paged.pageNumber"
              [totalPages]="paged.totalPages"
              (previous)="loadList(paged.pageNumber - 1)"
              (next)="loadList(paged.pageNumber + 1)">
            </erp-pager>
          </div>
        </article>
      </section>
    </div>
  `
})
export class ReturnsPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly returnTypeOptions = returnTypeOptions;
  readonly returnStatusOptions = returnStatusOptions;

  branches: LookupOption[] = [];
  products: LookupOption[] = [];
  invoiceOptions: InvoiceListItemDto[] = [];
  paged: PagedResult<ReturnListItemDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  };

  readonly form = this.fb.group({
    branchId: ['', [Validators.required]],
    type: [1, [Validators.required]],
    returnDateUtc: [toDateInputValue(new Date()), [Validators.required]],
    invoiceId: [null as string | null],
    reason: [''],
    lines: this.fb.array([])
  });

  constructor() {
    this.referenceData.getBranches().subscribe({ next: (branches) => this.branches = branches });
    this.referenceData.getProducts().subscribe({ next: (products) => this.products = products });
    this.addLine();
    this.refreshInvoices();
    this.loadList(1);
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  get returnTotal(): number {
    return this.lines.controls.reduce((sum, _, index) => sum + this.lineTotal(index), 0);
  }

  addLine(): void {
    this.lines.push(this.fb.group({
      productId: ['', [Validators.required]],
      quantity: [1, [Validators.required]],
      unitPrice: [0, [Validators.required]],
      reason: ['']
    }));
  }

  removeLine(index: number): void {
    if (this.lines.length === 1) {
      return;
    }

    this.lines.removeAt(index);
  }

  lineTotal(index: number): number {
    const line = this.lines.at(index).value;
    return Number(line.quantity ?? 0) * Number(line.unitPrice ?? 0);
  }

  refreshInvoices(): void {
    const endpoint = this.form.value.type === 1 ? 'invoices/sales' : 'invoices/purchase';
    this.api.get<PagedResult<InvoiceListItemDto>>(endpoint, {
      pageNumber: 1,
      pageSize: 100
    }).subscribe({
      next: (result) => {
        this.invoiceOptions = result.items;
      }
    });
  }

  loadInvoiceLines(invoiceId: string | null): void {
    const invoice = this.invoiceOptions.find((item) => item.id === invoiceId);
    if (!invoice) {
      return;
    }

    this.form.patchValue({
      branchId: invoice.branchId
    });

    const endpoint = this.form.value.type === 1 ? `invoices/sales/${invoiceId}` : `invoices/purchase/${invoiceId}`;
    this.api.get<any>(endpoint).subscribe({
      next: (details) => {
        this.lines.clear();
        details.lines.forEach((line: any) => {
          this.lines.push(this.fb.group({
            productId: [line.productId, [Validators.required]],
            quantity: [line.quantity, [Validators.required]],
            unitPrice: [line.unitPrice, [Validators.required]],
            reason: ['']
          }));
        });
      }
    });
  }

  loadList(pageNumber = 1): void {
    this.api.get<PagedResult<ReturnListItemDto>>('returns', {
      pageNumber,
      pageSize: 10
    }).subscribe({
      next: (result) => {
        this.paged = result;
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to load returns.');
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.lines.length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = {
      branchId: value.branchId,
      type: value.type,
      returnDateUtc: toUtcIso(value.returnDateUtc ?? ''),
      salesInvoiceId: value.type === 1 ? value.invoiceId : null,
      purchaseInvoiceId: value.type === 2 ? value.invoiceId : null,
      reason: value.reason,
      lines: this.lines.getRawValue().map((line) => ({
        productId: line.productId,
        quantity: Number(line.quantity ?? 0),
        unitPrice: Number(line.unitPrice ?? 0),
        reason: line.reason
      }))
    };

    this.api.post<string>('returns', request).subscribe({
      next: () => {
        this.notifications.success('Return posted successfully.');
        this.form.patchValue({
          returnDateUtc: toDateInputValue(new Date()),
          invoiceId: null,
          reason: ''
        });
        this.lines.clear();
        this.addLine();
        this.loadList(this.paged.pageNumber);
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to post the return.');
      }
    });
  }
}
