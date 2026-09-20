import { Component, input } from '@angular/core';

@Component({
  selector: 'app-api-error',
  template: `
    @if (message()) {
      <div class="feedback-banner feedback-banner--error" role="alert">
        {{ message() }}
      </div>
    }
  `,
})
export class ApiErrorComponent {
  readonly message = input<string | null>(null);
}
