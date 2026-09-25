import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { from, switchMap, tap } from 'rxjs';

import { encryptLoginPassword, LoginPublicKey } from './login-password-cipher';
import { AuthUser } from './models';

const STORAGE_KEY = 'carehome.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly currentUser = signal<AuthUser | null>(readStoredUser());

  get token(): string | null {
    return this.currentUser()?.token ?? null;
  }

  isLoggedIn(): boolean {
    return !!this.currentUser()?.token;
  }

  mustChangePassword(): boolean {
    return !!this.currentUser()?.mustChangePassword;
  }

  hasRole(...roles: string[]): boolean {
    const current = this.currentUser()?.roles ?? [];
    return roles.some((role) => current.includes(role));
  }

  canWrite(): boolean {
    return this.isLoggedIn() && !this.isReadOnlyOnly();
  }

  isReadOnlyOnly(): boolean {
    return (
      this.hasRole('ReadOnly') &&
      !this.hasRole('PlatformAdmin', 'SuperAdmin', 'TenantAdmin', 'Administrator')
    );
  }

  isPlatformAdmin(): boolean {
    return this.hasRole('PlatformAdmin', 'SuperAdmin');
  }

  canManageUsers(): boolean {
    return this.hasRole('TenantAdmin', 'Administrator');
  }

  canManageOrganisation(): boolean {
    return this.hasRole('TenantAdmin', 'Administrator');
  }

  showGuardian(): boolean {
    return !!this.currentUser()?.showGuardian;
  }

  financeModuleEnabled(): boolean {
    return !!this.currentUser()?.financeModuleEnabled;
  }

  homePath(): string[] {
    if (this.mustChangePassword()) {
      return ['/change-password'];
    }

    if (this.isPlatformAdmin() && !this.currentUser()?.tenantPublicId) {
      return ['/platform/tenants'];
    }

    return ['/dashboard'];
  }

  login(email: string, password: string) {
    return this.encryptSecret(password).pipe(
      switchMap((passwordCipher) =>
        this.http.post<AuthUser>('/api/auth/login', { email, passwordCipher }),
      ),
      tap((user) => this.storeUser(user)),
    );
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.encryptSecrets([currentPassword, newPassword]).pipe(
      switchMap(([currentPasswordCipher, newPasswordCipher]) =>
        this.http.post<AuthUser>('/api/auth/change-password', {
          currentPasswordCipher,
          newPasswordCipher,
        }),
      ),
      tap((user) => this.storeUser(user)),
    );
  }

  encryptSecret(plaintext: string) {
    return this.http
      .get<LoginPublicKey>('/api/auth/login-key')
      .pipe(switchMap((key) => from(encryptLoginPassword(key, plaintext))));
  }

  private encryptSecrets(values: string[]) {
    return this.http
      .get<LoginPublicKey>('/api/auth/login-key')
      .pipe(
        switchMap((key) =>
          from(Promise.all(values.map((value) => encryptLoginPassword(key, value)))),
        ),
      );
  }

  refreshProfile() {
    return this.http.get<AuthUser>('/api/auth/me').pipe(
      tap((profile) => {
        const current = this.currentUser();
        if (!current?.token) {
          return;
        }

        this.storeUser({ ...current, ...profile, token: current.token });
      }),
    );
  }

  forgotPassword(email: string) {
    return this.http.post<{ message: string }>('/api/auth/forgot-password', { email });
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.encryptSecret(newPassword).pipe(
      switchMap((newPasswordCipher) =>
        this.http.post<{ message: string }>('/api/auth/reset-password', {
          email,
          token,
          newPasswordCipher,
        }),
      ),
    );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.currentUser.set(null);
    void this.router.navigate(['/login']);
  }

  private storeUser(user: AuthUser): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }
}

function readStoredUser(): AuthUser | null {
  try {
    if (typeof localStorage === 'undefined') {
      return null;
    }

    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  } catch {
    return null;
  }
}
