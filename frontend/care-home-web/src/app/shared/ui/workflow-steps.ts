import { Component, input } from '@angular/core';

export interface WorkflowStep {
  label: string;
  description?: string;
}

@Component({
  selector: 'app-workflow-steps',
  template: `
    <ol class="workflow-steps" aria-label="Progress">
      @for (step of steps(); track step.label; let i = $index; let last = $last) {
        <li
          class="workflow-step"
          [class.workflow-step--complete]="stepState(i) === 'complete'"
          [class.workflow-step--current]="stepState(i) === 'current'"
          [class.workflow-step--blocked]="stepState(i) === 'blocked'"
          [class.workflow-step--upcoming]="stepState(i) === 'upcoming'"
          [attr.aria-current]="stepState(i) === 'current' ? 'step' : null"
        >
          <span class="workflow-step__marker" aria-hidden="true">
            @switch (stepState(i)) {
              @case ('complete') {
                ✓
              }
              @case ('current') {
                ●
              }
              @case ('blocked') {
                ○
              }
              @default {
                ○
              }
            }
          </span>
          <span class="workflow-step__text">
            <span class="workflow-step__label">{{ step.label }}</span>
            @if (step.description) {
              <span class="workflow-step__desc">{{ step.description }}</span>
            }
          </span>
        </li>
        @if (!last) {
          <li class="workflow-step-connector" aria-hidden="true"></li>
        }
      }
    </ol>
  `,
  styles: `
    .workflow-steps {
      list-style: none;
      margin: 0 0 1rem;
      padding: 0.35rem 0.5rem;
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      gap: 0;
      border: 1px solid var(--app-border);
      border-radius: var(--app-radius);
      background: var(--app-surface);
    }

    .workflow-step {
      display: flex;
      align-items: flex-start;
      gap: 0.4rem;
      padding: 0.25rem 0.35rem;
      min-width: 0;
      flex: 1 1 5.5rem;
      color: var(--app-text-muted);
      font-size: 0.78rem;
    }

    .workflow-step--complete {
      color: var(--app-text);
    }

    .workflow-step--current {
      color: var(--app-text);
      font-weight: 600;
    }

    .workflow-step--blocked .workflow-step__label {
      color: var(--app-text-muted);
    }

    .workflow-step--blocked .workflow-step__desc {
      color: var(--app-warning);
    }

    .workflow-step__marker {
      flex-shrink: 0;
      width: 1.1rem;
      text-align: center;
      font-size: 0.7rem;
      line-height: 1.35;
      color: inherit;
    }

    .workflow-step--current .workflow-step__marker {
      font-size: 0.55rem;
      line-height: 1.65;
    }

    .workflow-step__text {
      display: flex;
      flex-direction: column;
      gap: 0.05rem;
      min-width: 0;
    }

    .workflow-step__label {
      font-weight: 600;
      line-height: 1.25;
    }

    .workflow-step__desc {
      font-size: 0.68rem;
      line-height: 1.25;
      color: var(--app-text-muted);
    }

    .workflow-step-connector {
      list-style: none;
      flex: 1 1 1.5rem;
      min-width: 1rem;
      max-width: 3rem;
      align-self: center;
      height: 0;
      border-top: 1px solid var(--app-border);
      margin: 0.65rem 0.15rem 0;
    }

    @media (max-width: 639px) {
      .workflow-steps {
        flex-direction: column;
        align-items: stretch;
      }

      .workflow-step-connector {
        display: none;
      }

      .workflow-step {
        flex: 1 1 auto;
      }
    }
  `,
})
export class WorkflowStepsComponent {
  readonly steps = input.required<WorkflowStep[]>();
  readonly activeIndex = input(0);
  /** When set, this step is shown as blocked (e.g. Generate while exceptions exist). */
  readonly blockedIndex = input<number | null>(null);

  stepState(index: number): 'complete' | 'current' | 'blocked' | 'upcoming' {
    const active = this.activeIndex();
    const blocked = this.blockedIndex();
    if (blocked !== null && index === blocked) {
      return 'blocked';
    }
    if (index < active) {
      return 'complete';
    }
    if (index === active) {
      return 'current';
    }
    return 'upcoming';
  }
}
