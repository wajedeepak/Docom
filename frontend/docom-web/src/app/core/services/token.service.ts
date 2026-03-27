import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TokenDto, TakeTokenResponse } from '../models/token.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TokenService {
  constructor(private http: HttpClient) {}

  takeToken(slug: string, patientName?: string, phoneNumber?: string): Observable<TakeTokenResponse> {
    return this.http.post<TakeTokenResponse>(
      `${environment.apiUrl}/doctors/${slug}/tokens`,
      { patientName: patientName || null, phoneNumber: phoneNumber || null }
    );
  }

  next(sessionId: number): Observable<TokenDto> {
    return this.http.post<TokenDto>(
      `${environment.apiUrl}/sessions/${sessionId}/tokens/next`, {}
    );
  }

  skip(sessionId: number): Observable<TokenDto> {
    return this.http.post<TokenDto>(
      `${environment.apiUrl}/sessions/${sessionId}/tokens/skip`, {}
    );
  }

  addWalkIn(sessionId: number, patientName?: string, phoneNumber?: string): Observable<TakeTokenResponse> {
    return this.http.post<TakeTokenResponse>(
      `${environment.apiUrl}/sessions/${sessionId}/tokens/walkin`,
      { patientName: patientName || null, phoneNumber: phoneNumber || null }
    );
  }

  getStatus(sessionId: number, tokenId: number): Observable<TokenDto> {
    return this.http.get<TokenDto>(
      `${environment.apiUrl}/sessions/${sessionId}/tokens/${tokenId}`
    );
  }
}
