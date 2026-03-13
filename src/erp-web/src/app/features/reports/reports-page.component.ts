import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { LookupOption } from '../../core/models/common.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { ErpMaterialModule } from '../../shared/material';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

interface ReportDescriptor {
  key: string;
  label: string;
  endpoint: string;
  filters: Array<'branch' | 'customer' | 'supplier' | 'product' | 'dateRange'>;
}

@Component({
  selector: 'erp-reports-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Reports"
        kicker="Analytics"
        subtitle="Run operational and financial reports, filter by branch or counterparty, and export the result set to Excel or PDF.">
      </erp-page-header>

      <section class="erp-card erp-section">
        <form [formGroup]="form" class="erp-grid" (ngSubmit)="runReport()">
          <div class="erp-form-grid">
            <mat-form-field appearance="outline">
              <mat-label>Report</mat-label>
              <mat-select formControlName="reportKey" (selectionChange)="updateReportSelection()">
                <mat-option *ngFor="let report of reports" [value]="report.key">{{ report.label }}</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="supports('branch')">
              <mat-label>Branch</mat-label>
              <mat-select formControlName="branchId">
                <mat-option [value]="null">All branches</mat-option>
                <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="supports('customer')">
              <mat-label>Customer</mat-label>
              <mat-select formControlName="customerId">
                <mat-option [value]="null">All customers</mat-option>
                <mat-option *ngFor="let customer of customers" [value]="customer.id">{{ customer.name }}</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="supports('supplier')">
              <mat-label>Supplier</mat-label>
              <mat-select formControlName="supplierId">
                <mat-option [value]="null">All suppliers</mat-option>
                <mat-option *ngFor="let supplier of suppliers" [value]="supplier.id">{{ supplier.name }}</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="supports('product')">
              <mat-label>Product</mat-label>
              <mat-select formControlName="productId">
                <mat-option [value]="null">All products</mat-option>
                <mat-option *ngFor="let product of products" [value]="product.id">{{ product.name }}</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="supports('dateRange')">
              <mat-label>From</mat-label>
              <input matInput type="date" formControlName="dateFromUtc" />
            </mat-form-field>

            <mat-form-field appearance="outline" *ngIf="supports('dateRange')">
              <mat-label>To</mat-label>
              <input matInput type="date" formControlName="dateToUtc" />
            </mat-form-field>
          </div>

          <div class="erp-actions">
            <button mat-flat-button color="primary" type="submit">Run report</button>
            <button mat-stroked-button type="button" (click)="export('excel')">Export Excel</button>
            <button mat-stroked-button type="button" (click)="export('pdf')">Export PDF</button>
          </div>
        </form>
      </section>

      <section class="erp-card erp-section" style="margin-top: 16px;">
        <table class="erp-table" *ngIf="rows.length; else emptyState">
          <thead>
            <tr>
              <th *ngFor="let column of columns">{{ prettifyColumn(column) }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of rows">
              <td *ngFor="let column of columns">{{ row[column] }}</td>
            </tr>
          </tbody>
        </table>

        <ng-template #emptyState>
          <div class="erp-empty">Run a report to see filtered results here.</div>
        </ng-template>
      </section>
    </div>
  `
})
export class ReportsPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly reports: ReportDescriptor[] = [
    { key: 'sales', label: 'Sales summary', endpoint: 'sales-summary', filters: ['branch', 'customer', 'dateRange'] },
    { key: 'purchases', label: 'Purchase summary', endpoint: 'purchase-summary', filters: ['branch', 'supplier', 'dateRange'] },
    { key: 'inventoryValuation', label: 'Inventory valuation', endpoint: 'inventory-valuation', filters: ['branch', 'product'] },
    { key: 'stockMovement', label: 'Stock movement', endpoint: 'stock-movement', filters: ['branch', 'product', 'dateRange'] },
    { key: 'lowStock', label: 'Low stock', endpoint: 'low-stock', filters: ['branch', 'product'] },
    { key: 'receivables', label: 'Receivables', endpoint: 'receivables', filters: ['branch', 'customer'] },
    { key: 'payables', label: 'Payables', endpoint: 'payables', filters: ['branch', 'supplier'] }
  ];

  branches: LookupOption[] = [];
  customers: LookupOption[] = [];
  suppliers: LookupOption[] = [];
  products: LookupOption[] = [];
  rows: Array<Record<string, string | number>> = [];
  columns: string[] = [];
  selectedReport = this.reports[0];

  readonly form = this.fb.group({
    reportKey: [this.reports[0].key],
    branchId: [null as string | null],
    customerId: [null as string | null],
    supplierId: [null as string | null],
    productId: [null as string | null],
    dateFromUtc: [toDateInputValue(new Date(Date.now() - 29 * 24 * 60 * 60 * 1000))],
    dateToUtc: [toDateInputValue(new Date())]
  });

  constructor() {
    this.referenceData.getBranches().subscribe({ next: (items) => this.branches = items });
    this.referenceData.getCustomers().subscribe({ next: (items) => this.customers = items });
    this.referenceData.getSuppliers().subscribe({ next: (items) => this.suppliers = items });
    this.referenceData.getProducts().subscribe({ next: (items) => this.products = items });
  }

  supports(filter: ReportDescriptor['filters'][number]): boolean {
    return this.selectedReport.filters.includes(filter);
  }

  updateReportSelection(): void {
    this.selectedReport = this.reports.find((report) => report.key === this.form.value.reportKey) ?? this.reports[0];
  }

  runReport(): void {
    this.updateReportSelection();
    this.api.get<Array<Record<string, string | number>>>(`reports/${this.selectedReport.endpoint}`, this.buildQuery()).subscribe({
      next: (rows) => {
        this.rows = rows;
        this.columns = rows.length ? Object.keys(rows[0]) : [];
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to run the selected report.');
      }
    });
  }

  export(format: 'excel' | 'pdf'): void {
    this.updateReportSelection();
    this.api.download(`reports/${this.selectedReport.endpoint}/${format}`, this.buildQuery()).subscribe({
      next: (blob) => {
        const fileName = `${this.selectedReport.endpoint}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? `Unable to export the report as ${format.toUpperCase()}.`);
      }
    });
  }

  prettifyColumn(column: string): string {
    return column
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/Utc/g, 'UTC')
      .replace(/^./, (value) => value.toUpperCase());
  }

  private buildQuery(): Record<string, string | null> {
    const value = this.form.getRawValue();
    return {
      branchId: value.branchId,
      customerId: value.customerId,
      supplierId: value.supplierId,
      productId: value.productId,
      dateFromUtc: toUtcIso(value.dateFromUtc ?? ''),
      dateToUtc: toUtcIso(value.dateToUtc ?? '')
    };
  }
}
