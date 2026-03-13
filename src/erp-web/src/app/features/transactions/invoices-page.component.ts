import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { invoiceStatusOptions } from '../../core/models/erp.enums';
import { InvoiceListItemDto, PurchaseOrderDto, SalesOrderDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

type InvoiceMode = 'purchase' | 'sales';

@Component({
  selector: 'erp-invoices-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, PagerComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Invoices"
        kicker="Finance"
        subtitle="Create purchase and sales invoices, optionally derive them from approved orders, and track outstanding balances by branch and counterparty.">
      </erp-page-header>

      <mat-tab-group [selectedIndex]="mode === 'purchase' ? 0 : 1" (selectedIndexChange)="setMode($event === 0 ? 'purchase' : 'sales')">
        <mat-tab label="Purchase invoices"></mat-tab>
        <mat-tab label="Sales invoices"></mat-tab>
      </mat-tab-group>

      <section class="erp-split" style="margin-top: 16px;">
        <article class="erp-card">
          <div class="erp-section">
            <h2 style="margin-top: 0;">New {{ mode === 'purchase' ? 'purchase' : 'sales' }} invoice</h2>
            <form [formGroup]="form" (ngSubmit)="save()" class="erp-grid">
              <div class="erp-form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Branch</mat-label>
                  <mat-select formControlName="branchId">
                    <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>{{ mode === 'purchase' ? 'Supplier' : 'Customer' }}</mat-label>
                  <mat-select formControlName="counterpartyId">
                    <mat-option *ngFor="let item of currentCounterparties" [value]="item.id">{{ item.name }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>{{ mode === 'purchase' ? 'Purchase order' : 'Sales order' }}</mat-label>
                  <mat-select formControlName="orderId" (selectionChange)="loadOrder($event.value)">
                    <mat-option [value]="null">Manual invoice</mat-option>
                    <mat-option *ngFor="let order of orderOptions" [value]="order.id">{{ order.label }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Invoice date</mat-label>
                  <input matInput type="date" formControlName="invoiceDateUtc" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Due date</mat-label>
                  <input matInput type="date" formControlName="dueDateUtc" />
                </mat-form-field>
              </div>

              <mat-form-field appearance="outline">
                <mat-label>Notes</mat-label>
                <textarea matInput rows="3" formControlName="notes"></textarea>
              </mat-form-field>

              <div>
                <div class="erp-toolbar">
                  <strong>Line items</strong>
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
                        <mat-label>Tax %</mat-label>
                        <input matInput type="number" formControlName="taxPercent" />
                      </mat-form-field>

                      <div class="erp-pill">{{ lineTotal(index) | currency:'USD':'symbol':'1.0-2' }}</div>
                      <button mat-button type="button" (click)="removeLine(index)">Remove</button>
                    </div>
                  </div>
                </div>
              </div>

              <div class="erp-toolbar">
                <span class="erp-pill success">Invoice total {{ invoiceTotal | currency:'USD':'symbol':'1.0-2' }}</span>
                <span class="spacer"></span>
                <button mat-flat-button color="primary" type="submit">Create invoice</button>
              </div>
            </form>
          </div>
        </article>

        <article class="erp-card">
          <div class="erp-section">
            <div class="erp-toolbar">
              <mat-form-field appearance="outline">
                <mat-label>Search</mat-label>
                <input matInput [formControl]="searchControl" (keyup.enter)="loadList(1)" />
              </mat-form-field>
              <button mat-stroked-button type="button" (click)="loadList(1)">Filter</button>
            </div>

            <table class="erp-table" *ngIf="paged.items.length; else emptyState">
              <thead>
                <tr>
                  <th>Number</th>
                  <th>Branch</th>
                  <th>{{ mode === 'purchase' ? 'Supplier' : 'Customer' }}</th>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Total</th>
                  <th>Outstanding</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of paged.items">
                  <td>{{ item.number }}</td>
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.counterpartyName }}</td>
                  <td>{{ item.invoiceDateUtc | date:'mediumDate' }}</td>
                  <td>{{ item.status | optionLabel:invoiceStatusOptions }}</td>
                  <td>{{ item.totalAmount | currency:'USD':'symbol':'1.0-0' }}</td>
                  <td>{{ item.outstandingAmount | currency:'USD':'symbol':'1.0-0' }}</td>
                </tr>
              </tbody>
            </table>

            <ng-template #emptyState>
              <div class="erp-empty">No invoices matched the current mode and filters.</div>
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
export class InvoicesPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly invoiceStatusOptions = invoiceStatusOptions;
  readonly searchControl = this.fb.control('');

  mode: InvoiceMode = 'purchase';
  branches: LookupOption[] = [];
  customers: LookupOption[] = [];
  suppliers: LookupOption[] = [];
  products: LookupOption[] = [];
  orderOptions: Array<{ id: string; label: string }> = [];
  paged: PagedResult<InvoiceListItemDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  };

  readonly form = this.fb.group({
    branchId: ['', [Validators.required]],
    counterpartyId: ['', [Validators.required]],
    orderId: [null as string | null],
    invoiceDateUtc: [toDateInputValue(new Date()), [Validators.required]],
    dueDateUtc: [toDateInputValue(new Date()), [Validators.required]],
    notes: [''],
    lines: this.fb.array([])
  });

  constructor() {
    forkJoin({
      branches: this.referenceData.getBranches(),
      customers: this.referenceData.getCustomers(),
      suppliers: this.referenceData.getSuppliers(),
      products: this.referenceData.getProducts()
    }).subscribe({
      next: ({ branches, customers, suppliers, products }) => {
        this.branches = branches;
        this.customers = customers;
        this.suppliers = suppliers;
        this.products = products;
      }
    });

    this.addLine();
    this.refreshOrderOptions();
    this.loadList(1);
  }

  get currentCounterparties(): LookupOption[] {
    return this.mode === 'purchase' ? this.suppliers : this.customers;
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  get invoiceTotal(): number {
    return this.lines.controls.reduce((sum, _, index) => sum + this.lineTotal(index), 0);
  }

  setMode(mode: InvoiceMode): void {
    this.mode = mode;
    this.resetForm();
    this.refreshOrderOptions();
    this.loadList(1);
  }

  addLine(): void {
    this.lines.push(this.fb.group({
      productId: ['', [Validators.required]],
      quantity: [1, [Validators.required]],
      unitPrice: [0, [Validators.required]],
      taxPercent: [0],
      orderLineId: [null as string | null]
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
    const quantity = Number(line.quantity ?? 0);
    const unitPrice = Number(line.unitPrice ?? 0);
    const tax = Number(line.taxPercent ?? 0);
    return quantity * unitPrice * (1 + tax / 100);
  }

  refreshOrderOptions(): void {
    const endpoint = this.mode === 'purchase' ? 'purchase-orders' : 'sales-orders';
    this.api.get<PagedResult<any>>(endpoint, { pageNumber: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        this.orderOptions = result.items
          .filter((item: any) => item.status === 3 || item.status === 4)
          .map((item: any) => ({
            id: item.id,
            label: `${item.number} - ${item.branchName}`
          }));
      }
    });
  }

  loadOrder(orderId: string | null): void {
    if (!orderId) {
      return;
    }

    const endpoint = this.mode === 'purchase' ? 'purchase-orders' : 'sales-orders';
    this.api.get<PurchaseOrderDto | SalesOrderDto>(`${endpoint}/${orderId}`).subscribe({
      next: (order) => {
        this.form.patchValue({
          branchId: order.branchId,
          counterpartyId: this.mode === 'purchase' ? (order as PurchaseOrderDto).supplierId : (order as SalesOrderDto).customerId
        });
        this.lines.clear();
        order.lines.forEach((line: any) => {
          this.lines.push(this.fb.group({
            productId: [line.productId, [Validators.required]],
            quantity: [this.mode === 'purchase'
              ? line.orderedQuantity - line.receivedQuantity
              : line.orderedQuantity - line.deliveredQuantity, [Validators.required]],
            unitPrice: [line.unitPrice, [Validators.required]],
            taxPercent: [line.taxPercent],
            orderLineId: [line.id]
          }));
        });
      }
    });
  }

  loadList(pageNumber = 1): void {
    const endpoint = this.mode === 'purchase' ? 'invoices/purchase' : 'invoices/sales';
    this.api.get<PagedResult<InvoiceListItemDto>>(endpoint, {
      pageNumber,
      pageSize: 10,
      search: this.searchControl.value
    }).subscribe({
      next: (result) => {
        this.paged = result;
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to load invoices.');
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
      invoiceDateUtc: toUtcIso(value.invoiceDateUtc ?? ''),
      dueDateUtc: toUtcIso(value.dueDateUtc ?? ''),
      notes: value.notes,
      ...(this.mode === 'purchase'
        ? {
            supplierId: value.counterpartyId,
            purchaseOrderId: value.orderId,
            lines: this.lines.getRawValue().map((line) => ({
              productId: line.productId,
              quantity: Number(line.quantity ?? 0),
              unitPrice: Number(line.unitPrice ?? 0),
              taxPercent: Number(line.taxPercent ?? 0),
              purchaseOrderLineId: line.orderLineId
            }))
          }
        : {
            customerId: value.counterpartyId,
            salesOrderId: value.orderId,
            lines: this.lines.getRawValue().map((line) => ({
              productId: line.productId,
              quantity: Number(line.quantity ?? 0),
              unitPrice: Number(line.unitPrice ?? 0),
              taxPercent: Number(line.taxPercent ?? 0),
              salesOrderLineId: line.orderLineId
            }))
          })
    };

    const endpoint = this.mode === 'purchase' ? 'invoices/purchase' : 'invoices/sales';
    this.api.post<string>(endpoint, request).subscribe({
      next: () => {
        this.notifications.success('Invoice created successfully.');
        this.resetForm();
        this.loadList(this.paged.pageNumber);
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to create the invoice.');
      }
    });
  }

  private resetForm(): void {
    this.form.reset({
      branchId: '',
      counterpartyId: '',
      orderId: null,
      invoiceDateUtc: toDateInputValue(new Date()),
      dueDateUtc: toDateInputValue(new Date()),
      notes: ''
    });
    this.lines.clear();
    this.addLine();
  }
}
