export type TokenStatus = 'Waiting' | 'Serving' | 'Skipped' | 'Completed';

export interface TokenDto {
  id: number;
  tokenNumber: number;
  patientName: string | null;
  phoneNumber: string | null;
  status: TokenStatus;
  queueOrder: number;
  createdAt: string;
}

export interface TakeTokenResponse {
  tokenId: number;
  tokenNumber: number;
  currentTokenNumber: number;
  queuePosition: number;
  estimatedWaitMinutes: number;
  publicTokenId: string;
}
