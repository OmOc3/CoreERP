import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { paymentStatusOptions, paymentTypeOptions } from '../../core/models/erp.enums';
import { InvoiceListItemDto, PaymentDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

@Component({
  selector: 'erp-payments-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, PagerComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Payments"
        kicker="Finance"
        subtitle="Register customer collections and supplier payments against outstanding invoices and branch ledgers.">
      </erp-page-header>

      <section class="erp-split">
        <article class="erp-card">
          <div class="erp-section">
            <form [formGroup]="form" (ngSubmit)="save()" class="erp-form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Branch</mat-label>
                <mat-select formControlName="branchId">
                  <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Payment type</mat-label>
                <mat-select formControlName="type" (selectionChange)="refreshInvoiceOptions()">
                  <mat-option *ngFor="let type of paymentTypeOptions" [value]="type.value">{{ type.label }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Date</mat-label>
                <input matInput type="date" formControlName="paymentDateUtc" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Amount</mat-label>
                <input matInput type="number" formControlName="amount" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Method</mat-label>
                <input matInput formControlName="method" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Reference number</mat-label>
                <input matInput formControlName="referenceNumber" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>{{ form.value.type === 1 ? 'Customer' : 'Supplier' }}</mat-label>
                <mat-select formControlName="counterpartyId" (selectionChange)="refreshInvoiceOptions()">
                  <mat-option *ngFor="let item of currentCounterparties" [value]="item.id">{{ item.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Invoice</mat-label>
                <mat-select formControlName="invoiceId">
                  <mat-option [value]="null">Unapplied payment</mat-option>
                  <mat-option *ngFor="let invoice of invoiceOptions" [value]="invoice.id">
                    {{ invoice.number }} - {{ invoice.outstandingAmount | currency:'USD':'symbol':'1.0-0' }}
                  </mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Notes</mat-label>
                <textarea matInput rows="3" formControlName="notes"></textarea>
              </mat-form-field>

              <button mat-flat-button color="primary" type="submit">Post payment</button>
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
                  <th>Counterparty</th>
                  <th>Invoice</th>
                  <th>Amount</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of paged.items">
                  <td>{{ item.number }}</td>
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.type | optionLabel:paymentTypeOptions }}</td>
                  <td>{{ item.paymentDateUtc | date:'mediumDate' }}</td>
                  <td>{{ item.counterpartyName || 'N/A' }}</td>
                  <td>{{ item.invoiceNumber || 'Unapplied' }}</td>
                  <td>{{ item.amount | currency:'USD':'symbol':'1.0-0' }}</td>
                  <td>{{ item.status | optionLabel:paymentStatusOptions }}</td>
                </tr>
              </tbody>
            </table>

            <ng-template #emptyState>
              <div class="erp-empty">No payments have been posted yet.</div>
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
export class PaymentsPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly paymentTypeOptions = paymentTypeOptions;
  readonly paymentStatusOptions = paymentStatusOptions;

  branches: LookupOption[] = [];
  customers: LookupOption[] = [];
  suppliers: LookupOption[] = [];
  invoiceOptions: InvoiceListItemDto[] = [];
  paged: PagedResult<PaymentDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  };

  readonly form = this.fb.group({
    branchId: ['', [Validators.required]],
    type: [1, [Validators.required]],
    paymentDateUtc: [toDateInputValue(new Date()), [Validators.required]],
    amount: [0, [Validators.required]],
    method: ['Bank transfer', [Validators.required]],
    referenceNumber: [''],
    counterpartyId: [''],
    invoiceId: [null as string | null],
    notes: ['']
  });

  constructor() {
    forkJoin({
      branches: this.referenceData.getBranches(),
      customers: this.referenceData.getCustomers(),
      suppliers: this.referenceData.getSuppliers()
    }).subscribe({
      next: ({ branches, customers, suppliers }) => {
        this.branches = branches;
        this.customers = customers;
        this.suppliers = suppliers;
      }
    });

    this.loadList(1);
  }

  get currentCounterparties(): LookupOption[] {
    return this.form.value.type === 1 ? this.customers : this.suppliers;
  }

  refreshInvoiceOptions(): void {
    const counterpartyId = this.form.value.counterpartyId;
    if (!counterpartyId) {
      this.invoiceOptions = [];
      return;
    }

    const endpoint = this.form.value.type === 1 ? 'invoices/sales' : 'invoices/purchase';
    this.api.get<PagedResult<InvoiceListItemDto>>(endpoint, {
      pageNumber: 1,
      pageSize: 100,
      counterpartyId
    }).subscribe({
      next: (result) => {
        this.invoiceOptions = result.items.filter((item) => item.outstandingAmount > 0);
      }
    });
  }

  loadList(pageNumber = 1): void {
    this.api.get<PagedResult<PaymentDto>>('payments', {
      pageNumber,
      pageSize: 10
    }).subscribe({
      next: (result) => {
        this.paged = result;
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to load payments.');
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = value.type === 1
      ? {
          branchId: value.branchId,
          type: value.type,
          paymentDateUtc: toUtcIso(value.paymentDateUtc ?? ''),
          amount: Number(value.amount ?? 0),
          method: value.method,
          referenceNumber: value.referenceNumber,
          customerId: value.counterpartyId,
          salesInvoiceId: value.invoiceId,
          notes: value.notes
        }
      : {
          branchId: value.branchId,
          type: value.type,
          paymentDateUtc: toUtcIso(value.paymentDateUtc ?? ''),
          amount: Number(value.amount ?? 0),
          method: value.method,
          referenceNumber: value.referenceNumber,
          supplierId: value.counterpartyId,
          purchaseInvoiceId: value.invoiceId,
          notes: value.notes
        };

    this.api.post<string>('payments', request).subscribe({
      next: () => {
        this.notifications.success('Payment posted successfully.');
        this.form.patchValue({
          paymentDateUtc: toDateInputValue(new Date()),
          amount: 0,
          referenceNumber: '',
          invoiceId: null,
          notes: ''
        });
        this.invoiceOptions = [];
        this.loadList(this.paged.pageNumber);
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to post the payment.');
      }
    });
  }
}
