import { Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type LoadingVariant = 'spinner' | 'table' | 'form' | 'detail' | 'dashboard' | 'cards';

@Component({
  selector: 'app-loading-state',
  imports: [MatProgressSpinnerModule],
  template: `
    @if (variant() === 'spinner') {
      <div class="panel state-panel state-panel--loading" role="status" [attr.aria-label]="label()">
        <mat-progress-spinner diameter="32" mode="indeterminate" />
        <span class="text-sm text-[var(--app-text-muted)]">{{ label() }}</span>
      </div>
    } @else {
      <div class="skeleton-block" role="status" aria-busy="true" [attr.aria-label]="label()">
        <p class="skeleton-block__status">
          <mat-progress-spinner diameter="18" mode="indeterminate" />
          <span>{{ label() }}</span>
        </p>
        @switch (variant()) {
          @case ('form') {
            <div class="skeleton-form">
              @for (field of indexes(6); track field) {
                <span class="skeleton-bar skeleton-bar--field"></span>
              }
            </div>
          }
          @case ('cards') {
            @for (card of indexes(2); track card) {
              <div class="skeleton-card">
                <span class="skeleton-bar skeleton-bar--title"></span>
                <span class="skeleton-bar" [style.width.%]="barWidth(card, 1)"></span>
                <span class="skeleton-bar" [style.width.%]="barWidth(card, 2)"></span>
              </div>
            }
          }
          @case ('dashboard') {
            <div class="skeleton-kpis">
              @for (card of indexes(3); track card) {
                <div class="skeleton-kpi">
                  <span class="skeleton-bar skeleton-bar--short"></span>
                  <span class="skeleton-bar skeleton-bar--value"></span>
                </div>
              }
            </div>
            <div class="skeleton-table">
              @for (row of indexes(4); track row) {
                <div class="skeleton-table__row">
                  @for (column of indexes(3); track column) {
                    <span class="skeleton-bar" [style.width.%]="barWidth(row, column)"></span>
                  }
                </div>
              }
            </div>
          }
          @case ('detail') {
            <span class="skeleton-bar skeleton-bar--title"></span>
            <div class="skeleton-form">
              @for (field of indexes(4); track field) {
                <span class="skeleton-bar skeleton-bar--field"></span>
              }
            </div>
            <div class="skeleton-table">
              @for (row of indexes(rows()); track row) {
                <div class="skeleton-table__row">
                  @for (column of indexes(columns()); track column) {
                    <span class="skeleton-bar" [style.width.%]="barWidth(row, column)"></span>
                  }
                </div>
              }
            </div>
          }
          @default {
            <div class="skeleton-table">
              <div class="skeleton-table__row skeleton-table__row--head">
                @for (column of indexes(columns()); track column) {
                  <span class="skeleton-bar skeleton-bar--head"></span>
                }
              </div>
              @for (row of indexes(rows()); track row) {
                <div class="skeleton-table__row">
                  @for (column of indexes(columns()); track column) {
                    <span class="skeleton-bar" [style.width.%]="barWidth(row, column)"></span>
                  }
                </div>
              }
            </div>
          }
        }
      </div>
    }
  `,
  styles: `
    .skeleton-block {
      padding: var(--space-4);
      border: 1px solid var(--app-border);
      border-radius: var(--app-radius);
      background: var(--app-surface);
    }
    .skeleton-block__status {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      margin: 0 0 var(--space-4);
      color: var(--app-text-muted);
      font-size: 0.875rem;
    }
    .skeleton-bar {
      display: block;
      height: 0.75rem;
      max-width: 100%;
      border-radius: 999px;
      background: linear-gradient(
        90deg,
        var(--app-surface-muted) 0%,
        color-mix(in srgb, var(--app-text) 10%, var(--app-surface)) 50%,
        var(--app-surface-muted) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-shimmer 1.2s ease-in-out infinite;
    }
    .skeleton-bar--head {
      height: 0.65rem;
      width: 70%;
    }
    .skeleton-bar--title {
      width: 12rem;
      height: 1rem;
      margin-bottom: var(--space-3);
    }
    .skeleton-bar--short {
      width: 45%;
    }
    .skeleton-bar--value {
      width: 30%;
      height: 1.25rem;
    }
    .skeleton-bar--field {
      height: 2.5rem;
      width: 100%;
      border-radius: 8px;
    }
    .skeleton-table {
      display: flex;
      flex-direction: column;
      gap: 0.85rem;
    }
    .skeleton-table__row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(4rem, 1fr));
      gap: var(--space-3);
      align-items: center;
    }
    .skeleton-form,
    .skeleton-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: var(--space-3);
      margin-bottom: var(--space-4);
    }
    .skeleton-card,
    .skeleton-kpi {
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      padding: var(--space-3);
      border: 1px solid var(--app-border);
      border-radius: var(--app-radius);
      background: var(--app-surface-alt);
    }
    .skeleton-card + .skeleton-card {
      margin-top: var(--space-3);
    }
    @keyframes skeleton-shimmer {
      0% {
        background-position: 100% 0;
      }
      100% {
        background-position: -100% 0;
      }
    }
    @media (prefers-reduced-motion: reduce) {
      .skeleton-bar {
        animation: none;
      }
    }
  `,
})
export class LoadingStateComponent {
  readonly label = input('Loading...');
  readonly variant = input<LoadingVariant>('spinner');
  readonly columns = input(5);
  readonly rows = input(6);

  protected indexes(count: number): number[] {
    return Array.from({ length: Math.max(count, 0) }, (_, index) => index);
  }

  protected barWidth(row: number, column: number): number {
    const widths = [92, 68, 84, 58, 76, 88];
    return widths[(row + column) % widths.length];
  }
}
