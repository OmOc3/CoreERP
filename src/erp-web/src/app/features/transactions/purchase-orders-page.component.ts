import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { purchaseOrderStatusOptions } from '../../core/models/erp.enums';
import { PurchaseOrderDto, PurchaseOrderListItemDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

@Component({
  selector: 'erp-purchase-orders-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, PagerComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Purchase orders"
        kicker="Purchasing"
        subtitle="Create draft purchase orders, edit line pricing, submit them into approval, and monitor branch supplier commitments.">
      </erp-page-header>

      <section class="erp-split">
        <article class="erp-card">
          <div class="erp-section">
            <h2 style="margin-top: 0;">{{ selectedId ? 'Edit purchase order' : 'New purchase order' }}</h2>
            <form [formGroup]="form" (ngSubmit)="save()" class="erp-grid">
              <div class="erp-form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Branch</mat-label>
                  <mat-select formControlName="branchId">
                    <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Supplier</mat-label>
                  <mat-select formControlName="supplierId">
                    <mat-option *ngFor="let supplier of suppliers" [value]="supplier.id">{{ supplier.name }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Order date</mat-label>
                  <input matInput type="date" formControlName="orderDateUtc" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Expected date</mat-label>
                  <input matInput type="date" formControlName="expectedDateUtc" />
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
                        <mat-label>Discount %</mat-label>
                        <input matInput type="number" formControlName="discountPercent" />
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Tax %</mat-label>
                        <input matInput type="number" formControlName="taxPercent" />
                      </mat-form-field>

                      <button mat-button type="button" (click)="removeLine(index)">Remove</button>
                    </div>

                    <mat-form-field appearance="outline" style="width: 100%;">
                      <mat-label>Description</mat-label>
                      <input matInput formControlName="description" />
                    </mat-form-field>

                    <div class="erp-toolbar" style="margin-bottom: 0;">
                      <span class="spacer"></span>
                      <span class="erp-pill">{{ lineTotal(index) | currency:'USD':'symbol':'1.0-2' }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="erp-toolbar">
                <span class="erp-pill success">Order total {{ orderTotal | currency:'USD':'symbol':'1.0-2' }}</span>
                <span class="spacer"></span>
                <div class="erp-actions">
                  <button mat-flat-button color="primary" type="submit">{{ selectedId ? 'Save changes' : 'Create order' }}</button>
                  <button mat-stroked-button type="button" (click)="startNew()">Clear</button>
                </div>
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

              <mat-form-field appearance="outline">
                <mat-label>Status</mat-label>
                <mat-select [formControl]="statusControl">
                  <mat-option [value]="null">All statuses</mat-option>
                  <mat-option *ngFor="let status of purchaseOrderStatusOptions" [value]="status.value">{{ status.label }}</mat-option>
                </mat-select>
              </mat-form-field>

              <button mat-stroked-button type="button" (click)="loadList(1)">Filter</button>
            </div>

            <table class="erp-table" *ngIf="paged.items.length; else emptyState">
              <thead>
                <tr>
                  <th>Number</th>
                  <th>Branch</th>
                  <th>Supplier</th>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Total</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of paged.items">
                  <td>{{ item.number }}</td>
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.supplierName }}</td>
                  <td>{{ item.orderDateUtc | date:'mediumDate' }}</td>
                  <td>{{ item.status | optionLabel:purchaseOrderStatusOptions }}</td>
                  <td>{{ item.totalAmount | currency:'USD':'symbol':'1.0-0' }}</td>
                  <td>
                    <div class="erp-actions">
                      <button mat-stroked-button type="button" (click)="edit(item.id)">Edit</button>
                      <button mat-flat-button color="primary" type="button" (click)="submitForApproval(item.id)" [disabled]="item.status !== 1">Submit</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>

            <ng-template #emptyState>
              <div class="erp-empty">No purchase orders matched the current filters.</div>
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
export class PurchaseOrdersPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly purchaseOrderStatusOptions = purchaseOrderStatusOptions;
  readonly searchControl = this.fb.control('');
  readonly statusControl = this.fb.control<number | null>(null);

  selectedId: string | null = null;
  branches: LookupOption[] = [];
  suppliers: LookupOption[] = [];
  products: LookupOption[] = [];
  paged: PagedResult<PurchaseOrderListItemDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  };

  readonly form = this.fb.group({
    branchId: ['', [Validators.required]],
    supplierId: ['', [Validators.required]],
    orderDateUtc: [toDateInputValue(new Date()), [Validators.required]],
    expectedDateUtc: [''],
    notes: [''],
    lines: this.fb.array([])
  });

  constructor() {
    forkJoin({
      branches: this.referenceData.getBranches(),
      suppliers: this.referenceData.getSuppliers(),
      products: this.referenceData.getProducts()
    }).subscribe({
      next: ({ branches, suppliers, products }) => {
        this.branches = branches;
        this.suppliers = suppliers;
        this.products = products;
      }
    });

    this.addLine();
    this.loadList(1);
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  get orderTotal(): number {
    return this.lines.controls.reduce((sum, _, index) => sum + this.lineTotal(index), 0);
  }

  addLine(): void {
    this.lines.push(this.fb.group({
      productId: ['', [Validators.required]],
      quantity: [1, [Validators.required]],
      unitPrice: [0, [Validators.required]],
      discountPercent: [0],
      taxPercent: [0],
      description: ['']
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
    const discount = Number(line.discountPercent ?? 0);
    const tax = Number(line.taxPercent ?? 0);
    const net = quantity * unitPrice * (1 - discount / 100);
    return net * (1 + tax / 100);
  }

  loadList(pageNumber = 1): void {
    this.api.get<PagedResult<PurchaseOrderListItemDto>>('purchase-orders', {
      pageNumber,
      pageSize: 10,
      search: this.searchControl.value,
      status: this.statusControl.value
    }).subscribe({
      next: (result) => {
        this.paged = result;
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to load purchase orders.');
      }
    });
  }

  startNew(): void {
    this.selectedId = null;
    this.form.reset({
      branchId: '',
      supplierId: '',
      orderDateUtc: toDateInputValue(new Date()),
      expectedDateUtc: '',
      notes: ''
    });
    this.lines.clear();
    this.addLine();
  }

  edit(id: string): void {
    this.api.get<PurchaseOrderDto>(`purchase-orders/${id}`).subscribe({
      next: (order) => {
        this.selectedId = id;
        this.form.patchValue({
          branchId: order.branchId,
          supplierId: order.supplierId,
          orderDateUtc: toDateInputValue(order.orderDateUtc),
          expectedDateUtc: toDateInputValue(order.expectedDateUtc ?? ''),
          notes: order.notes ?? ''
        });
        this.lines.clear();
        order.lines.forEach((line) => {
          this.lines.push(this.fb.group({
            productId: [line.productId, [Validators.required]],
            quantity: [line.orderedQuantity, [Validators.required]],
            unitPrice: [line.unitPrice, [Validators.required]],
            discountPercent: [line.discountPercent],
            taxPercent: [line.taxPercent],
            description: [line.description ?? '']
          }));
        });
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to load the purchase order.');
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
      supplierId: value.supplierId,
      orderDateUtc: toUtcIso(value.orderDateUtc ?? ''),
      expectedDateUtc: toUtcIso(value.expectedDateUtc ?? ''),
      notes: value.notes,
      lines: this.lines.getRawValue().map((line) => ({
        productId: line.productId,
        quantity: Number(line.quantity ?? 0),
        unitPrice: Number(line.unitPrice ?? 0),
        discountPercent: Number(line.discountPercent ?? 0),
        taxPercent: Number(line.taxPercent ?? 0),
        description: line.description
      }))
    };

    if (this.selectedId) {
      this.api.put<void>(`purchase-orders/${this.selectedId}`, request).subscribe({
        next: () => {
          this.notifications.success('Purchase order saved successfully.');
          this.startNew();
          this.loadList(this.paged.pageNumber);
        },
        error: (error: any) => {
          this.notifications.error(error?.error?.detail ?? 'Unable to save the purchase order.');
        }
      });
      return;
    }

    this.api.post<string>('purchase-orders', request).subscribe({
      next: () => {
        this.notifications.success('Purchase order saved successfully.');
        this.startNew();
        this.loadList(this.paged.pageNumber);
      },
      error: (error: any) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to save the purchase order.');
      }
    });
  }

  submitForApproval(id: string): void {
    this.api.post<void>(`purchase-orders/${id}/submit`, {}).subscribe({
      next: () => {
        this.notifications.success('Purchase order submitted for approval.');
        this.loadList(this.paged.pageNumber);
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to submit the purchase order.');
      }
    });
  }
}
