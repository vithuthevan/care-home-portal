import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-forbidden',
  imports: [RouterLink, MatButtonModule],
  template: `
    <div class="flex min-h-full items-center justify-center p-6">
      <div class="panel state-panel w-full max-w-md">
        <div class="state-panel__icon" aria-hidden="true">!</div>
        <h1 class="m-0 text-2xl font-semibold">Access denied</h1>
        <p class="m-0 text-sm text-[var(--app-text-muted)]">
          You do not have permission to view this page.
        </p>
        <a mat-flat-button color="primary" routerLink="/dashboard">Back to dashboard</a>
      </div>
    </div>
  `,
})
export class ForbiddenPage {}
