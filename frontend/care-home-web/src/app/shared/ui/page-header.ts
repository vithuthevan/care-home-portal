import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  template: `
    <header class="page-header">
      <div class="page-header__copy">
        <h1 class="page-title m-0 font-semibold tracking-tight text-[var(--app-text)]">
          {{ title() }}
        </h1>
        @if (subtitle()) {
          <p class="page-header__subtitle">{{ subtitle() }}</p>
        }
      </div>
      <div class="page-header__actions">
        <ng-content />
      </div>
    </header>
  `,
  styles: `
    .page-header {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
      margin-bottom: var(--space-5);
    }
    .page-header__subtitle {
      margin: var(--space-1) 0 0;
      font-size: 0.875rem;
      line-height: 1.45;
      color: var(--app-text-muted);
      max-width: 42rem;
    }
    .page-header__actions {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--space-2);
    }
    @media (min-width: 768px) {
      .page-header {
        flex-direction: row;
        align-items: flex-start;
        justify-content: space-between;
      }
      .page-header__actions {
        justify-content: flex-end;
        flex-shrink: 0;
        max-width: min(100%, 28rem);
      }
    }
    @media (max-width: 479px) {
      .page-header__actions .mat-mdc-button-base {
        flex: 1 1 calc(50% - var(--space-2));
        min-width: 0;
      }
      .page-header__actions .mat-mdc-button-base:only-child {
        flex: 1 1 100%;
      }
    }
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
}
