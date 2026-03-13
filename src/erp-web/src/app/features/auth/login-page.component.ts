import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ErpMaterialModule } from '../../shared/material';
import { AuthService } from '../../core/services/auth.service';
import { NotificationsService } from '../../core/services/notifications.service';

@Component({
  selector: 'erp-login-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ErpMaterialModule],
  template: `
    <div class="login-shell">
      <section class="login-copy">
        <div class="login-kicker">CoreERP</div>
        <h1>Business operations, approvals, inventory, and finance in one workspace.</h1>
        <p>
          Sign in with one of the seeded accounts to manage purchasing, sales, stock valuation,
          low stock alerts, branch-restricted workflows, and enterprise audit trails.
        </p>

        <div class="login-demo erp-card">
          <div class="erp-section">
            <strong>Seeded demo accounts</strong>
            <div class="demo-row"><span>Admin</span><code>admin / Admin&#64;123</code></div>
            <div class="demo-row"><span>Manager</span><code>manager / Manager&#64;123</code></div>
            <div class="demo-row"><span>Branch User</span><code>branch.user / Branch&#64;123</code></div>
          </div>
        </div>
      </section>

      <section class="login-panel erp-card">
        <div class="erp-section">
          <h2>Welcome back</h2>
          <p class="muted">Use your ERP credentials to continue.</p>

          <form [formGroup]="form" (ngSubmit)="submit()" class="erp-form-grid">
            <mat-form-field appearance="outline">
              <mat-label>Username or email</mat-label>
              <input matInput formControlName="userNameOrEmail" />
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Password</mat-label>
              <input matInput type="password" formControlName="password" />
            </mat-form-field>

            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || submitting">
              {{ submitting ? 'Signing in...' : 'Sign in' }}
            </button>
          </form>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .login-shell {
      min-height: 100vh;
      display: grid;
      grid-template-columns: minmax(0, 1.2fr) minmax(360px, 460px);
      gap: 24px;
      padding: 40px;
      align-items: center;
    }

    .login-copy h1 {
      font-size: 52px;
      line-height: 1.02;
      max-width: 760px;
      margin: 10px 0 16px;
    }

    .login-kicker {
      text-transform: uppercase;
      letter-spacing: 0.18em;
      font-size: 12px;
      color: var(--erp-accent);
      font-weight: 700;
    }

    .login-copy p,
    .muted {
      color: var(--erp-muted);
      line-height: 1.7;
      max-width: 680px;
    }

    .login-panel h2 {
      margin: 0 0 8px;
      font-size: 30px;
    }

    .login-demo {
      margin-top: 28px;
      max-width: 520px;
    }

    .demo-row {
      display: flex;
      justify-content: space-between;
      gap: 14px;
      padding: 10px 0;
      border-bottom: 1px solid rgba(97, 112, 103, 0.16);
    }

    .demo-row:last-child {
      border-bottom: 0;
    }

    code {
      background: rgba(17, 75, 95, 0.08);
      padding: 3px 8px;
      border-radius: 999px;
      font-family: Consolas, monospace;
    }

    @media (max-width: 960px) {
      .login-shell {
        grid-template-columns: 1fr;
        padding: 24px;
      }

      .login-copy h1 {
        font-size: 38px;
      }
    }
  `]
})
export class LoginPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  submitting = false;

  readonly form = this.fb.group({
    userNameOrEmail: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const value = this.form.getRawValue();
    this.auth.login(value.userNameOrEmail ?? '', value.password ?? '').subscribe({
      next: () => {
        this.submitting = false;
        this.notifications.success('Signed in successfully.');
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (error) => {
        this.submitting = false;
        this.notifications.error(error?.error?.detail ?? 'Unable to sign in with the provided credentials.');
      }
    });
  }
}
