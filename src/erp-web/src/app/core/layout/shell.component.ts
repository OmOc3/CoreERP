import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ErpMaterialModule } from '../../shared/material';
import { AuthService } from '../services/auth.service';
import { NotificationsService } from '../services/notifications.service';

interface NavigationGroup {
  title: string;
  items: Array<{ label: string; route: string }>;
}

@Component({
  selector: 'erp-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, ErpMaterialModule],
  template: `
    <mat-sidenav-container class="shell">
      <mat-sidenav opened mode="side" class="shell-nav">
        <div class="brand">
          <div class="brand-kicker">CoreERP</div>
          <h1>Operations Hub</h1>
          <p>Branch-aware purchasing, sales, stock, finance, and admin workflows.</p>
        </div>

        <div class="nav-groups">
          <section *ngFor="let group of navigation">
            <h2>{{ group.title }}</h2>
            <mat-nav-list>
              <a mat-list-item *ngFor="let item of group.items" [routerLink]="item.route" routerLinkActive="active-link">
                {{ item.label }}
              </a>
            </mat-nav-list>
          </section>
        </div>
      </mat-sidenav>

      <mat-sidenav-content>
        <mat-toolbar class="shell-toolbar">
          <div>
            <div class="toolbar-kicker">Internal ERP Workspace</div>
            <div class="toolbar-title">Mini ERP Suite</div>
          </div>

          <div class="toolbar-user">
            <div>
              <strong>{{ currentUser()?.userName ?? 'User' }}</strong>
              <div class="toolbar-meta">{{ currentUser()?.roles?.join(', ') || 'No roles assigned' }}</div>
            </div>
            <button mat-stroked-button type="button" (click)="logout()">Sign out</button>
          </div>
        </mat-toolbar>

        <router-outlet />
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .shell {
      height: 100vh;
      background: transparent;
    }

    .shell-nav {
      width: 290px;
      border-right: 1px solid var(--erp-border);
      background:
        linear-gradient(180deg, rgba(17, 75, 95, 0.92), rgba(25, 106, 124, 0.94)),
        linear-gradient(180deg, #0c3644, #124c5f);
      color: #f2fbff;
      padding: 20px 18px 28px;
    }

    .brand {
      padding: 10px 8px 24px;
      border-bottom: 1px solid rgba(255, 255, 255, 0.16);
      margin-bottom: 16px;
    }

    .brand-kicker,
    .toolbar-kicker {
      text-transform: uppercase;
      letter-spacing: 0.16em;
      font-size: 11px;
      opacity: 0.75;
    }

    .brand h1 {
      margin: 10px 0 8px;
      font-size: 28px;
      line-height: 1.1;
    }

    .brand p {
      margin: 0;
      color: rgba(242, 251, 255, 0.78);
      font-size: 13px;
      line-height: 1.5;
    }

    .nav-groups {
      display: grid;
      gap: 18px;
    }

    .nav-groups h2 {
      margin: 0 0 6px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.16em;
      color: rgba(242, 251, 255, 0.72);
      padding-left: 8px;
    }

    .active-link {
      background: rgba(255, 255, 255, 0.12);
      border-radius: 12px;
    }

    .shell-toolbar {
      position: sticky;
      top: 0;
      z-index: 10;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
      padding: 16px 24px;
      background: rgba(248, 250, 248, 0.92);
      backdrop-filter: blur(10px);
      border-bottom: 1px solid var(--erp-border);
    }

    .toolbar-title {
      font-size: 22px;
      font-weight: 700;
    }

    .toolbar-user {
      display: flex;
      align-items: center;
      gap: 16px;
      text-align: right;
    }

    .toolbar-meta {
      font-size: 12px;
      color: var(--erp-muted);
    }
  `]
})
export class ShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationsService);
  private readonly router = inject(Router);

  readonly currentUser = computed(() => this.auth.user());
  readonly navigation: NavigationGroup[] = [
    { title: 'Overview', items: [{ label: 'Dashboard', route: '/dashboard' }] },
    {
      title: 'Master Data',
      items: [
        { label: 'Products', route: '/master/products' },
        { label: 'Categories', route: '/master/categories' },
        { label: 'Customers', route: '/master/customers' },
        { label: 'Suppliers', route: '/master/suppliers' },
        { label: 'Branches', route: '/master/branches' }
      ]
    },
    {
      title: 'Transactions',
      items: [
        { label: 'Purchase Orders', route: '/transactions/purchase-orders' },
        { label: 'Sales Orders', route: '/transactions/sales-orders' },
        { label: 'Invoices', route: '/transactions/invoices' },
        { label: 'Payments', route: '/transactions/payments' },
        { label: 'Returns', route: '/transactions/returns' },
        { label: 'Inventory', route: '/transactions/inventory' }
      ]
    },
    {
      title: 'Insights',
      items: [
        { label: 'Reports', route: '/reports' },
        { label: 'Audit Logs', route: '/admin/audit-logs' },
        { label: 'Alerts', route: '/admin/alerts' }
      ]
    },
    {
      title: 'Administration',
      items: [
        { label: 'Users', route: '/admin/users' },
        { label: 'Roles & Workflow', route: '/admin/roles' }
      ]
    }
  ];

  ngOnInit(): void {
    if (!this.auth.user()) {
      this.auth.getCurrentUser().subscribe({
        error: () => {
          this.auth.logout(false);
          this.notifications.error('Your session has expired. Please sign in again.');
        }
      });
    }
  }

  logout(): void {
    this.auth.logout();
    this.notifications.success('You have been signed out.');
    void this.router.navigate(['/login']);
  }
}

export const shellComponent = ShellComponent;
