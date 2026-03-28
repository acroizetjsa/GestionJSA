import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { ApiService } from '../services/api.service';

export interface LoginPayload {
  username: string;
  password: string;
}

interface LoginResponse {
  token: string;
  userId: number;
  username: string;
  fullName: string;
  roleCode: string;
}

export interface AuthSession {
  token: string;
  userId: number;
  username: string;
  fullName: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly storageKey = 'gmao.session';

  readonly session = signal<AuthSession | null>(this.readSession());

  login(payload: LoginPayload) {
    return this.api.post<LoginResponse>('/auth/login', payload).pipe(
      tap(response => {
        const session: AuthSession = {
          token: response.token,
          userId: response.userId,
          username: response.username,
          fullName: response.fullName,
          role: response.roleCode
        };
        localStorage.setItem(this.storageKey, JSON.stringify(session));
        this.session.set(session);
      })
    );
  }

  logout() {
    localStorage.removeItem(this.storageKey);
    this.session.set(null);
    this.router.navigateByUrl('/login');
  }

  token(): string | null {
    return this.session()?.token ?? null;
  }

  role(): string | null {
    return this.session()?.role ?? null;
  }

  isAuthenticated(): boolean {
    return this.token() !== null;
  }

  private readSession(): AuthSession | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      return raw ? (JSON.parse(raw) as AuthSession) : null;
    } catch {
      return null;
    }
  }
}
