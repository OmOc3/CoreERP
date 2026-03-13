import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { RoleDto, UserDetailDto, UserListItemDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { ErpMaterialModule } from '../../shared/material';

@Component({
  selector: 'erp-users-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, PagerComponent, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Users"
        kicker="Administration"
        subtitle="Manage active users, assign roles, restrict branch access, and reset passwords when operational access changes.">
      </erp-page-header>

      <section class="erp-split">
        <article class="erp-card">
          <div class="erp-section">
            <h2 style="margin-top: 0;">{{ selectedId ? 'Edit user' : 'Create user' }}</h2>
            <form [formGroup]="form" (ngSubmit)="save()" class="erp-form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Username</mat-label>
                <input matInput formControlName="userName" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Email</mat-label>
                <input matInput formControlName="email" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Password</mat-label>
                <input matInput type="password" formControlName="password" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Roles</mat-label>
                <mat-select formControlName="roles" multiple>
                  <mat-option *ngFor="let role of roles" [value]="role.name">{{ role.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Branch access</mat-label>
                <mat-select formControlName="branchIds" multiple>
                  <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Default branch</mat-label>
                <mat-select formControlName="defaultBranchId">
                  <mat-option [value]="null">None</mat-option>
                  <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-slide-toggle formControlName="isActive">Active</mat-slide-toggle>

              <div class="erp-actions">
                <button mat-flat-button color="primary" type="submit">{{ selectedId ? 'Save changes' : 'Create user' }}</button>
                <button mat-stroked-button type="button" (click)="startNew()">Clear</button>
                <button mat-button type="button" *ngIf="selectedId" (click)="resetPassword()">Reset password</button>
              </div>
            </form>
          </div>
        </article>

        <article class="erp-card">
          <div class="erp-section">
            <table class="erp-table" *ngIf="paged.items.length; else emptyState">
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Email</th>
                  <th>Roles</th>
                  <th>Branches</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of paged.items">
                  <td>{{ item.userName }}</td>
                  <td>{{ item.email || 'N/A' }}</td>
                  <td>{{ item.roles.join(', ') }}</td>
                  <td>{{ item.branchIds.length }}</td>
                  <td>{{ item.isActive ? 'Active' : 'Inactive' }}</td>
                  <td><button mat-stroked-button type="button" (click)="edit(item.id)">Edit</button></td>
                </tr>
              </tbody>
            </table>

            <ng-template #emptyState><div class="erp-empty">No users are available yet.</div></ng-template>
            <erp-pager [pageNumber]="paged.pageNumber" [totalPages]="paged.totalPages" (previous)="loadList(paged.pageNumber - 1)" (next)="loadList(paged.pageNumber + 1)"></erp-pager>
          </div>
        </article>
      </section>
    </div>
  `
})
export class UsersPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  selectedId: string | null = null;
  branches: LookupOption[] = [];
  roles: RoleDto[] = [];
  paged: PagedResult<UserListItemDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  };

  readonly form = this.fb.group({
    userName: ['', [Validators.required]],
    email: [''],
    password: [''],
    roles: [[] as string[]],
    branchIds: [[] as string[]],
    defaultBranchId: [null as string | null],
    isActive: [true]
  });

  constructor() {
    forkJoin({
      branches: this.referenceData.getBranches(),
      roles: this.referenceData.getRoles()
    }).subscribe({
      next: ({ branches, roles }) => {
        this.branches = branches;
        this.roles = roles;
      }
    });

    this.loadList(1);
  }

  loadList(pageNumber = 1): void {
    this.api.get<PagedResult<UserListItemDto>>('users', { pageNumber, pageSize: 10 }).subscribe({
      next: (result) => this.paged = result,
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to load users.')
    });
  }

  startNew(): void {
    this.selectedId = null;
    this.form.reset({
      userName: '',
      email: '',
      password: '',
      roles: [],
      branchIds: [],
      defaultBranchId: null,
      isActive: true
    });
  }

  edit(id: string): void {
    this.api.get<UserDetailDto>(`users/${id}`).subscribe({
      next: (user) => {
        this.selectedId = id;
        this.form.patchValue({
          userName: user.userName,
          email: user.email ?? '',
          password: '',
          roles: user.roles,
          branchIds: user.branchIds,
          defaultBranchId: user.defaultBranchId ?? null,
          isActive: user.isActive
        });
      },
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to load the selected user.')
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();
    if (this.selectedId) {
      this.api.put<void>(`users/${this.selectedId}`, request).subscribe({
        next: () => {
          this.notifications.success('User saved successfully.');
          this.startNew();
          this.loadList(this.paged.pageNumber);
        },
        error: (error: any) => this.notifications.error(error?.error?.detail ?? 'Unable to save the user.')
      });
      return;
    }

    this.api.post<string>('users', request).subscribe({
      next: () => {
        this.notifications.success('User saved successfully.');
        this.startNew();
        this.loadList(this.paged.pageNumber);
      },
      error: (error: any) => this.notifications.error(error?.error?.detail ?? 'Unable to save the user.')
    });
  }

  resetPassword(): void {
    if (!this.selectedId) {
      return;
    }

    const password = window.prompt('Enter the new password for this user:');
    if (!password) {
      return;
    }

    this.api.post<void>(`users/${this.selectedId}/reset-password`, { newPassword: password }).subscribe({
      next: () => this.notifications.success('Password reset successfully.'),
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to reset the password.')
    });
  }
}
