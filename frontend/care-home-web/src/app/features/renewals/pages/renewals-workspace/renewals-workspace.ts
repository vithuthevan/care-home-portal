import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { getApiErrorMessage } from '../../../../core/api-error';

@Component({
  selector: 'app-renewals-workspace',
  imports: [PageHeaderComponent, ApiErrorComponent],
  template: `
    <app-page-header title="Contract renewals" subtitle="Expiring funding contracts and renewal workflow" />
    @if (errorMessage()) {
      <app-api-error [message]="errorMessage()!" />
    }
    @if (dash(); as d) {
      <section class="panel panel--flat mt-4">
        <p>Expiring in 30 days: {{ d.expiring30 }}</p>
        <p>Expiring in 60 days: {{ d.expiring60 }}</p>
        <p>Expiring in 90 days: {{ d.expiring90 }}</p>
      </section>
    }
    <ul class="mt-4">
      @for (r of renewals(); track r.publicId) {
        <li>Contract #{{ r.contractId }} — {{ r.status }} — ends {{ r.currentEndDate }}</li>
      }
    </ul>
  `,
})
export class RenewalsWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  dash = signal<{ expiring30: number; expiring60: number; expiring90: number } | null>(null);
  renewals = signal<{ publicId: string; contractId: number; status: string; currentEndDate: string }[]>([]);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.http.get<{ expiring30: number; expiring60: number; expiring90: number }>('/api/contract-renewals/dashboard').subscribe({
      next: (d) => this.dash.set(d),
    });
    this.http
      .get<{ publicId: string; contractId: number; status: string; currentEndDate: string }[]>('/api/contract-renewals')
      .subscribe({
        next: (r) => this.renewals.set(r),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load renewals.')),
      });
  }
}
