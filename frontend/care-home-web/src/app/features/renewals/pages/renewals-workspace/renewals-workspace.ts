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
  selector: 'app-renewals-workspace',
  imports: [
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header title="Contract renewals" subtitle="Expiring funding contracts and renewal workflow" />
    @if (errorMessage()) {
      <app-api-error
        title="Unable to load contract renewals"
        [message]="errorMessage()"
        [showRetry]="true"
        (retry)="load()"
      />
    } @else if (isLoading()) {
      <app-loading-state label="Loading contract renewals..." />
    } @else {
      @if (dash(); as d) {
        <section class="panel panel--flat mt-4">
          <p>Expiring in 30 days: {{ d.expiring30 }}</p>
          <p>Expiring in 60 days: {{ d.expiring60 }}</p>
          <p>Expiring in 90 days: {{ d.expiring90 }}</p>
        </section>
      }
      @if (renewals().length === 0) {
        <app-empty-state
          title="No contracts due for renewal"
          message="Active funding contracts with renewal workflows will appear here."
        />
      } @else {
        <ul class="mt-4">
          @for (r of renewals(); track r.publicId) {
            <li>Contract #{{ r.contractId }} — {{ r.status }} — ends {{ r.currentEndDate }}</li>
          }
        </ul>
      }
    }
  `,
})
export class RenewalsWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  dash = signal<{ expiring30: number; expiring60: number; expiring90: number } | null>(null);
  renewals = signal<{ publicId: string; contractId: number; status: string; currentEndDate: string }[]>(
    [],
  );
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http
      .get<{ expiring30: number; expiring60: number; expiring90: number }>(
        '/api/contract-renewals/dashboard',
      )
      .subscribe({
        next: (d) => this.dash.set(d),
      });

    this.http
      .get<{ publicId: string; contractId: number; status: string; currentEndDate: string }[]>(
        '/api/contract-renewals',
      )
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (r) => this.renewals.set(r),
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
