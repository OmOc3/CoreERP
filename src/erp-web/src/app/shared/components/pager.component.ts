import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ErpMaterialModule } from '../material';

@Component({
  selector: 'erp-pager',
  standalone: true,
  imports: [CommonModule, ErpMaterialModule],
  template: `
    <div class="pager" *ngIf="totalPages > 1">
      <button mat-stroked-button type="button" (click)="previous.emit()" [disabled]="pageNumber <= 1">Previous</button>
      <span>Page {{ pageNumber }} of {{ totalPages }}</span>
      <button mat-stroked-button type="button" (click)="next.emit()" [disabled]="pageNumber >= totalPages">Next</button>
    </div>
  `,
  styles: [`
    .pager {
      display: flex;
      justify-content: flex-end;
      align-items: center;
      gap: 12px;
      margin-top: 16px;
      color: var(--erp-muted);
      font-size: 13px;
    }
  `]
})
export class PagerComponent {
  @Input() pageNumber = 1;
  @Input() totalPages = 1;
  @Output() previous = new EventEmitter<void>();
  @Output() next = new EventEmitter<void>();
}
