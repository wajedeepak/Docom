import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TrackingResolveResult {
  doctorSlug: string;
  sessionId: number;
  tokenNumber: number;
}

@Injectable({ providedIn: 'root' })
export class TrackingService {
  constructor(private http: HttpClient) {}

  resolve(publicTokenId: string): Observable<TrackingResolveResult> {
    return this.http.get<TrackingResolveResult>(`${environment.apiUrl}/t/${publicTokenId}`);
  }
}
