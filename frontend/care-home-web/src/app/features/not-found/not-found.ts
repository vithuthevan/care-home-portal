import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButtonModule],
  template: `
    <div class="flex min-h-full items-center justify-center p-6">
      <div class="panel state-panel w-full max-w-md">
        <div class="state-panel__icon" aria-hidden="true">?</div>
        <h1 class="m-0 text-2xl font-semibold">Page not found</h1>
        <p class="m-0 text-sm text-[var(--app-text-muted)]">
          This page does not exist or has been moved.
        </p>
        <a mat-flat-button color="primary" [routerLink]="auth.isLoggedIn() ? auth.homePath() : ['/login']">
          {{ auth.isLoggedIn() ? 'Go to home' : 'Sign in' }}
        </a>
      </div>
    </div>
  `,
})
export class NotFoundPage {
  readonly auth = inject(AuthService);
}
