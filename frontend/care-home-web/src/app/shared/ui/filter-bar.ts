import { Component, input } from '@angular/core';

/**
 * Coherent filter area wrapper. Place fields in the default slot and actions in [filterActions].
 */
@Component({
  selector: 'app-filter-bar',
  template: `
    <section
      class="filter-bar"
      [class.panel]="!embedded()"
      [class.filter-bar--compact]="compact()"
      [class.filter-bar--embedded]="embedded()"
    >
      <div class="filter-bar__fields">
        <ng-content />
      </div>
      <div class="filter-bar__actions">
        <ng-content select="[filterActions]" />
      </div>
    </section>
  `,
})
export class FilterBarComponent {
  readonly compact = input(false);
  /** When true, omits outer panel chrome (for use inside another panel). */
  readonly embedded = input(false);
}
