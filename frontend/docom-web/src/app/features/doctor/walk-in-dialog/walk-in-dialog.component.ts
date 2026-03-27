import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TokenService } from '../../../core/services/token.service';
import { TakeTokenResponse } from '../../../core/models/token.model';

export interface WalkInDialogData {
  sessionId: number;
}

/** Returned to the opener so it can refresh the queue */
export interface WalkInDoneResult {
  tokenNumber: number;
}

@Component({
  selector: 'app-walk-in-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatDialogModule,
    MatFormFieldModule, MatInputModule,
    MatIconModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './walk-in-dialog.component.html',
  styleUrls: ['./walk-in-dialog.component.scss']
})
export class WalkInDialogComponent {
  private dialogRef   = inject(MatDialogRef<WalkInDialogComponent>);
  private data        = inject<WalkInDialogData>(MAT_DIALOG_DATA);
  private tokenService = inject(TokenService);

  patientName = '';
  phoneNumber = '';

  phase   = signal<'form' | 'loading' | 'success'>('form');
  result  = signal<TakeTokenResponse | null>(null);
  errMsg  = signal<string | null>(null);

  get trackingUrl(): string {
    const r = this.result();
    return r ? `https://docom.in/t/${r.publicTokenId}` : '';
  }

  get qrUrl(): string {
    return `https://api.qrserver.com/v1/create-qr-code/?size=180x180&margin=8&data=${encodeURIComponent(this.trackingUrl)}`;
  }

  add(): void {
    this.errMsg.set(null);
    this.phase.set('loading');
    this.tokenService.addWalkIn(
      this.data.sessionId,
      this.patientName.trim() || undefined,
      this.phoneNumber.trim() || undefined
    ).subscribe({
      next: (r) => {
        this.result.set(r);
        this.phase.set('success');
      },
      error: (err) => {
        this.errMsg.set(err?.error?.error ?? 'Could not add walk-in. Please try again.');
        this.phase.set('form');
      }
    });
  }

  copyUrl(): void {
    navigator.clipboard.writeText(this.trackingUrl).catch(() => {});
  }

  close(): void {
    const r = this.result();
    this.dialogRef.close(r ? ({ tokenNumber: r.tokenNumber } as WalkInDoneResult) : undefined);
  }
}
