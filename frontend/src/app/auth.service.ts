import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenKey = 'auth_token';

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  set token(value: string) {
    localStorage.setItem(this.tokenKey, value);
  }

  get isLoggedIn(): boolean {
    return !!this.token;
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
  }

  getAuthHeaders(): Record<string, string> {
    const t = this.token;
    return t ? { Authorization: `Bearer ${t}`, 'Content-Type': 'application/json' } : { 'Content-Type': 'application/json' };
  }
}