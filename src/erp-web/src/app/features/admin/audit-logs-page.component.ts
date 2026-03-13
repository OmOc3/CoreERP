import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { PagedResult } from '../../core/models/common.models';
import { AuditLogDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { ErpMaterialModule } from '../../shared/material';
import { toDateInputValue, toUtcIso } from '../../shared/utils/date-utils';

@Component({
  selector: 'erp-audit-logs-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, PageHeaderComponent, PagerComponent, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Audit logs"
        kicker="Governance"
        subtitle="Inspect critical create, update, post, approve, reject, and delete actions captured by the ERP audit trail.">
      </erp-page-header>

      <section class="erp-card erp-section">
        <form class="erp-toolbar" [formGroup]="form" (ngSubmit)="load(1)">
          <mat-form-field appearance="outline">
            <mat-label>Entity</mat-label>
            <input matInput formControlName="entityName" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Action</mat-label>
            <input matInput formControlName="action" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Search</mat-label>
            <input matInput formControlName="search" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>From</mat-label>
            <input matInput type="date" formControlName="dateFromUtc" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>To</mat-label>
            <input matInput type="date" formControlName="dateToUtc" />
          </mat-form-field>

          <button mat-stroked-button type="submit">Filter</button>
        </form>
      </section>

      <section class="erp-card erp-section" style="margin-top: 16px;">
        <table class="erp-table" *ngIf="paged.items.length; else emptyState">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Entity</th>
              <th>Action</th>
              <th>User</th>
              <th>Entity id</th>
              <th>IP</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of paged.items">
              <td>{{ item.timestampUtc | date:'medium' }}</td>
              <td>{{ item.entityName }}</td>
              <td>{{ item.action }}</td>
              <td>{{ item.userName || 'system' }}</td>
              <td>{{ item.entityId }}</td>
              <td>{{ item.ipAddress || 'n/a' }}</td>
            </tr>
          </tbody>
        </table>

        <ng-template #emptyState><div class="erp-empty">No audit log entries matched the current filters.</div></ng-template>
        <erp-pager [pageNumber]="paged.pageNumber" [totalPages]="paged.totalPages" (previous)="load(paged.pageNumber - 1)" (next)="load(paged.pageNumber + 1)"></erp-pager>
      </section>
    </div>
  `
})
export class AuditLogsPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);

  paged: PagedResult<AuditLogDto> = { items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

  readonly form = this.fb.group({
    entityName: [''],
    action: [''],
    search: [''],
    dateFromUtc: [toDateInputValue(new Date(Date.now() - 6 * 24 * 60 * 60 * 1000))],
    dateToUtc: [toDateInputValue(new Date())]
  });

  constructor() {
    this.load(1);
  }

  load(pageNumber = 1): void {
    const value = this.form.getRawValue();
    this.api.get<PagedResult<AuditLogDto>>('audit-logs', {
      pageNumber,
      pageSize: 20,
      entityName: value.entityName,
      action: value.action,
      search: value.search,
      dateFromUtc: toUtcIso(value.dateFromUtc ?? ''),
      dateToUtc: toUtcIso(value.dateToUtc ?? '')
    }).subscribe({
      next: (result) => this.paged = result,
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to load audit logs.')
    });
  }
}
