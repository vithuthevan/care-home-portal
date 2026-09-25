import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { getApiErrorMessage } from '../../../../core/api-error';

@Component({
  selector: 'app-collections-workspace',
  imports: [
    DecimalPipe,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header title="Collections" subtitle="Overdue receivables and escalation policy" />
    @if (errorMessage()) {
      <app-api-error
        title="Unable to load collections"
        [message]="errorMessage()"
        [showRetry]="true"
        (retry)="load()"
      />
    } @else if (isLoading()) {
      <app-loading-state label="Loading collections..." />
    } @else if (dashboard(); as d) {
      <section class="panel panel--flat mt-4">
        <p>Overdue: £{{ d.overdue | number: '1.2-2' }}</p>
        <p>90+ days: £{{ d.overdue90 | number: '1.2-2' }}</p>
        <p class="text-sm">
          Policy thresholds: {{ d.policy.overdue7Days }} / {{ d.policy.overdue14Days }} /
          {{ d.policy.overdue30Days }} days
        </p>
      </section>
    } @else {
      <app-empty-state title="No overdue accounts" message="No collection dashboard data is available yet." />
    }
  `,
})
export class CollectionsWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  dashboard = signal<{
    overdue: number;
    overdue90: number;
    policy: { overdue7Days: number; overdue14Days: number; overdue30Days: number };
  } | null>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.http
      .get<{
        overdue: number;
        overdue90: number;
        policy: { overdue7Days: number; overdue14Days: number; overdue30Days: number };
      }>('/api/collections/dashboard')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (d) => this.dashboard.set(d),
        error: (err) =>
          this.errorMessage.set(
            getApiErrorMessage(
              err,
              "We couldn't retrieve this information right now. Please try again.",
            ),
          ),
      });
  }
}
