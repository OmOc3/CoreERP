import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { ErpMaterialModule } from '../../shared/material';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { BranchDto, CategoryDto, CustomerDto, ProductListItemDto, SupplierDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';

type MasterEntityKey = 'products' | 'categories' | 'customers' | 'suppliers' | 'branches';

interface EntityConfig {
  title: string;
  subtitle: string;
  endpoint: string;
}

@Component({
  selector: 'erp-master-data-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, PagerComponent, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header [title]="currentConfig.title" [subtitle]="currentConfig.subtitle" kicker="Master Data">
        <button mat-flat-button color="primary" type="button" (click)="startNew()">New {{ singularLabel }}</button>
      </erp-page-header>

      <section class="erp-split">
        <article class="erp-card">
          <div class="erp-section">
            <h2 style="margin-top: 0;">{{ selectedId ? 'Edit record' : 'Create record' }}</h2>
            <form [formGroup]="form" (ngSubmit)="save()" class="erp-form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Code</mat-label>
                <input matInput formControlName="code" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Name</mat-label>
                <input matInput formControlName="name" />
              </mat-form-field>

              <ng-container *ngIf="entity === 'products'">
                <mat-form-field appearance="outline">
                  <mat-label>SKU</mat-label>
                  <input matInput formControlName="sku" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Category</mat-label>
                  <mat-select formControlName="categoryId">
                    <mat-option *ngFor="let category of categories" [value]="category.id">{{ category.name }}</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Reorder level</mat-label>
                  <input matInput type="number" formControlName="reorderLevel" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Standard cost</mat-label>
                  <input matInput type="number" formControlName="standardCost" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Sale price</mat-label>
                  <input matInput type="number" formControlName="salePrice" />
                </mat-form-field>

                <mat-slide-toggle formControlName="isStockTracked">Track inventory</mat-slide-toggle>
              </ng-container>

              <mat-form-field appearance="outline" *ngIf="entity === 'categories'">
                <mat-label>Description</mat-label>
                <textarea matInput rows="4" formControlName="description"></textarea>
              </mat-form-field>

              <ng-container *ngIf="entity === 'branches'">
                <mat-form-field appearance="outline">
                  <mat-label>Address</mat-label>
                  <textarea matInput rows="3" formControlName="address"></textarea>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Phone</mat-label>
                  <input matInput formControlName="phone" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Email</mat-label>
                  <input matInput formControlName="email" />
                </mat-form-field>
              </ng-container>

              <ng-container *ngIf="entity === 'customers' || entity === 'suppliers'">
                <mat-form-field appearance="outline">
                  <mat-label>Tax number</mat-label>
                  <input matInput formControlName="taxNumber" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Email</mat-label>
                  <input matInput formControlName="email" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Phone</mat-label>
                  <input matInput formControlName="phone" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Address</mat-label>
                  <textarea matInput rows="3" formControlName="address"></textarea>
                </mat-form-field>

                <mat-form-field appearance="outline" *ngIf="entity === 'customers'">
                  <mat-label>Credit limit</mat-label>
                  <input matInput type="number" formControlName="creditLimit" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Payment terms (days)</mat-label>
                  <input matInput type="number" formControlName="paymentTermsDays" />
                </mat-form-field>
              </ng-container>

              <mat-slide-toggle *ngIf="supportsActive" formControlName="isActive">Active</mat-slide-toggle>

              <div class="erp-actions">
                <button mat-flat-button color="primary" type="submit">{{ selectedId ? 'Save changes' : 'Create' }}</button>
                <button mat-stroked-button type="button" (click)="startNew()">Clear</button>
              </div>
            </form>
          </div>
        </article>

        <article class="erp-card erp-table-card">
          <div class="erp-section">
            <div class="erp-toolbar">
              <mat-form-field appearance="outline">
                <mat-label>Search</mat-label>
                <input matInput [formControl]="searchControl" (keyup.enter)="loadList(1)" />
              </mat-form-field>
              <button mat-stroked-button type="button" (click)="loadList(1)">Search</button>
              <span class="spacer"></span>
              <span class="erp-pill">{{ paged.totalCount }} records</span>
            </div>

            <table class="erp-table" *ngIf="paged.items.length; else emptyState">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Name</th>
                  <th>Details</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of paged.items">
                  <td>{{ item.code }}</td>
                  <td>{{ item.name }}</td>
                  <td>{{ detailsText(item) }}</td>
                  <td>{{ statusText(item) }}</td>
                  <td>
                    <div class="erp-actions">
                      <button mat-stroked-button type="button" (click)="edit(item.id)">Edit</button>
                      <button mat-button type="button" (click)="remove(item.id)">Delete</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>

            <ng-template #emptyState>
              <div class="erp-empty">No {{ currentConfig.title.toLowerCase() }} matched the current search.</div>
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
export class MasterDataPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly configs: Record<MasterEntityKey, EntityConfig> = {
    products: {
      title: 'Products',
      subtitle: 'Manage SKUs, categories, pricing, reorder thresholds, and stock tracking rules.',
      endpoint: 'products'
    },
    categories: {
      title: 'Categories',
      subtitle: 'Group products into operational and reporting families.',
      endpoint: 'categories'
    },
    customers: {
      title: 'Customers',
      subtitle: 'Maintain commercial customer records, credit limits, and payment terms.',
      endpoint: 'customers'
    },
    suppliers: {
      title: 'Suppliers',
      subtitle: 'Maintain vendors, tax data, and purchasing payment terms.',
      endpoint: 'suppliers'
    },
    branches: {
      title: 'Branches',
      subtitle: 'Manage branch master data used by inventory, approvals, and reporting.',
      endpoint: 'branches'
    }
  };

  entity: MasterEntityKey = 'products';
  currentConfig = this.configs.products;
  selectedId: string | null = null;
  categories: LookupOption[] = [];
  paged: PagedResult<BranchDto | CategoryDto | CustomerDto | SupplierDto | ProductListItemDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0
  };

  readonly form = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]],
    description: [''],
    address: [''],
    phone: [''],
    email: [''],
    isActive: [true],
    taxNumber: [''],
    creditLimit: [0],
    paymentTermsDays: [0],
    sku: [''],
    categoryId: [''],
    reorderLevel: [0],
    standardCost: [0],
    salePrice: [0],
    isStockTracked: [true]
  });

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const entity = params.get('entity') as MasterEntityKey | null;
      this.entity = entity && entity in this.configs ? entity : 'products';
      this.currentConfig = this.configs[this.entity];
      this.selectedId = null;
      this.resetForm();

      if (this.entity === 'products') {
        this.referenceData.getCategories().subscribe({
          next: (categories) => {
            this.categories = categories;
          }
        });
      }

      this.loadList(1);
    });
  }

  get supportsActive(): boolean {
    return this.entity !== 'categories';
  }

  get singularLabel(): string {
    return this.currentConfig.title.endsWith('s')
      ? this.currentConfig.title.slice(0, -1)
      : this.currentConfig.title;
  }

  loadList(pageNumber = 1): void {
    this.api.get<PagedResult<any>>(this.currentConfig.endpoint, {
      pageNumber,
      pageSize: 10,
      search: this.searchControl.value
    }).subscribe({
      next: (result) => {
        this.paged = result;
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? `Unable to load ${this.currentConfig.title.toLowerCase()}.`);
      }
    });
  }

  startNew(): void {
    this.selectedId = null;
    this.resetForm();
  }

  edit(id: string): void {
    this.api.get<any>(`${this.currentConfig.endpoint}/${id}`).subscribe({
      next: (item) => {
        this.selectedId = id;
        this.form.patchValue({
          code: item.code,
          name: item.name,
          description: item.description ?? '',
          address: item.address ?? '',
          phone: item.phone ?? '',
          email: item.email ?? '',
          isActive: item.isActive ?? true,
          taxNumber: item.taxNumber ?? '',
          creditLimit: item.creditLimit ?? 0,
          paymentTermsDays: item.paymentTermsDays ?? 0,
          sku: item.sku ?? '',
          categoryId: item.categoryId ?? '',
          reorderLevel: item.reorderLevel ?? 0,
          standardCost: item.standardCost ?? 0,
          salePrice: item.salePrice ?? 0,
          isStockTracked: item.isStockTracked ?? true
        });
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to load the selected record.');
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.buildRequest();
    if (this.selectedId) {
      this.api.put<void>(`${this.currentConfig.endpoint}/${this.selectedId}`, request).subscribe({
        next: () => {
          this.notifications.success(`${this.singularLabel} saved successfully.`);
          this.startNew();
          this.loadList(this.paged.pageNumber);
        },
        error: (error: any) => {
          this.notifications.error(error?.error?.detail ?? 'Unable to save the record.');
        }
      });
      return;
    }

    this.api.post<string>(this.currentConfig.endpoint, request).subscribe({
      next: () => {
        this.notifications.success(`${this.singularLabel} saved successfully.`);
        this.startNew();
        this.loadList(this.paged.pageNumber);
      },
      error: (error: any) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to save the record.');
      }
    });
  }

  remove(id: string): void {
    if (!window.confirm('Delete this record?')) {
      return;
    }

    this.api.delete<void>(`${this.currentConfig.endpoint}/${id}`).subscribe({
      next: () => {
        this.notifications.success('Record deleted successfully.');
        if (this.selectedId === id) {
          this.startNew();
        }
        this.loadList(this.paged.pageNumber);
      },
      error: (error) => {
        this.notifications.error(error?.error?.detail ?? 'Unable to delete the record.');
      }
    });
  }

  detailsText(item: any): string {
    if (this.entity === 'products') {
      return `${item.sku} | ${item.categoryName} | Sell ${item.salePrice}`;
    }

    if (this.entity === 'branches') {
      return [item.phone, item.email].filter(Boolean).join(' | ') || 'Branch directory record';
    }

    if (this.entity === 'categories') {
      return item.description || 'No description';
    }

    if (this.entity === 'customers') {
      return [item.phone, item.email].filter(Boolean).join(' | ') || `Credit ${item.creditLimit}`;
    }

    return [item.phone, item.email].filter(Boolean).join(' | ') || `Terms ${item.paymentTermsDays} days`;
  }

  statusText(item: any): string {
    if (!this.supportsActive) {
      return 'Always available';
    }

    return item.isActive ? 'Active' : 'Inactive';
  }

  private resetForm(): void {
    this.form.reset({
      code: '',
      name: '',
      description: '',
      address: '',
      phone: '',
      email: '',
      isActive: true,
      taxNumber: '',
      creditLimit: 0,
      paymentTermsDays: 0,
      sku: '',
      categoryId: '',
      reorderLevel: 0,
      standardCost: 0,
      salePrice: 0,
      isStockTracked: true
    });
  }

  private buildRequest(): any {
    const value = this.form.getRawValue();

    switch (this.entity) {
      case 'products':
        return {
          code: value.code,
          name: value.name,
          sku: value.sku,
          description: value.description,
          categoryId: value.categoryId,
          unitOfMeasureId: null,
          reorderLevel: Number(value.reorderLevel ?? 0),
          standardCost: Number(value.standardCost ?? 0),
          salePrice: Number(value.salePrice ?? 0),
          isStockTracked: !!value.isStockTracked,
          isActive: !!value.isActive
        };
      case 'categories':
        return {
          code: value.code,
          name: value.name,
          description: value.description
        };
      case 'customers':
        return {
          code: value.code,
          name: value.name,
          taxNumber: value.taxNumber,
          email: value.email,
          phone: value.phone,
          address: value.address,
          creditLimit: Number(value.creditLimit ?? 0),
          paymentTermsDays: Number(value.paymentTermsDays ?? 0),
          isActive: !!value.isActive
        };
      case 'suppliers':
        return {
          code: value.code,
          name: value.name,
          taxNumber: value.taxNumber,
          email: value.email,
          phone: value.phone,
          address: value.address,
          paymentTermsDays: Number(value.paymentTermsDays ?? 0),
          isActive: !!value.isActive
        };
      case 'branches':
        return {
          code: value.code,
          name: value.name,
          address: value.address,
          phone: value.phone,
          email: value.email,
          isActive: !!value.isActive
        };
    }
  }
}
