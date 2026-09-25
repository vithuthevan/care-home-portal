import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';

export type KpiTone = 'info' | 'success' | 'finance' | 'attention' | 'critical';

@Component({
  selector: 'app-kpi-card',
  imports: [RouterLink, MatIconModule],
  template: `
    @if (routerLink(); as link) {
      <a
        class="panel kpi-card kpi-card--link"
        [class.kpi-card--info]="tone() === 'info'"
        [class.kpi-card--success]="tone() === 'success'"
        [class.kpi-card--finance]="tone() === 'finance'"
        [class.kpi-card--attention]="tone() === 'attention'"
        [class.kpi-card--critical]="tone() === 'critical'"
        [routerLink]="link"
        [queryParams]="queryParams()"
      >
        @if (icon()) {
          <div class="kpi-icon" aria-hidden="true">
            <mat-icon>{{ icon() }}</mat-icon>
          </div>
        }
        <div class="kpi-card__body">
          <h3 class="kpi-card__label">{{ label() }}</h3>
          <p class="kpi-card__value">{{ value() }}</p>
          @if (hint()) {
            <p class="kpi-card__hint">{{ hint() }}</p>
          }
          <ng-content />
        </div>
      </a>
    } @else {
      <div
        class="panel kpi-card"
        [class.kpi-card--info]="tone() === 'info'"
        [class.kpi-card--success]="tone() === 'success'"
        [class.kpi-card--finance]="tone() === 'finance'"
        [class.kpi-card--attention]="tone() === 'attention'"
        [class.kpi-card--critical]="tone() === 'critical'"
      >
        @if (icon()) {
          <div class="kpi-icon" aria-hidden="true">
            <mat-icon>{{ icon() }}</mat-icon>
          </div>
        }
        <div class="kpi-card__body">
          <h3 class="kpi-card__label">{{ label() }}</h3>
          <p class="kpi-card__value">{{ value() }}</p>
          @if (hint()) {
            <p class="kpi-card__hint">{{ hint() }}</p>
          }
          <ng-content />
        </div>
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
  `,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly hint = input('');
  readonly icon = input('');
  readonly tone = input<KpiTone>('info');
  readonly routerLink = input<string | readonly (string | number)[] | null>(null);
  readonly queryParams = input<Record<string, string | number | boolean | null | undefined> | null>(
    null,
  );
}
