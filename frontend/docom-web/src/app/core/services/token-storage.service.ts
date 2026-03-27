import { Injectable } from '@angular/core';

export interface StoredToken {
  tokenNumber: number;
  sessionId: number;
  doctorSlug: string;
  timestamp: number;
}

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private key(slug: string, sessionId: number): string {
    return `docom_token_${slug}_${sessionId}`;
  }

  save(slug: string, sessionId: number, tokenNumber: number): void {
    const value: StoredToken = {
      tokenNumber,
      sessionId,
      doctorSlug: slug,
      timestamp: Date.now()
    };
    localStorage.setItem(this.key(slug, sessionId), JSON.stringify(value));
  }

  load(slug: string, sessionId: number): StoredToken | null {
    try {
      const raw = localStorage.getItem(this.key(slug, sessionId));
      return raw ? (JSON.parse(raw) as StoredToken) : null;
    } catch {
      return null;
    }
  }

  clear(slug: string, sessionId: number): void {
    localStorage.removeItem(this.key(slug, sessionId));
  }
}
