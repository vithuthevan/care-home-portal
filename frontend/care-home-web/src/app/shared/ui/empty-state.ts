import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="panel state-panel">
      <div class="state-panel__icon" aria-hidden="true">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
          <path d="M4 7h16M4 12h10M4 17h7" stroke-linecap="round" />
        </svg>
      </div>
      <h2 class="m-0 text-lg font-semibold text-[var(--app-text)]">{{ title() }}</h2>
      @if (message()) {
        <p class="m-0 max-w-md text-sm text-[var(--app-text-muted)]">{{ message() }}</p>
      }
      <div class="flex flex-wrap justify-center gap-2">
        <ng-content />
      </div>
    </div>
  `,
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly message = input('');
}
