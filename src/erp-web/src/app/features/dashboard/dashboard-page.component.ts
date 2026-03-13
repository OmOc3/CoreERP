import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { forkJoin } from 'rxjs';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { ErpMaterialModule } from '../../shared/material';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { approvalDocumentTypeOptions, approvalStatusOptions } from '../../core/models/erp.enums';
import { ApprovalRequestDto, DashboardDto } from '../../core/models/erp.models';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

@Component({
  selector: 'erp-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    BaseChartDirective,
    PageHeaderComponent,
    OptionLabelPipe,
    ErpMaterialModule
  ],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Executive dashboard"
        kicker="Overview"
        subtitle="Track branch-aware sales, purchasing, stock value, low stock risk, collections, and workflow approvals from one view.">
      </erp-page-header>

      <section class="erp-card erp-section">
        <form class="erp-toolbar" [formGroup]="filters" (ngSubmit)="load()">
          <mat-form-field appearance="outline">
            <mat-label>Branch</mat-label>
            <mat-select formControlName="branchId">
              <mat-option [value]="null">All branches</mat-option>
              <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>From</mat-label>
            <input matInput type="date" formControlName="dateFromUtc" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>To</mat-label>
            <input matInput type="date" formControlName="dateToUtc" />
          </mat-form-field>

          <button mat-flat-button color="primary" type="submit" [disabled]="loading">Refresh dashboard</button>
        </form>
      </section>

      <mat-progress-bar *ngIf="loading" mode="indeterminate"></mat-progress-bar>

      <section class="erp-kpi-grid" *ngIf="dashboard">
        <article class="erp-card erp-kpi-card" *ngFor="let card of dashboard.kpis">
          <div class="label">{{ card.label }}</div>
          <div class="value">{{ card.value | currency:'USD':'symbol':'1.0-0' }}</div>
        </article>
      </section>

      <section class="erp-grid" style="grid-template-columns: 2fr 1fr; margin-top: 16px;">
        <article class="erp-card erp-section">
          <div class="erp-toolbar">
            <div>
              <strong>Sales vs purchases trend</strong>
              <div class="muted">Daily posted invoice totals for the selected range.</div>
            </div>
          </div>
          <canvas baseChart [data]="trendChartData" [options]="chartOptions" [type]="'line'"></canvas>
        </article>

        <article class="erp-card erp-section">
          <strong>Collection status</strong>
          <div class="erp-grid" style="margin-top: 14px;">
            <div class="erp-card erp-section" style="background: rgba(55, 116, 91, 0.08);">
              <div class="label">Paid invoices</div>
              <div class="value">{{ dashboard?.paidInvoices ?? 0 | currency:'USD':'symbol':'1.0-0' }}</div>
            </div>
            <div class="erp-card erp-section" style="background: rgba(182, 106, 34, 0.08);">
              <div class="label">Open invoices</div>
              <div class="value">{{ dashboard?.openInvoices ?? 0 | currency:'USD':'symbol':'1.0-0' }}</div>
            </div>
            <div class="erp-card erp-section" style="background: rgba(17, 75, 95, 0.08);">
              <div class="label">Pending approvals</div>
              <div class="value">{{ dashboard?.pendingApprovals ?? 0 }}</div>
            </div>
          </div>
        </article>
      </section>

      <section class="erp-grid" style="grid-template-columns: repeat(2, minmax(0, 1fr)); margin-top: 16px;">
        <article class="erp-card erp-section">
          <strong>Top selling products</strong>
          <table class="erp-table" *ngIf="dashboard?.topProducts?.length; else noProducts">
            <thead>
              <tr>
                <th>Product</th>
                <th>Quantity</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of dashboard?.topProducts">
                <td>{{ item.label }}</td>
                <td>{{ item.value | number:'1.0-2' }}</td>
              </tr>
            </tbody>
          </table>
          <ng-template #noProducts><div class="erp-empty">No sales in the selected period.</div></ng-template>
        </article>

        <article class="erp-card erp-section">
          <strong>Top customers</strong>
          <table class="erp-table" *ngIf="dashboard?.topCustomers?.length; else noCustomers">
            <thead>
              <tr>
                <th>Customer</th>
                <th>Value</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of dashboard?.topCustomers">
                <td>{{ item.label }}</td>
                <td>{{ item.value | currency:'USD':'symbol':'1.0-0' }}</td>
              </tr>
            </tbody>
          </table>
          <ng-template #noCustomers><div class="erp-empty">No customer activity in the selected period.</div></ng-template>
        </article>
      </section>

      <section class="erp-grid" style="grid-template-columns: 1.2fr 1fr; margin-top: 16px;">
        <article class="erp-card erp-section erp-scroll-card">
          <div class="erp-toolbar">
            <div>
              <strong>Pending approvals</strong>
              <div class="muted">Approve or reject workflow requests without leaving the dashboard.</div>
            </div>
            <span class="erp-pill warn" *ngIf="approvalQueue.length">{{ approvalQueue.length }} pending</span>
          </div>

          <table class="erp-table" *ngIf="approvalQueue.length; else noApprovals">
            <thead>
              <tr>
                <th>Document</th>
                <th>Rule</th>
                <th>Branch</th>
                <th>Requested</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let request of approvalQueue">
                <td>
                  <div>{{ request.documentType | optionLabel:approvalDocumentTypeOptions }}</div>
                  <small class="muted">{{ request.status | optionLabel:approvalStatusOptions }}</small>
                </td>
                <td>{{ request.ruleName }}</td>
                <td>{{ request.branchName || 'Global' }}</td>
                <td>{{ request.requestedAtUtc | date:'medium' }}</td>
                <td>
                  <div class="erp-actions">
                    <button mat-flat-button color="primary" type="button" (click)="review(request, true)">Approve</button>
                    <button mat-stroked-button type="button" (click)="review(request, false)">Reject</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>

          <ng-template #noApprovals>
            <div class="erp-empty">There are no pending approvals for your accessible branches.</div>
          </ng-template>
        </article>

        <article class="erp-card erp-section erp-scroll-card">
          <div class="erp-toolbar">
            <div>
              <strong>Low stock alerts</strong>
              <div class="muted">Recent alert feed generated from branch stock balances and reorder levels.</div>
            </div>
            <span class="erp-pill danger" *ngIf="dashboard?.lowStockCount">{{ dashboard?.lowStockCount }} items</span>
          </div>

          <div class="erp-grid" *ngIf="dashboard?.lowStockAlerts?.length; else noAlerts">
            <article class="erp-card erp-section" *ngFor="let alert of dashboard?.lowStockAlerts">
              <div class="erp-toolbar" style="margin-bottom: 10px;">
                <strong>{{ alert.title }}</strong>
                <span class="erp-pill" [class.warn]="!alert.isRead">{{ alert.isRead ? 'Read' : 'Unread' }}</span>
              </div>
              <p class="muted">{{ alert.message }}</p>
              <small class="muted">{{ alert.triggeredAtUtc | date:'medium' }}</small>
            </article>
          </div>

          <ng-template #noAlerts>
            <div class="erp-empty">No low stock alerts were generated for the selected branch and date range.</div>
          </ng-template>
        </article>
      </section>
    </div>
  `,
  styles: [`
    .muted {
      color: var(--erp-muted);
    }
  `]
})
export class DashboardPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  loading = false;
  branches: LookupOption[] = [];
  dashboard: DashboardDto | null = null;
  approvalQueue: ApprovalRequestDto[] = [];

  readonly approvalDocumentTypeOptions = approvalDocumentTypeOptions;
  readonly approvalStatusOptions = approvalStatusOptions;
  readonly chartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true
      }
    }
  };
  trendChartData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      { label: 'Sales', data: [], tension: 0.3, borderColor: '#114b5f', backgroundColor: 'rgba(17, 75, 95, 0.16)' },
      { label: 'Purchases', data: [], tension: 0.3, borderColor: '#b66a22', backgroundColor: 'rgba(182, 106, 34, 0.16)' }
    ]
  };

  readonly filters = this.fb.group({
    branchId: [null as string | null],
    dateFromUtc: [toDateInputValue(new Date(Date.now() - 29 * 24 * 60 * 60 * 1000))],
    dateToUtc: [toDateInputValue(new Date())]
  });

  constructor() {
    this.referenceData.getBranches().subscribe({
      next: (branches) => {
        this.branches = branches;
      }
    });

    this.load();
  }

  load(): void {
    this.loading = true;
    const query = {
      branchId: this.filters.value.branchId,
      dateFromUtc: toUtcIso(this.filters.value.dateFromUtc ?? ''),
      dateToUtc: toUtcIso(this.filters.value.dateToUtc ?? '')
    };

    forkJoin({
      dashboard: this.api.get<DashboardDto>('dashboard', query),
      approvals: this.api.get<PagedResult<ApprovalRequestDto>>('approvals/requests', {
        pageNumber: 1,
        pageSize: 10,
        status: 1,
        branchId: query.branchId
      })
    }).subscribe({
      next: ({ dashboard, approvals }) => {
        this.loading = false;
        this.dashboard = dashboard;
        this.approvalQueue = approvals.items;
        this.trendChartData = {
          labels: dashboard.trends.map((item) => new Date(item.date).toLocaleDateString()),
          datasets: [
            { ...this.trendChartData.datasets[0], data: dashboard.trends.map((item) => item.sales) },
            { ...this.trendChartData.datasets[1], data: dashboard.trends.map((item) => item.purchases) }
          ]
        };
      },
      error: (error) => {
        this.loading = false;
        this.notifications.error(error?.error?.detail ?? 'Unable to load the dashboard.');
      }
    });
  }

  review(request: ApprovalRequestDto, approved: boolean): void {
    const action = approved ? 'approve' : 'reject';
    this.api.post<void>(`approvals/requests/${request.id}/${action}`, { comments: null }).subscribe({
      next: () => {
        this.notifications.success(`Approval request ${approved ? 'approved' : 'rejected'}.`);
        this.load();
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to update the approval request.');
      }
    });
  }
}
