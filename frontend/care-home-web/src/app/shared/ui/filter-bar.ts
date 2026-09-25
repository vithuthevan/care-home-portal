import { Component, input } from '@angular/core';

/**
 * Coherent filter area wrapper. Place fields in the default slot and actions in [filterActions].
 */
@Component({
  selector: 'app-filter-bar',
  template: `
    <section
      class="filter-bar"
      [class.panel]="!embedded() && !toolbar()"
      [class.filter-bar--compact]="compact()"
      [class.filter-bar--embedded]="embedded()"
      [class.filter-bar--toolbar]="toolbar()"
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
  /** Compact table toolbar: search/filters on the left, actions on the right. */
  readonly toolbar = input(false);
}
