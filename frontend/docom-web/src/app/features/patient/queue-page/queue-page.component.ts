import {
  Component, OnInit, OnDestroy, signal, computed, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { interval, Subscription, switchMap } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SessionService } from '../../../core/services/session.service';
import { TokenService } from '../../../core/services/token.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { TokenStorageService } from '../../../core/services/token-storage.service';
import { PublicQueueStateDto } from '../../../core/models/session.model';
import { TakeTokenResponse } from '../../../core/models/token.model';

// 'token-grace' = token was passed but within the 5-token grace window
type PageState = 'loading' | 'no-session' | 'active' | 'paused' | 'token-taken' | 'token-grace';

const GRACE_WINDOW = 5;

@Component({
  selector: 'app-queue-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatProgressSpinnerModule, MatSnackBarModule,
    MatInputModule, MatFormFieldModule, MatIconModule, MatTooltipModule
  ],
  templateUrl: './queue-page.component.html',
  styleUrls: ['./queue-page.component.scss']
})
export class QueuePageComponent implements OnInit, OnDestroy {
  private route          = inject(ActivatedRoute);
  private sessionService = inject(SessionService);
  private tokenService   = inject(TokenService);
  private signalR        = inject(SignalRService);
  private tokenStorage   = inject(TokenStorageService);
  private snackBar       = inject(MatSnackBar);

  slug          = '';
  pageState     = signal<PageState>('loading');
  queueState    = signal<PublicQueueStateDto | null>(null);
  myToken       = signal<TakeTokenResponse | null>(null);
  patientName   = '';
  patientPhone  = '';
  isTakingToken = signal(false);
  isRefreshing  = signal(false);

  // Shown above take-token form when a stored token has expired
  expiredMessage = signal<string | null>(null);
  trackingUrl    = signal<string | null>(null);

  myPosition = computed(() => {
    const token = this.myToken();
    const queue = this.queueState();
    if (!token || !queue) return null;
    if (token.tokenNumber <= queue.currentTokenNumber) return 0;
    return token.tokenNumber - queue.currentTokenNumber;
  });

  private subs         = new Subscription();
  private pollInterval = 2_000;  // 2 seconds

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.loadQueueState();
    this.startPolling();
    this.connectSignalR();
  }

  // ─── Queue state ────────────────────────────────────────────────────────────

  private loadQueueState(): void {
    this.isRefreshing.set(true);
    this.sessionService.getPublicQueueState(this.slug).subscribe({
      next: (state) => {
        this.queueState.set(state);
        this.resolvePageState(state);
        this.isRefreshing.set(false);
      },
      error: () => {
        this.pageState.set('no-session');
        this.isRefreshing.set(false);
      }
    });
  }

  /**
   * Single function that decides which UI state to show.
   * Called on: initial load, every poll tick, every SignalR queue event.
   */
  private resolvePageState(state: PublicQueueStateDto): void {
    // Session ended/inactive — clear any stored token and show no-session
    if (!state.sessionId || state.sessionStatus === 'Ended' || state.sessionStatus === 'NoSession') {
      if (state.sessionId) this.tokenStorage.clear(this.slug, state.sessionId);
      this.myToken.set(null);
      this.pageState.set('no-session');
      return;
    }

    const stored = this.tokenStorage.load(this.slug, state.sessionId);

    if (stored) {
      const diff = state.currentTokenNumber - stored.tokenNumber;

      if (diff > GRACE_WINDOW) {
        // Token expired — clear storage, show message, return to take-token
        this.tokenStorage.clear(this.slug, state.sessionId);
        this.myToken.set(null);
        this.expiredMessage.set(
          'Your previous token is no longer active. You can take a new token if the session is still open.'
        );
        this.pageState.set(state.sessionStatus === 'Active' ? 'active' : 'paused');
        return;
      }

      // Restore token into component state
      this.myToken.set({
        tokenId:             0,
        tokenNumber:         stored.tokenNumber,
        currentTokenNumber:  state.currentTokenNumber,
        queuePosition:       Math.max(0, stored.tokenNumber - state.currentTokenNumber),
        estimatedWaitMinutes: Math.max(0, stored.tokenNumber - state.currentTokenNumber) * 5,
        publicTokenId:       ''
      });

      // diff > 0 means turn passed but within grace window
      this.pageState.set(diff > 0 ? 'token-grace' : 'token-taken');
      return;
    }

    // No stored token — show state based on session status
    switch (state.sessionStatus) {
      case 'Active': this.pageState.set('active'); break;
      case 'Paused': this.pageState.set('paused'); break;
      default:       this.pageState.set('no-session');
    }
  }

  // ─── Polling ────────────────────────────────────────────────────────────────

  private startPolling(): void {
    const sub = interval(this.pollInterval)
      .pipe(switchMap(() => this.sessionService.getPublicQueueState(this.slug)))
      .subscribe({
        next: (state) => {
          this.queueState.set(state);
          this.resolvePageState(state);
        }
      });
    this.subs.add(sub);
  }

  // ─── SignalR ─────────────────────────────────────────────────────────────────

  private connectSignalR(): void {
    this.signalR.connectToDoctor(this.slug).catch(() => {});

    this.subs.add(
      this.signalR.queueAdvanced$.subscribe(event => {
        this.queueState.update(s => s ? {
          ...s,
          currentTokenNumber:   event.currentToken,
          waitingCount:         event.waitingCount,
          estimatedWaitMinutes: event.estimatedWaitMinutes
        } : s);
        const updated = this.queueState();
        if (updated) this.resolvePageState(updated);
      })
    );

    this.subs.add(
      this.signalR.sessionChanged$.subscribe(() => this.loadQueueState())
    );
  }

  // ─── Actions ─────────────────────────────────────────────────────────────────

  takeToken(): void {
    if (this.isTakingToken()) return;
    this.isTakingToken.set(true);
    this.expiredMessage.set(null);

    this.tokenService.takeToken(this.slug, this.patientName || undefined, this.patientPhone || undefined).subscribe({
      next: (result) => {
        const sessionId = this.queueState()?.sessionId;
        if (sessionId) {
          this.tokenStorage.save(this.slug, sessionId, result.tokenNumber);
        }
        this.myToken.set(result);
        this.pageState.set('token-taken');
        this.trackingUrl.set(`https://docom.in/t/${result.publicTokenId}`);
        this.isTakingToken.set(false);
      },
      error: (err) => {
        const msg = err?.error?.error ?? 'Could not take token. Please try again.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
        this.isTakingToken.set(false);
      }
    });
  }

  copyTrackingUrl(): void {
    const url = this.trackingUrl();
    if (url) navigator.clipboard.writeText(url).catch(() => {});
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────────

  get waitLabel(): string {
    const mins = this.queueState()?.estimatedWaitMinutes ?? 0;
    if (mins < 5)  return 'Very soon';
    if (mins < 60) return `~${mins} min`;
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return m === 0 ? `~${h}h` : `~${h}h ${m}min`;
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.signalR.disconnect();
  }
}
