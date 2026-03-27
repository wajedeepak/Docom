import { TokenDto } from './token.model';

export type SessionStatus = 'Created' | 'Active' | 'Paused' | 'Ended' | 'NoSession';

export interface SessionDto {
  id: number;
  label: string;
  status: SessionStatus;
  currentTokenNumber: number;
  lastIssuedTokenNumber: number;
  createdAt: string;
  startedAt: string | null;
  endedAt: string | null;
}

export interface QueueStateDto {
  sessionId: number;
  sessionLabel: string;
  sessionStatus: SessionStatus;
  currentTokenNumber: number;
  lastIssuedTokenNumber: number;
  waitingCount: number;
  estimatedWaitMinutes: number;
  queue: TokenDto[];
}

export interface PublicQueueStateDto {
  sessionId: number | null;
  doctorName: string;
  specialization: string;
  sessionActive: boolean;
  currentTokenNumber: number;
  waitingCount: number;
  estimatedWaitMinutes: number;
  sessionStatus: SessionStatus;
}
