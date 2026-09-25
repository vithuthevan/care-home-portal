import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-api-error',
  imports: [MatButtonModule],
  template: `
    @if (message()) {
      <div class="feedback-banner feedback-banner--error panel panel--flat" role="alert">
        @if (title()) {
          <p class="font-semibold mb-1">{{ title() }}</p>
        }
        <p [class.mb-2]="showRetry()">{{ message() }}</p>
        @if (showRetry()) {
          <button mat-stroked-button type="button" (click)="retry.emit()">Retry</button>
        }
      </div>
    }
  `,
})
export class ApiErrorComponent {
  readonly title = input<string | null>(null);
  readonly message = input<string | null>(null);
  readonly showRetry = input(false);
  readonly retry = output<void>();
}
