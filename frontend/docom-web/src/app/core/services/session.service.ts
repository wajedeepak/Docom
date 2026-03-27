import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SessionDto, QueueStateDto, PublicQueueStateDto } from '../models/session.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly base = `${environment.apiUrl}/sessions`;

  constructor(private http: HttpClient) {}

  create(label: string): Observable<SessionDto> {
    return this.http.post<SessionDto>(this.base, { label });
  }

  getActive(): Observable<SessionDto | null> {
    return this.http.get<SessionDto | null>(`${this.base}/active`);
  }

  start(sessionId: number): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.base}/${sessionId}/start`, {});
  }

  pause(sessionId: number): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.base}/${sessionId}/pause`, {});
  }

  resume(sessionId: number): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.base}/${sessionId}/resume`, {});
  }

  end(sessionId: number): Observable<SessionDto> {
    return this.http.post<SessionDto>(`${this.base}/${sessionId}/end`, {});
  }

  getQueueState(sessionId: number): Observable<QueueStateDto> {
    return this.http.get<QueueStateDto>(`${this.base}/${sessionId}/queue`);
  }

  getPublicQueueState(slug: string): Observable<PublicQueueStateDto> {
    return this.http.get<PublicQueueStateDto>(`${environment.apiUrl}/doctors/${slug}/queue`);
  }
}
