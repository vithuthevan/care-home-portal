import { DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-currency-display',
  imports: [DecimalPipe],
  template: `
    <span class="currency-display" [class]="sizeClass()">
      £{{ amount() | number: '1.2-2' }}
      @if (suffix()) {
        <span class="currency-suffix">{{ suffix() }}</span>
      }
    </span>
  `,
  styles: `
    .currency-display {
      font-variant-numeric: tabular-nums;
      font-weight: 600;
      color: var(--app-text);
    }
    .currency-display--hero {
      font-size: 1.75rem;
      font-weight: 700;
      letter-spacing: -0.02em;
    }
    .currency-display--lg {
      font-size: 1.35rem;
    }
    .currency-suffix {
      margin-left: 0.35rem;
      font-size: 0.85em;
      font-weight: 500;
      color: var(--app-text-muted);
    }
  `,
})
export class CurrencyDisplayComponent {
  readonly amount = input.required<number>();
  readonly suffix = input('');
  readonly size = input<'default' | 'lg' | 'hero'>('default');

  sizeClass(): string {
    const size = this.size();
    if (size === 'hero') return 'currency-display--hero';
    if (size === 'lg') return 'currency-display--lg';
    return '';
  }
}
