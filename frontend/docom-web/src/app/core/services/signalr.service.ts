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

  readonly queueAdvanced$ = new Subject<QueueAdvancedEvent>();
  readonly tokenCreated$ = new Subject<TokenCreatedEvent>();
  readonly tokenSkipped$ = new Subject<{ skippedToken: number; currentToken: number }>();
  readonly sessionChanged$ = new Subject<SessionChangedEvent>();
  readonly connected$ = new Subject<boolean>();

  constructor(private authService: AuthService) {}

  async connectToDoctor(slug: string): Promise<void> {
    if (this.connection && this.currentGroup === slug) return;

    await this.disconnect();

    const token = this.authService.getToken();
    const builder = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/hubs/queue`, {
        accessTokenFactory: () => token ?? '',
        transport: signalR.HttpTransportType.WebSockets |
                   signalR.HttpTransportType.ServerSentEvents |
                   signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning);

    this.connection = builder.build();

    this.connection.on('QueueAdvanced', (data: QueueAdvancedEvent) => {
      this.queueAdvanced$.next(data);
    });

    this.connection.on('TokenCreated', (data: TokenCreatedEvent) => {
      this.tokenCreated$.next(data);
    });

    this.connection.on('TokenSkipped', (data: any) => {
      this.tokenSkipped$.next(data);
    });

    this.connection.on('SessionChanged', (data: SessionChangedEvent) => {
      this.sessionChanged$.next(data);
    });

    this.connection.onreconnected(() => {
      if (this.currentGroup) {
        this.connection?.invoke('JoinDoctorQueue', this.currentGroup);
      }
      this.connected$.next(true);
    });

    this.connection.onclose(() => this.connected$.next(false));

    await this.connection.start();
    await this.connection.invoke('JoinDoctorQueue', slug);
    this.currentGroup = slug;
    this.connected$.next(true);
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      if (this.currentGroup) {
        await this.connection.invoke('LeaveDoctorQueue', this.currentGroup).catch(() => {});
      }
      await this.connection.stop().catch(() => {});
      this.connection = null;
      this.currentGroup = null;
    }
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
