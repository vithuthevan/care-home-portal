import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BreadcrumbService } from './breadcrumb.service';

@Component({
  selector: 'app-page-header',
  imports: [RouterLink],
  template: `
    <header class="page-header">
      <div class="page-header__copy">
        @if (showBreadcrumbs() && crumbs.items().length) {
          <nav class="page-header__crumbs" aria-label="Breadcrumb">
            <a routerLink="/dashboard">Home</a>
            @for (item of crumbs.items(); track $index) {
              <span class="page-header__crumb-sep" aria-hidden="true">/</span>
              @if (item.routerLink && $index < crumbs.items().length - 1) {
                <a [routerLink]="item.routerLink">{{ item.label }}</a>
              } @else {
                <span class="page-header__crumb-current">{{ item.label }}</span>
              }
            }
          </nav>
        }
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
      gap: var(--space-3);
      margin-bottom: var(--space-4);
    }
    .page-header__crumbs {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.35rem;
      margin: 0 0 var(--space-1);
      font-size: 0.8125rem;
      line-height: 1.4;
      color: var(--app-text-muted);
    }
    .page-header__crumbs a {
      color: var(--app-text-muted);
      text-decoration: none;
    }
    .page-header__crumbs a:hover {
      color: var(--app-primary);
      text-decoration: underline;
    }
    .page-header__crumb-current {
      color: var(--app-text-secondary);
      font-weight: 500;
    }
    .page-header__crumb-sep {
      color: #d1d5db;
      user-select: none;
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
        margin-bottom: var(--space-5);
      }
      .page-header__actions {
        justify-content: flex-end;
        flex-shrink: 0;
        max-width: none;
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
  protected readonly crumbs = inject(BreadcrumbService);

  readonly title = input.required<string>();
  readonly subtitle = input('');
  /** When false, hides the trail (e.g. login). Default shows shell breadcrumbs. */
  readonly showBreadcrumbs = input(true);
}
