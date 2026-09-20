import { Component, input } from '@angular/core';

@Component({
  selector: 'app-section-header',
  template: `
    <div class="section-header">
      <div>
        <h2 class="section-title">{{ title() }}</h2>
        @if (subtitle()) {
          <p class="section-subtitle">{{ subtitle() }}</p>
        }
      </div>
      <div class="section-actions">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    .section-header {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      justify-content: space-between;
      gap: 0.75rem 1rem;
      margin-bottom: 1rem;
    }
    .section-title {
      margin: 0;
      font-size: 1.05rem;
      font-weight: 600;
      color: var(--app-text);
    }
    .section-subtitle {
      margin: 0.25rem 0 0;
      font-size: 0.875rem;
      color: var(--app-text-muted);
      max-width: 42rem;
    }
    .section-actions {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem;
    }
  `,
})
export class SectionHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
}
