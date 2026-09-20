import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-icon-action',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    @if (routerLink(); as link) {
      <a
        mat-icon-button
        [routerLink]="link"
        [queryParams]="queryParams()"
        [attr.aria-label]="ariaLabel()"
        [matTooltip]="ariaLabel()"
        [class.icon-action--warn]="warn()"
      >
        <mat-icon>{{ icon() }}</mat-icon>
      </a>
    } @else {
      <button
        mat-icon-button
        type="button"
        (click)="action.emit($event)"
        [disabled]="disabled()"
        [attr.aria-label]="ariaLabel()"
        [matTooltip]="ariaLabel()"
        [class.icon-action--warn]="warn()"
      >
        <mat-icon>{{ icon() }}</mat-icon>
      </button>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
    }

    .icon-action--warn {
      color: var(--app-danger);
    }
  `,
})
export class IconActionButtonComponent {
  readonly icon = input.required<string>();
  readonly ariaLabel = input.required<string>();
  readonly routerLink = input<string | readonly (string | number)[] | null>(null);
  readonly queryParams = input<Record<string, string | number | boolean | null | undefined> | null>(
    null,
  );
  readonly disabled = input(false);
  readonly warn = input(false);

  readonly action = output<Event>();
}
