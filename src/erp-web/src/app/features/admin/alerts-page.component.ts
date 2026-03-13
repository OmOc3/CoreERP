import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { PagedResult } from '../../core/models/common.models';
import { alertTypeOptions } from '../../core/models/erp.enums';
import { AlertDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';

@Component({
  selector: 'erp-alerts-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, PageHeaderComponent, PagerComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Alerts"
        kicker="Operations"
        subtitle="Track low stock and workflow alerts across accessible branches and mark them as reviewed when action is complete.">
      </erp-page-header>

      <section class="erp-card erp-section">
        <form class="erp-toolbar" [formGroup]="form" (ngSubmit)="load(1)">
          <mat-form-field appearance="outline">
            <mat-label>Alert type</mat-label>
            <mat-select formControlName="type">
              <mat-option [value]="null">All types</mat-option>
              <mat-option *ngFor="let type of alertTypeOptions" [value]="type.value">{{ type.label }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-slide-toggle formControlName="activeOnly">Active only</mat-slide-toggle>
          <button mat-stroked-button type="submit">Filter</button>
        </form>
      </section>

      <section class="erp-card erp-section" style="margin-top: 16px;">
        <table class="erp-table" *ngIf="paged.items.length; else emptyState">
          <thead>
            <tr>
              <th>Type</th>
              <th>Branch</th>
              <th>Title</th>
              <th>Message</th>
              <th>Triggered</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of paged.items">
              <td>{{ item.type | optionLabel:alertTypeOptions }}</td>
              <td>{{ item.branchName }}</td>
              <td>{{ item.title }}</td>
              <td>{{ item.message }}</td>
              <td>{{ item.triggeredAtUtc | date:'medium' }}</td>
              <td>{{ item.isRead ? 'Read' : 'Unread' }}</td>
              <td>
                <button mat-stroked-button type="button" (click)="markRead(item.id)" [disabled]="item.isRead">Mark read</button>
              </td>
            </tr>
          </tbody>
        </table>

        <ng-template #emptyState><div class="erp-empty">No alerts matched the current filters.</div></ng-template>
        <erp-pager [pageNumber]="paged.pageNumber" [totalPages]="paged.totalPages" (previous)="load(paged.pageNumber - 1)" (next)="load(paged.pageNumber + 1)"></erp-pager>
      </section>
    </div>
  `
})
export class AlertsPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);

  readonly alertTypeOptions = alertTypeOptions;
  paged: PagedResult<AlertDto> = { items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

  readonly form = this.fb.group({
    type: [null as number | null],
    activeOnly: [true]
  });

  constructor() {
    this.load(1);
  }

  load(pageNumber = 1): void {
    const value = this.form.getRawValue();
    this.api.get<PagedResult<AlertDto>>('alerts', {
      pageNumber,
      pageSize: 20,
      type: value.type,
      activeOnly: value.activeOnly
    }).subscribe({
      next: (result) => this.paged = result,
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to load alerts.')
    });
  }

  markRead(id: string): void {
    this.api.post<void>(`alerts/${id}/read`, {}).subscribe({
      next: () => {
        this.notifications.success('Alert marked as read.');
        this.load(this.paged.pageNumber);
      },
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to update the alert.')
    });
  }
}
