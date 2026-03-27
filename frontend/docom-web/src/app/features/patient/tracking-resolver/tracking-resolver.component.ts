import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TrackingService } from '../../../core/services/tracking.service';
import { TokenStorageService } from '../../../core/services/token-storage.service';

@Component({
  selector: 'app-tracking-resolver',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    <div style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;gap:16px">
      @if (error()) {
        <p style="color:#dc2626;font-size:16px">Token link not found or expired.</p>
        <a href="/" style="color:#1a73e8">Go to home</a>
      } @else {
        <mat-spinner diameter="40" />
        <p style="color:#6b7280;font-size:14px">Loading your token...</p>
      }
    </div>
  `
})
export class TrackingResolverComponent implements OnInit {
  private route        = inject(ActivatedRoute);
  private router       = inject(Router);
  private tracking     = inject(TrackingService);
  private tokenStorage = inject(TokenStorageService);

  error = () => this._error;
  private _error = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('publicTokenId') ?? '';
    this.tracking.resolve(id).subscribe({
      next: (result) => {
        this.tokenStorage.save(result.doctorSlug, result.sessionId, result.tokenNumber);
        this.router.navigate(['/', result.doctorSlug], { replaceUrl: true });
      },
      error: () => { this._error = true; }
    });
  }
}
