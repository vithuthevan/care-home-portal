import { Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-state',
  imports: [MatProgressSpinnerModule],
  template: `
    <div class="panel state-panel state-panel--loading" role="status" [attr.aria-label]="label()">
      <mat-progress-spinner diameter="32" mode="indeterminate" />
      <span class="text-sm text-[var(--app-text-muted)]">{{ label() }}</span>
    </div>
  `,
})
export class LoadingStateComponent {
  readonly label = input('Loading...');
}
