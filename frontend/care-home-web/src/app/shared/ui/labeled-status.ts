import { Component, input } from '@angular/core';

import { StatusBadgeComponent } from './status-badge';

@Component({
  selector: 'app-labeled-status',
  imports: [StatusBadgeComponent],
  template: `
    <div class="labeled-status" role="group" [attr.aria-label]="kind() + ' status'">
      <span class="labeled-status__kind">{{ kind() }}</span>
      <app-status-badge [value]="value()" [label]="label()" />
    </div>
  `,
  styles: `
    .labeled-status {
      display: inline-flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 0.2rem;
    }
    .labeled-status__kind {
      font-size: 0.68rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      color: var(--app-text-muted);
    }
  `,
})
export class LabeledStatusComponent {
  readonly kind = input.required<string>();
  readonly value = input.required<string>();
  readonly label = input<string>();
}
