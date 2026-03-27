import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';

const QUICK_LABELS = ['Morning', 'Afternoon', 'Evening'];

@Component({
  selector: 'app-new-session-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatInputModule, MatFormFieldModule,
    MatDialogModule, MatChipsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>New Session</h2>
      <mat-dialog-content>
        <p class="hint">Choose a label for this session.</p>
        <div class="quick-labels">
          @for (label of quickLabels; track label) {
            <button
              mat-stroked-button
              class="label-chip"
              [class.selected]="sessionLabel() === label"
              (click)="sessionLabel.set(label)">
              {{ label }}
            </button>
          }
        </div>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Or enter custom label</mat-label>
          <input matInput [(ngModel)]="customLabel" placeholder="e.g. Session 1"
                 (input)="sessionLabel.set(customLabel)" maxlength="50" />
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button mat-dialog-close>Cancel</button>
        <button mat-flat-button class="btn-create"
          [disabled]="!sessionLabel()"
          (click)="create()">
          Create
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .dialog-container { padding: 8px; min-width: 320px; }
    .hint { font-size: 14px; color: #6b7280; margin: 0 0 12px; }
    .quick-labels { display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 16px; }
    .label-chip { border-radius: 20px !important; font-size: 13px; }
    .label-chip.selected { background: #e8f0fe !important; color: #1a73e8 !important; border-color: #1a73e8 !important; }
    .full-width { width: 100%; }
    .btn-create { background: #1a73e8 !important; color: white !important; border-radius: 8px !important; }
  `]
})
export class NewSessionDialogComponent {
  quickLabels = QUICK_LABELS;
  sessionLabel = signal('Morning');
  customLabel = '';

  constructor(private dialogRef: MatDialogRef<NewSessionDialogComponent>) {}

  create(): void {
    this.dialogRef.close(this.sessionLabel());
  }
}
