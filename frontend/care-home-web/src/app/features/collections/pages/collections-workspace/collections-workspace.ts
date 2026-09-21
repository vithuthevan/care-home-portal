import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { getApiErrorMessage } from '../../../../core/api-error';

@Component({
  selector: 'app-collections-workspace',
  imports: [DecimalPipe, PageHeaderComponent, ApiErrorComponent],
  template: `
    <app-page-header title="Collections" subtitle="Overdue receivables and escalation policy" />
    @if (errorMessage()) {
      <app-api-error [message]="errorMessage()!" />
    }
    @if (dashboard(); as d) {
      <section class="panel panel--flat mt-4">
        <p>Overdue: £{{ d.overdue | number: '1.2-2' }}</p>
        <p>90+ days: £{{ d.overdue90 | number: '1.2-2' }}</p>
        <p class="text-sm">
          Policy thresholds: {{ d.policy.overdue7Days }} / {{ d.policy.overdue14Days }} /
          {{ d.policy.overdue30Days }} days
        </p>
      </section>
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

  ngOnInit(): void {
    this.http
      .get<{
        overdue: number;
        overdue90: number;
        policy: { overdue7Days: number; overdue14Days: number; overdue30Days: number };
      }>('/api/collections/dashboard')
      .subscribe({
        next: (d) => this.dashboard.set(d),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load collections.')),
      });
  }
}
