import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'erp-page-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="page-header">
      <div>
        <div class="page-kicker">{{ kicker }}</div>
        <h1>{{ title }}</h1>
        <p *ngIf="subtitle">{{ subtitle }}</p>
      </div>
      <div class="page-actions">
        <ng-content />
      </div>
    </header>
  `,
  styles: [`
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 20px;
      margin-bottom: 20px;
    }

    .page-kicker {
      text-transform: uppercase;
      letter-spacing: 0.16em;
      font-size: 11px;
      color: var(--erp-muted);
      margin-bottom: 8px;
    }

    h1 {
      margin: 0;
      font-size: 30px;
      line-height: 1.1;
    }

    p {
      margin: 10px 0 0;
      max-width: 720px;
      color: var(--erp-muted);
      line-height: 1.6;
    }

    .page-actions {
      display: flex;
      gap: 12px;
      align-items: center;
    }
  `]
})
export class PageHeaderComponent {
  @Input({ required: true }) title = '';
  @Input() subtitle = '';
  @Input() kicker = 'Workspace';
}
