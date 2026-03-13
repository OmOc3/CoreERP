import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ErpMaterialModule } from '../../shared/material';

@Component({
  selector: 'erp-forbidden-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ErpMaterialModule],
  template: `
    <div class="forbidden-shell">
      <section class="erp-card forbidden-card">
        <div class="erp-section">
          <div class="badge">403</div>
          <h1>Access denied</h1>
          <p>
            Your account is authenticated, but it does not currently have permission to access
            this part of the ERP. Switch to an authorized role or return to the dashboard.
          </p>
          <div class="erp-actions">
            <a mat-flat-button color="primary" routerLink="/dashboard">Back to dashboard</a>
            <a mat-stroked-button routerLink="/login">Sign in as another user</a>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .forbidden-shell {
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 24px;
    }

    .forbidden-card {
      width: min(560px, 100%);
      text-align: center;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 72px;
      height: 72px;
      border-radius: 20px;
      background: rgba(164, 50, 50, 0.12);
      color: var(--erp-danger);
      font-size: 24px;
      font-weight: 700;
      margin-bottom: 16px;
    }

    h1 {
      margin: 0 0 10px;
      font-size: 34px;
    }

    p {
      margin: 0 auto 20px;
      max-width: 420px;
      color: var(--erp-muted);
      line-height: 1.7;
    }

    .erp-actions {
      justify-content: center;
    }
  `]
})
export class ForbiddenPageComponent {}
