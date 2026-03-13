import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { shellComponent } from './core/layout/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login-page.component').then((m) => m.LoginPageComponent)
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/auth/forbidden-page.component').then((m) => m.ForbiddenPageComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    component: shellComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard-page.component').then((m) => m.DashboardPageComponent)
      },
      {
        path: 'master/:entity',
        loadComponent: () => import('./features/master-data/master-data-page.component').then((m) => m.MasterDataPageComponent)
      },
      {
        path: 'transactions/purchase-orders',
        loadComponent: () => import('./features/transactions/purchase-orders-page.component').then((m) => m.PurchaseOrdersPageComponent)
      },
      {
        path: 'transactions/sales-orders',
        loadComponent: () => import('./features/transactions/sales-orders-page.component').then((m) => m.SalesOrdersPageComponent)
      },
      {
        path: 'transactions/invoices',
        loadComponent: () => import('./features/transactions/invoices-page.component').then((m) => m.InvoicesPageComponent)
      },
      {
        path: 'transactions/payments',
        loadComponent: () => import('./features/transactions/payments-page.component').then((m) => m.PaymentsPageComponent)
      },
      {
        path: 'transactions/returns',
        loadComponent: () => import('./features/transactions/returns-page.component').then((m) => m.ReturnsPageComponent)
      },
      {
        path: 'transactions/inventory',
        loadComponent: () => import('./features/transactions/inventory-page.component').then((m) => m.InventoryPageComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports-page.component').then((m) => m.ReportsPageComponent)
      },
      {
        path: 'admin/users',
        loadComponent: () => import('./features/admin/users-page.component').then((m) => m.UsersPageComponent)
      },
      {
        path: 'admin/roles',
        loadComponent: () => import('./features/admin/roles-page.component').then((m) => m.RolesPageComponent)
      },
      {
        path: 'admin/audit-logs',
        loadComponent: () => import('./features/admin/audit-logs-page.component').then((m) => m.AuditLogsPageComponent)
      },
      {
        path: 'admin/alerts',
        loadComponent: () => import('./features/admin/alerts-page.component').then((m) => m.AlertsPageComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
