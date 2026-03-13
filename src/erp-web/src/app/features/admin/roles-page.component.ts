import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { approvalDocumentTypeOptions } from '../../core/models/erp.enums';
import { ApprovalRuleDto, PermissionDto, RoleDto, UserListItemDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';

@Component({
  selector: 'erp-roles-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Roles & workflow"
        kicker="Administration"
        subtitle="Manage permission-bearing roles and the approval thresholds that control purchasing, sales, and high-value invoice workflows.">
      </erp-page-header>

      <mat-tab-group>
        <mat-tab label="Roles">
          <section class="erp-split" style="margin-top: 16px;">
            <article class="erp-card">
              <div class="erp-section">
                <form [formGroup]="roleForm" (ngSubmit)="saveRole()" class="erp-form-grid">
                  <mat-form-field appearance="outline">
                    <mat-label>Name</mat-label>
                    <input matInput formControlName="name" />
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Description</mat-label>
                    <textarea matInput rows="3" formControlName="description"></textarea>
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Permissions</mat-label>
                    <mat-select formControlName="permissionCodes" multiple>
                      <mat-option *ngFor="let permission of permissions" [value]="permission.code">
                        {{ permission.module }} | {{ permission.name }}
                      </mat-option>
                    </mat-select>
                  </mat-form-field>

                  <div class="erp-actions">
                    <button mat-flat-button color="primary" type="submit">{{ selectedRoleId ? 'Save role' : 'Create role' }}</button>
                    <button mat-stroked-button type="button" (click)="resetRoleForm()">Clear</button>
                  </div>
                </form>
              </div>
            </article>

            <article class="erp-card">
              <div class="erp-section">
                <table class="erp-table" *ngIf="roles.length; else noRoles">
                  <thead>
                    <tr>
                      <th>Role</th>
                      <th>Description</th>
                      <th>Permissions</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let role of roles">
                      <td>{{ role.name }}</td>
                      <td>{{ role.description || 'No description' }}</td>
                      <td>{{ role.permissionCodes.length }}</td>
                      <td><button mat-stroked-button type="button" (click)="editRole(role)">Edit</button></td>
                    </tr>
                  </tbody>
                </table>
                <ng-template #noRoles><div class="erp-empty">No roles are available yet.</div></ng-template>
              </div>
            </article>
          </section>
        </mat-tab>

        <mat-tab label="Approval rules">
          <section class="erp-split" style="margin-top: 16px;">
            <article class="erp-card">
              <div class="erp-section">
                <form [formGroup]="ruleForm" (ngSubmit)="saveRule()" class="erp-form-grid">
                  <mat-form-field appearance="outline">
                    <mat-label>Name</mat-label>
                    <input matInput formControlName="name" />
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Document type</mat-label>
                    <mat-select formControlName="documentType">
                      <mat-option *ngFor="let type of approvalDocumentTypeOptions" [value]="type.value">{{ type.label }}</mat-option>
                    </mat-select>
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Branch</mat-label>
                    <mat-select formControlName="branchId">
                      <mat-option [value]="null">All branches</mat-option>
                      <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                    </mat-select>
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Minimum amount</mat-label>
                    <input matInput type="number" formControlName="minimumAmount" />
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Maximum amount</mat-label>
                    <input matInput type="number" formControlName="maximumAmount" />
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Approver role</mat-label>
                    <mat-select formControlName="approverRoleName">
                      <mat-option [value]="null">No role restriction</mat-option>
                      <mat-option *ngFor="let role of roles" [value]="role.name">{{ role.name }}</mat-option>
                    </mat-select>
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Approver user</mat-label>
                    <mat-select formControlName="approverUserId">
                      <mat-option [value]="null">No user restriction</mat-option>
                      <mat-option *ngFor="let user of users" [value]="user.id">{{ user.userName }}</mat-option>
                    </mat-select>
                  </mat-form-field>

                  <mat-slide-toggle formControlName="isActive">Active</mat-slide-toggle>

                  <div class="erp-actions">
                    <button mat-flat-button color="primary" type="submit">{{ selectedRuleId ? 'Save rule' : 'Create rule' }}</button>
                    <button mat-stroked-button type="button" (click)="resetRuleForm()">Clear</button>
                  </div>
                </form>
              </div>
            </article>

            <article class="erp-card">
              <div class="erp-section">
                <table class="erp-table" *ngIf="rulePage.items.length; else noRules">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Document</th>
                      <th>Branch</th>
                      <th>Threshold</th>
                      <th>Approver</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let rule of rulePage.items">
                      <td>{{ rule.name }}</td>
                      <td>{{ rule.documentType | optionLabel:approvalDocumentTypeOptions }}</td>
                      <td>{{ rule.branchName || 'All branches' }}</td>
                      <td>{{ rule.minimumAmount }} - {{ rule.maximumAmount || 'No max' }}</td>
                      <td>{{ rule.approverRoleName || rule.approverUserId || 'Any approver' }}</td>
                      <td>
                        <div class="erp-actions">
                          <button mat-stroked-button type="button" (click)="editRule(rule)">Edit</button>
                          <button mat-button type="button" (click)="deleteRule(rule.id)">Delete</button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
                <ng-template #noRules><div class="erp-empty">No approval rules are configured yet.</div></ng-template>
              </div>
            </article>
          </section>
        </mat-tab>
      </mat-tab-group>
    </div>
  `
})
export class RolesPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly approvalDocumentTypeOptions = approvalDocumentTypeOptions;

  selectedRoleId: string | null = null;
  selectedRuleId: string | null = null;
  branches: LookupOption[] = [];
  permissions: PermissionDto[] = [];
  roles: RoleDto[] = [];
  users: UserListItemDto[] = [];
  rulePage: PagedResult<ApprovalRuleDto> = { items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

  readonly roleForm = this.fb.group({
    name: ['', [Validators.required]],
    description: [''],
    permissionCodes: [[] as string[]]
  });

  readonly ruleForm = this.fb.group({
    name: ['', [Validators.required]],
    documentType: [1, [Validators.required]],
    branchId: [null as string | null],
    minimumAmount: [0, [Validators.required]],
    maximumAmount: [null as number | null],
    approverRoleName: [null as string | null],
    approverUserId: [null as string | null],
    isActive: [true]
  });

  constructor() {
    forkJoin({
      branches: this.referenceData.getBranches(),
      permissions: this.referenceData.getPermissions(),
      roles: this.referenceData.getRoles(),
      users: this.api.get<PagedResult<UserListItemDto>>('users', { pageNumber: 1, pageSize: 100 }),
      rules: this.api.get<PagedResult<ApprovalRuleDto>>('approvals/rules', { pageNumber: 1, pageSize: 100 })
    }).subscribe({
      next: ({ branches, permissions, roles, users, rules }) => {
        this.branches = branches;
        this.permissions = permissions;
        this.roles = roles;
        this.users = users.items;
        this.rulePage = rules;
      }
    });
  }

  editRole(role: RoleDto): void {
    this.selectedRoleId = role.id;
    this.roleForm.patchValue({
      name: role.name,
      description: role.description ?? '',
      permissionCodes: role.permissionCodes
    });
  }

  saveRole(): void {
    if (this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }

    const request = this.roleForm.getRawValue();
    if (this.selectedRoleId) {
      this.api.put<void>(`roles/${this.selectedRoleId}`, request).subscribe({
        next: () => {
          this.notifications.success('Role saved successfully.');
          this.resetRoleForm();
          this.reloadData();
        },
        error: (error: any) => this.notifications.error(error?.error?.detail ?? 'Unable to save the role.')
      });
      return;
    }

    this.api.post<string>('roles', request).subscribe({
      next: () => {
        this.notifications.success('Role saved successfully.');
        this.resetRoleForm();
        this.reloadData();
      },
      error: (error: any) => this.notifications.error(error?.error?.detail ?? 'Unable to save the role.')
    });
  }

  resetRoleForm(): void {
    this.selectedRoleId = null;
    this.roleForm.reset({
      name: '',
      description: '',
      permissionCodes: []
    });
  }

  editRule(rule: ApprovalRuleDto): void {
    this.selectedRuleId = rule.id;
    this.ruleForm.patchValue({
      name: rule.name,
      documentType: rule.documentType,
      branchId: rule.branchId ?? null,
      minimumAmount: rule.minimumAmount,
      maximumAmount: rule.maximumAmount ?? null,
      approverRoleName: rule.approverRoleName ?? null,
      approverUserId: rule.approverUserId ?? null,
      isActive: rule.isActive
    });
  }

  saveRule(): void {
    if (this.ruleForm.invalid) {
      this.ruleForm.markAllAsTouched();
      return;
    }

    const request = this.ruleForm.getRawValue();
    if (this.selectedRuleId) {
      this.api.put<void>(`approvals/rules/${this.selectedRuleId}`, request).subscribe({
        next: () => {
          this.notifications.success('Approval rule saved successfully.');
          this.resetRuleForm();
          this.reloadData();
        },
        error: (error: any) => this.notifications.error(error?.error?.detail ?? 'Unable to save the approval rule.')
      });
      return;
    }

    this.api.post<string>('approvals/rules', request).subscribe({
      next: () => {
        this.notifications.success('Approval rule saved successfully.');
        this.resetRuleForm();
        this.reloadData();
      },
      error: (error: any) => this.notifications.error(error?.error?.detail ?? 'Unable to save the approval rule.')
    });
  }

  resetRuleForm(): void {
    this.selectedRuleId = null;
    this.ruleForm.reset({
      name: '',
      documentType: 1,
      branchId: null,
      minimumAmount: 0,
      maximumAmount: null,
      approverRoleName: null,
      approverUserId: null,
      isActive: true
    });
  }

  deleteRule(id: string): void {
    if (!window.confirm('Delete this approval rule?')) {
      return;
    }

    this.api.delete<void>(`approvals/rules/${id}`).subscribe({
      next: () => {
        this.notifications.success('Approval rule deleted successfully.');
        this.reloadData();
      },
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to delete the approval rule.')
    });
  }

  private reloadData(): void {
    this.referenceData.getRoles().subscribe({ next: (roles) => this.roles = roles });
    this.api.get<PagedResult<ApprovalRuleDto>>('approvals/rules', { pageNumber: 1, pageSize: 100 }).subscribe({ next: (rules) => this.rulePage = rules });
  }
}
