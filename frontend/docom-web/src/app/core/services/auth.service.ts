import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AuthResponse, CurrentUser } from '../models/auth.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly STORAGE_KEY = 'docom_user';
  private readonly _user = signal<CurrentUser | null>(this.loadFromStorage());

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.role === 'Admin');
  readonly isDoctor = computed(() => this._user()?.role === 'Doctor' || this._user()?.role === 'Receptionist');

  constructor(private http: HttpClient, private router: Router) {}

  requestOtp(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${environment.apiUrl}/auth/request-otp`,
      { email }
    );
  }

  verifyOtp(email: string, otp: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/verify-otp`,
      { email, otp }
    ).pipe(
      tap(response => {
        const user: CurrentUser = { ...response };
        this._user.set(user);
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
      })
    );
  }

  loginWithPassword(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/login-password`,
      { email, password }
    ).pipe(
      tap(response => {
        const user: CurrentUser = { ...response };
        this._user.set(user);
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
      })
    );
  }

  setPassword(email: string, password: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${environment.apiUrl}/auth/set-password`,
      { email, password }
    );
  }

  requestPasswordReset(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${environment.apiUrl}/auth/request-password-reset`,
      { email }
    );
  }

  resetPasswordWithOtp(email: string, resetCode: string, newPassword: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/reset-password-with-otp`,
      { email, resetCode, newPassword }
    ).pipe(
      tap(response => {
        const user: CurrentUser = { ...response };
        this._user.set(user);
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
      })
    );
  }

  logout(): void {
    this._user.set(null);
    localStorage.removeItem(this.STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this._user()?.token ?? null;
  }

  private loadFromStorage(): CurrentUser | null {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }
}
