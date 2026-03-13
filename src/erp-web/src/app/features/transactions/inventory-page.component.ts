import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupOption, PagedResult } from '../../core/models/common.models';
import { inventoryMovementTypeOptions } from '../../core/models/erp.enums';
import { InventoryMovementDto, LowStockItemDto, StockBalanceDto } from '../../core/models/erp.models';
import { ApiService } from '../../core/services/api.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PagerComponent } from '../../shared/components/pager.component';
import { OptionLabelPipe } from '../../shared/pipes/option-label.pipe';
import { ErpMaterialModule } from '../../shared/material';

@Component({
  selector: 'erp-inventory-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, DatePipe, DecimalPipe, PageHeaderComponent, PagerComponent, OptionLabelPipe, ErpMaterialModule],
  template: `
    <div class="erp-page">
      <erp-page-header
        title="Inventory"
        kicker="Stock Control"
        subtitle="Monitor branch balances, movement ledgers, low stock risk, and post adjustments or inter-branch transfers.">
      </erp-page-header>

      <mat-tab-group>
        <mat-tab label="Balances">
          <section class="erp-card erp-section" style="margin-top: 16px;">
            <table class="erp-table" *ngIf="balances.items.length; else noBalances">
              <thead>
                <tr>
                  <th>Branch</th>
                  <th>Product</th>
                  <th>On hand</th>
                  <th>Available</th>
                  <th>Avg cost</th>
                  <th>Stock value</th>
                  <th>Low stock</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of balances.items">
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.productCode }} - {{ item.productName }}</td>
                  <td>{{ item.quantityOnHand | number:'1.0-2' }}</td>
                  <td>{{ item.availableQuantity | number:'1.0-2' }}</td>
                  <td>{{ item.averageCost | currency:'USD':'symbol':'1.0-2' }}</td>
                  <td>{{ item.stockValue | currency:'USD':'symbol':'1.0-0' }}</td>
                  <td><span class="erp-pill" [class.danger]="item.isLowStock">{{ item.isLowStock ? 'Low' : 'Healthy' }}</span></td>
                </tr>
              </tbody>
            </table>

            <ng-template #noBalances><div class="erp-empty">No stock balances are available yet.</div></ng-template>
            <erp-pager [pageNumber]="balances.pageNumber" [totalPages]="balances.totalPages" (previous)="loadBalances(balances.pageNumber - 1)" (next)="loadBalances(balances.pageNumber + 1)"></erp-pager>
          </section>
        </mat-tab>

        <mat-tab label="Movements">
          <section class="erp-card erp-section" style="margin-top: 16px;">
            <table class="erp-table" *ngIf="movements.items.length; else noMovements">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Branch</th>
                  <th>Product</th>
                  <th>Type</th>
                  <th>Qty</th>
                  <th>Reference</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of movements.items">
                  <td>{{ item.movementDateUtc | date:'medium' }}</td>
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.productCode }} - {{ item.productName }}</td>
                  <td>{{ item.type | optionLabel:inventoryMovementTypeOptions }}</td>
                  <td>{{ item.quantity | number:'1.0-2' }}</td>
                  <td>{{ item.referenceNumber }}</td>
                </tr>
              </tbody>
            </table>

            <ng-template #noMovements><div class="erp-empty">No stock movements have been recorded yet.</div></ng-template>
            <erp-pager [pageNumber]="movements.pageNumber" [totalPages]="movements.totalPages" (previous)="loadMovements(movements.pageNumber - 1)" (next)="loadMovements(movements.pageNumber + 1)"></erp-pager>
          </section>
        </mat-tab>

        <mat-tab label="Low stock">
          <section class="erp-card erp-section" style="margin-top: 16px;">
            <table class="erp-table" *ngIf="lowStock.length; else noLowStock">
              <thead>
                <tr>
                  <th>Branch</th>
                  <th>Product</th>
                  <th>On hand</th>
                  <th>Reorder level</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of lowStock">
                  <td>{{ item.branchName }}</td>
                  <td>{{ item.productCode }} - {{ item.productName }}</td>
                  <td>{{ item.quantityOnHand | number:'1.0-2' }}</td>
                  <td>{{ item.reorderLevel | number:'1.0-2' }}</td>
                </tr>
              </tbody>
            </table>

            <ng-template #noLowStock><div class="erp-empty">No products are currently below reorder level.</div></ng-template>
          </section>
        </mat-tab>
      </mat-tab-group>

      <section class="erp-grid" style="grid-template-columns: repeat(2, minmax(0, 1fr)); margin-top: 16px;">
        <article class="erp-card">
          <div class="erp-section">
            <h2 style="margin-top: 0;">Stock adjustment</h2>
            <form [formGroup]="adjustmentForm" (ngSubmit)="postAdjustment()" class="erp-form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Branch</mat-label>
                <mat-select formControlName="branchId">
                  <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Product</mat-label>
                <mat-select formControlName="productId">
                  <mat-option *ngFor="let product of products" [value]="product.id">{{ product.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Qty difference</mat-label>
                <input matInput type="number" formControlName="quantityDifference" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Unit cost</mat-label>
                <input matInput type="number" formControlName="unitCost" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Remarks</mat-label>
                <textarea matInput rows="2" formControlName="remarks"></textarea>
              </mat-form-field>

              <button mat-flat-button color="primary" type="submit">Post adjustment</button>
            </form>
          </div>
        </article>

        <article class="erp-card">
          <div class="erp-section">
            <h2 style="margin-top: 0;">Branch transfer</h2>
            <form [formGroup]="transferForm" (ngSubmit)="postTransfer()" class="erp-form-grid">
              <mat-form-field appearance="outline">
                <mat-label>From branch</mat-label>
                <mat-select formControlName="fromBranchId">
                  <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>To branch</mat-label>
                <mat-select formControlName="toBranchId">
                  <mat-option *ngFor="let branch of branches" [value]="branch.id">{{ branch.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Product</mat-label>
                <mat-select formControlName="productId">
                  <mat-option *ngFor="let product of products" [value]="product.id">{{ product.name }}</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Quantity</mat-label>
                <input matInput type="number" formControlName="quantity" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Remarks</mat-label>
                <textarea matInput rows="2" formControlName="remarks"></textarea>
              </mat-form-field>

              <button mat-flat-button color="primary" type="submit">Transfer stock</button>
            </form>
          </div>
        </article>
      </section>
    </div>
  `
})
export class InventoryPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationsService);
  private readonly referenceData = inject(ReferenceDataService);

  readonly inventoryMovementTypeOptions = inventoryMovementTypeOptions;
  branches: LookupOption[] = [];
  products: LookupOption[] = [];
  lowStock: LowStockItemDto[] = [];
  balances: PagedResult<StockBalanceDto> = { items: [], pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0 };
  movements: PagedResult<InventoryMovementDto> = { items: [], pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0 };

  readonly adjustmentForm = this.fb.group({
    branchId: ['', [Validators.required]],
    productId: ['', [Validators.required]],
    quantityDifference: [0, [Validators.required]],
    unitCost: [0, [Validators.required]],
    remarks: ['']
  });

  readonly transferForm = this.fb.group({
    fromBranchId: ['', [Validators.required]],
    toBranchId: ['', [Validators.required]],
    productId: ['', [Validators.required]],
    quantity: [0, [Validators.required]],
    remarks: ['']
  });

  constructor() {
    forkJoin({
      branches: this.referenceData.getBranches(),
      products: this.referenceData.getProducts()
    }).subscribe({
      next: ({ branches, products }) => {
        this.branches = branches;
        this.products = products;
      }
    });

    this.loadBalances(1);
    this.loadMovements(1);
    this.loadLowStock();
  }

  loadBalances(pageNumber = 1): void {
    this.api.get<PagedResult<StockBalanceDto>>('inventory/balances', { pageNumber, pageSize: 10 }).subscribe({
      next: (result) => this.balances = result
    });
  }

  loadMovements(pageNumber = 1): void {
    this.api.get<PagedResult<InventoryMovementDto>>('inventory/movements', { pageNumber, pageSize: 10 }).subscribe({
      next: (result) => this.movements = result
    });
  }

  loadLowStock(): void {
    this.api.get<LowStockItemDto[]>('inventory/low-stock').subscribe({
      next: (items) => this.lowStock = items
    });
  }

  postAdjustment(): void {
    if (this.adjustmentForm.invalid) {
      this.adjustmentForm.markAllAsTouched();
      return;
    }

    const request = this.adjustmentForm.getRawValue();
    this.api.post<void>('inventory/adjustments', request).subscribe({
      next: () => {
        this.notifications.success('Stock adjustment posted successfully.');
        this.loadBalances(this.balances.pageNumber);
        this.loadMovements(this.movements.pageNumber);
        this.loadLowStock();
      },
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to post the stock adjustment.')
    });
  }

  postTransfer(): void {
    if (this.transferForm.invalid) {
      this.transferForm.markAllAsTouched();
      return;
    }

    this.api.post<void>('inventory/transfers', this.transferForm.getRawValue()).subscribe({
      next: () => {
        this.notifications.success('Stock transfer posted successfully.');
        this.loadBalances(this.balances.pageNumber);
        this.loadMovements(this.movements.pageNumber);
        this.loadLowStock();
      },
      error: (error) => this.notifications.error(error?.error?.detail ?? 'Unable to post the stock transfer.')
    });
  }
}
