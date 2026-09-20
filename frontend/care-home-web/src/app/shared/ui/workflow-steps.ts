import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export interface WorkflowStep {
  label: string;
  description?: string;
}

@Component({
  selector: 'app-workflow-steps',
  imports: [MatIconModule],
  template: `
    <ol class="workflow-steps" aria-label="Progress">
      @for (step of steps(); track step.label; let i = $index) {
        <li
          class="workflow-step"
          [class.workflow-step--complete]="i < activeIndex()"
          [class.workflow-step--current]="i === activeIndex()"
        >
          <span class="workflow-step__marker" aria-hidden="true">
            @if (i < activeIndex()) {
              <mat-icon>check</mat-icon>
            } @else {
              {{ i + 1 }}
            }
          </span>
          <span class="workflow-step__text">
            <span class="workflow-step__label">{{ step.label }}</span>
            @if (step.description) {
              <span class="workflow-step__desc">{{ step.description }}</span>
            }
          </span>
        </li>
      }
    </ol>
  `,
  styles: `
    .workflow-steps {
      list-style: none;
      margin: 0 0 1.25rem;
      padding: 0;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
      gap: 0.5rem;
    }
    .workflow-step {
      display: flex;
      align-items: flex-start;
      gap: 0.65rem;
      padding: 0.65rem 0.75rem;
      border-radius: 12px;
      border: 1px solid var(--app-border);
      background: #fff;
      color: var(--app-text-muted);
      font-size: 0.8rem;
    }
    .workflow-step--complete {
      border-color: #bbf7d0;
      background: #f8fdf9;
      color: var(--app-text);
    }
    .workflow-step--current {
      border-color: var(--app-primary);
      box-shadow: inset 0 0 0 1px var(--app-primary);
      color: var(--app-text);
    }
    .workflow-step__marker {
      flex-shrink: 0;
      width: 1.65rem;
      height: 1.65rem;
      border-radius: 999px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
      font-weight: 700;
      background: #f3f4f6;
      color: var(--app-text-muted);
    }
    .workflow-step--complete .workflow-step__marker,
    .workflow-step--current .workflow-step__marker {
      background: var(--app-primary-soft);
      color: var(--app-primary);
    }
    .workflow-step__marker mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }
    .workflow-step__text {
      display: flex;
      flex-direction: column;
      gap: 0.1rem;
      min-width: 0;
    }
    .workflow-step__label {
      font-weight: 600;
      color: inherit;
    }
    .workflow-step__desc {
      font-size: 0.72rem;
      line-height: 1.3;
    }
  `,
})
export class WorkflowStepsComponent {
  readonly steps = input.required<WorkflowStep[]>();
  readonly activeIndex = input(0);
}
