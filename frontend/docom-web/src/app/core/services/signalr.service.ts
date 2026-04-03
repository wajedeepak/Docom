import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface QueueAdvancedEvent {
  currentToken: number;
  waitingCount: number;
  estimatedWaitMinutes: number;
}

export interface TokenCreatedEvent {
  tokenNumber: number;
  queuePosition: number;
}

export interface SessionChangedEvent {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private connection: signalR.HubConnection | null = null;
  private currentGroup: string | null = null;
  private reconnectTimeout: any = null;

  readonly queueAdvanced$ = new Subject<QueueAdvancedEvent>();
  readonly tokenCreated$ = new Subject<TokenCreatedEvent>();
  readonly tokenSkipped$ = new Subject<{ skippedToken: number; currentToken: number }>();
  readonly sessionChanged$ = new Subject<SessionChangedEvent>();
  readonly connected$ = new Subject<boolean>();

  constructor(private authService: AuthService) {}

  async connectToDoctor(slug: string): Promise<void> {
    if (this.connection && this.currentGroup === slug) {
      console.log('[SignalR] Already connected to doctor:', slug);
      return;
    }

    await this.disconnect();

    try {
      const token = this.authService.getToken();
      if (!token) {
        console.warn('[SignalR] No auth token available. Connect may fail.');
      }

      const hubUrl = `${environment.hubUrl}/hubs/queue`;
      console.log('[SignalR] Connecting to:', hubUrl);

      const builder = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => {
            const currentToken = this.authService.getToken();
            console.log('[SignalR] Using token:', currentToken ? 'yes' : 'no');
            return currentToken ?? '';
          },
          // Explicitly prefer WebSocket first, then fallback
          transport: signalR.HttpTransportType.WebSockets |
                     signalR.HttpTransportType.ServerSentEvents |
                     signalR.HttpTransportType.LongPolling,
          skipNegotiation: false,
          withCredentials: true
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .withKeepAliveInterval(15_000);

      this.connection = builder.build();
      if (!this.connection) {
        throw new Error('[SignalR] Failed to build connection');
      }

      const conn = this.connection;

      conn.on('QueueAdvanced', (data: QueueAdvancedEvent) => {
        console.log('[SignalR] QueueAdvanced received:', data);
        this.queueAdvanced$.next(data);
      });

      conn.on('TokenCreated', (data: TokenCreatedEvent) => {
        console.log('[SignalR] TokenCreated received:', data);
        this.tokenCreated$.next(data);
      });

      conn.on('TokenSkipped', (data: any) => {
        console.log('[SignalR] TokenSkipped received:', data);
        this.tokenSkipped$.next(data);
      });

      conn.on('SessionChanged', (data: SessionChangedEvent) => {
        console.log('[SignalR] SessionChanged received:', data);
        this.sessionChanged$.next(data);
      });

      conn.onreconnecting(() => {
        console.warn('[SignalR] Connection lost. Reconnecting...');
        this.connected$.next(false);
      });

      conn.onreconnected(() => {
        console.log('[SignalR] Reconnected. Rejoining group:', this.currentGroup);
        if (this.currentGroup) {
          conn.invoke('JoinDoctorQueue', this.currentGroup)
            .catch(err => console.error('[SignalR] Failed to rejoin group:', err));
        }
        this.connected$.next(true);
      });

      conn.onclose((error) => {
        console.error('[SignalR] Connection closed:', error);
        this.connected$.next(false);
      });

      await this.connection.start();
      console.log('[SignalR] Connected. Joining group:', slug);

      await this.connection.invoke('JoinDoctorQueue', slug);
      this.currentGroup = slug;
      this.connected$.next(true);
    } catch (err) {
      console.error('[SignalR] Connection failed:', err);
      this.connected$.next(false);
      throw err;
    }
  }

  async disconnect(): Promise<void> {
    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }

    if (this.connection) {
      try {
        if (this.currentGroup) {
          await this.connection.invoke('LeaveDoctorQueue', this.currentGroup).catch(() => {});
        }
        await this.connection.stop();
        console.log('[SignalR] Disconnected');
      } catch (err) {
        console.error('[SignalR] Error during disconnect:', err);
      }
      this.connection = null;
      this.currentGroup = null;
      this.connected$.next(false);
    }
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
