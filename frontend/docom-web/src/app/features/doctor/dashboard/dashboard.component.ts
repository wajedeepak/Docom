import {
  Component, OnInit, OnDestroy, signal, inject, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { SessionService } from '../../../core/services/session.service';
import { TokenService } from '../../../core/services/token.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { SessionDto, QueueStateDto, SessionStatus } from '../../../core/models/session.model';
import { TokenDto } from '../../../core/models/token.model';
import { NewSessionDialogComponent } from '../new-session-dialog/new-session-dialog.component';
import { WalkInDialogComponent, WalkInDoneResult } from '../walk-in-dialog/walk-in-dialog.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatIconModule, MatCardModule, MatChipsModule,
    MatDividerModule, MatSnackBarModule, MatDialogModule,
    MatProgressSpinnerModule, MatTooltipModule, MatMenuModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  private sessionService = inject(SessionService);
  private tokenService = inject(TokenService);
  private signalR = inject(SignalRService);
  private auth = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private router = inject(Router);

  user = this.auth.user;
  session = signal<SessionDto | null>(null);
  queueState = signal<QueueStateDto | null>(null);
  loading = signal(true);
  actionLoading = signal<string | null>(null);

  currentToken = computed(() => this.queueState()?.currentTokenNumber ?? 0);
  waitingCount = computed(() => this.queueState()?.waitingCount ?? 0);
  sessionStatus = computed(() => this.session()?.status ?? 'NoSession' as SessionStatus);

  canStart = computed(() => this.session()?.status === 'Created');
  canPause = computed(() => this.session()?.status === 'Active');
  canResume = computed(() => this.session()?.status === 'Paused');
  canEnd = computed(() => ['Active', 'Paused'].includes(this.session()?.status ?? ''));
  canNext = computed(() => this.session()?.status === 'Active');
  canSkip = computed(() => this.session()?.status === 'Active' && this.currentToken() > 0);
  canWalkIn = computed(() => this.session()?.status === 'Active');

  private subs = new Subscription();

  ngOnInit(): void {
    this.loadActiveSession();
    this.connectSignalR();
  }

  private loadActiveSession(): void {
    this.sessionService.getActive().subscribe({
      next: (s) => {
        this.session.set(s);
        if (s && s.status !== 'Ended') {
          this.loadQueueState(s.id);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadQueueState(sessionId: number): void {
    this.sessionService.getQueueState(sessionId).subscribe({
      next: (q) => this.queueState.set(q)
    });
  }

  private connectSignalR(): void {
    const slug = this.user()?.doctorSlug;
    if (!slug) return;

    this.signalR.connectToDoctor(slug).catch(() => {});

    this.subs.add(
      this.signalR.queueAdvanced$.subscribe(() => {
        const s = this.session();
        if (s) this.loadQueueState(s.id);
      })
    );

    this.subs.add(
      this.signalR.tokenCreated$.subscribe(() => {
        const s = this.session();
        if (s) this.loadQueueState(s.id);
      })
    );
  }

  openNewSession(): void {
    const ref = this.dialog.open(NewSessionDialogComponent, { width: '360px' });
    ref.afterClosed().subscribe((label: string | null) => {
      if (!label) return;
      this.doAction('creating', () => this.sessionService.create(label), (s) => {
        this.session.set(s);
        this.queueState.set(null);
      });
    });
  }

  startSession(): void {
    const id = this.session()?.id;
    if (!id) return;
    this.doAction('starting', () => this.sessionService.start(id), s => this.session.set(s));
  }

  pauseSession(): void {
    const id = this.session()?.id;
    if (!id) return;
    this.doAction('pausing', () => this.sessionService.pause(id), s => this.session.set(s));
  }

  resumeSession(): void {
    const id = this.session()?.id;
    if (!id) return;
    this.doAction('resuming', () => this.sessionService.resume(id), s => this.session.set(s));
  }

  endSession(): void {
    const id = this.session()?.id;
    if (!id) return;
    this.doAction('ending', () => this.sessionService.end(id), s => {
      this.session.set(s);
      this.queueState.set(null);
    });
  }

  nextPatient(): void {
    const id = this.session()?.id;
    if (!id) return;
    this.doAction('next', () => this.tokenService.next(id), () => {
      this.loadQueueState(id);
    });
  }

  skipPatient(): void {
    const id = this.session()?.id;
    if (!id) return;
    this.doAction('skip', () => this.tokenService.skip(id), () => {
      this.loadQueueState(id);
    });
  }

  addWalkIn(): void {
    const id = this.session()?.id;
    if (!id) return;
    const ref = this.dialog.open(WalkInDialogComponent, {
      width: '380px',
      data: { sessionId: id }
    });
    ref.afterClosed().subscribe((result: WalkInDoneResult | undefined) => {
      if (result) this.loadQueueState(id);
    });
  }

  logout(): void {
    this.auth.logout();
  }

  getStatusColor(status: string): string {
    const map: Record<string, string> = {
      Waiting: 'waiting',
      Serving: 'serving',
      Skipped: 'skipped',
      Completed: 'completed'
    };
    return map[status] ?? '';
  }

  private doAction<T>(
    key: string,
    action: () => import('rxjs').Observable<T>,
    onSuccess: (result: T) => void
  ): void {
    this.actionLoading.set(key);
    action().subscribe({
      next: (result) => {
        onSuccess(result);
        this.actionLoading.set(null);
      },
      error: (err) => {
        const msg = err?.error?.error ?? 'Action failed. Please try again.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
        this.actionLoading.set(null);
      }
    });
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.signalR.disconnect();
  }
}
