import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface EntitySummaryItem {
  label: string;
  value: string;
  hint?: string;
  routerLink?: string | readonly (string | number)[];
  tone?: 'default' | 'success' | 'attention' | 'finance' | 'critical';
}

@Component({
  selector: 'app-entity-summary-strip',
  imports: [RouterLink],
  template: `
    <div class="entity-summary-strip" role="list">
      @for (item of items(); track item.label) {
        <div
          class="entity-summary-strip__cell"
          [class.entity-summary-strip__cell--success]="item.tone === 'success'"
          [class.entity-summary-strip__cell--attention]="item.tone === 'attention'"
          [class.entity-summary-strip__cell--finance]="item.tone === 'finance'"
          [class.entity-summary-strip__cell--critical]="item.tone === 'critical'"
          role="listitem"
        >
          <span class="entity-summary-strip__label">{{ item.label }}</span>
          @if (item.routerLink) {
            <a class="entity-summary-strip__value" [routerLink]="item.routerLink">{{ item.value }}</a>
          } @else {
            <span class="entity-summary-strip__value">{{ item.value }}</span>
          }
          @if (item.hint) {
            <span class="entity-summary-strip__hint">{{ item.hint }}</span>
          }
        </div>
      }
    </div>
  `,
})
export class EntitySummaryStripComponent {
  readonly items = input.required<EntitySummaryItem[]>();
}
