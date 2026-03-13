import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3500,
      panelClass: ['success-snackbar']
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 4500
    });
  }
}
