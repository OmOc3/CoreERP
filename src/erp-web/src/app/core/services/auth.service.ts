import { HttpBackend, HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, map, shareReplay, tap, throwError } from 'rxjs';
import { AuthenticatedUser, TokenEnvelope } from '../models/common.models';
import { AppConfigService } from './app-config.service';

const SESSION_STORAGE_KEY = 'erp.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly appConfig = inject(AppConfigService);
  private readonly router = inject(Router);
  private readonly rawHttp = new HttpClient(inject(HttpBackend));

  private readonly sessionState = signal<TokenEnvelope | null>(this.restoreSession());
  private refreshRequest$?: Observable<TokenEnvelope>;

  readonly session = computed(() => this.sessionState());
  readonly user = computed<AuthenticatedUser | null>(() => this.sessionState()?.user ?? null);
  readonly isAuthenticated = computed(() => !!this.sessionState()?.accessToken);

  get accessToken(): string | null {
    return this.sessionState()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.sessionState()?.refreshToken ?? null;
  }

  login(userNameOrEmail: string, password: string): Observable<TokenEnvelope> {
    return this.rawHttp
      .post<TokenEnvelope>(this.url('auth/login'), { userNameOrEmail, password })
      .pipe(tap((session) => this.persistSession(session)));
  }

  getCurrentUser(): Observable<AuthenticatedUser> {
    const token = this.accessToken;
    if (!token) {
      return throwError(() => new Error('No active session.'));
    }

    return this.rawHttp.get<AuthenticatedUser>(this.url('auth/me'), {
      headers: new HttpHeaders({
        Authorization: `Bearer ${token}`
      })
    }).pipe(
      tap((user) => {
        const current = this.sessionState();
        if (current) {
          this.persistSession({
            ...current,
            user
          });
        }
      })
    );
  }

  refresh(): Observable<string> {
    const refreshToken = this.refreshToken;
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token.'));
    }

    if (!this.refreshRequest$) {
      this.refreshRequest$ = this.rawHttp
        .post<TokenEnvelope>(this.url('auth/refresh'), { refreshToken })
        .pipe(
          tap((session) => this.persistSession(session)),
          shareReplay(1),
          finalize(() => {
            this.refreshRequest$ = undefined;
          })
        );
    }

    return this.refreshRequest$.pipe(map((session) => session.accessToken));
  }

  logout(shouldNotifyApi = true): void {
    const refreshToken = this.refreshToken;
    if (shouldNotifyApi && refreshToken) {
      this.rawHttp.post<void>(this.url('auth/logout'), { refreshToken }).subscribe({
        error: () => {
          // Best-effort revocation.
        }
      });
    }

    this.clearSession();
    void this.router.navigate(['/login']);
  }

  clearSession(): void {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    this.sessionState.set(null);
  }

  private persistSession(session: TokenEnvelope): void {
    this.sessionState.set(session);
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
  }

  private restoreSession(): TokenEnvelope | null {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as TokenEnvelope;
    } catch {
      localStorage.removeItem(SESSION_STORAGE_KEY);
      return null;
    }
  }

  private url(path: string): string {
    return `${this.appConfig.apiBaseUrl}/${path}`;
  }
}
