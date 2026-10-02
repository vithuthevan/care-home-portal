import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { getApiErrorMessage } from '../../../../core/api-error';
import { ImportExportToolbarComponent } from '../../../../shared/ui/import-export-toolbar';

interface DisputeRow {
  publicId: string;
  invoiceNumber: string;
  funderName: string;
  disputedAmount: number;
  status: string;
  reasonCode: string;
}

@Component({
  selector: 'app-disputes-workspace',
  imports: [
    DecimalPipe,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ImportExportToolbarComponent,
  ],
  template: `
    <app-page-header title="Disputes" subtitle="Invoice disputes and funder queries">
      <app-import-export-toolbar entity="disputes" [importEnabled]="false" />
    </app-page-header>
    @if (errorMessage()) {
      <app-api-error [message]="errorMessage()!" />
    }
    @if (isLoading()) {
      <app-loading-state variant="table" label="Loading disputes..." [columns]="5" />
    } @else if (!errorMessage() && rows().length === 0) {
      <app-empty-state
        title="No disputes"
        message="Invoice disputes opened against funders will appear here."
      />
    } @else if (!errorMessage()) {
    <div class="table-wrap mt-4">
      <table class="data-table">
        <thead>
          <tr>
            <th>Invoice</th>
            <th>Funder</th>
            <th>Amount</th>
            <th>Reason</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          @for (d of rows(); track d.publicId) {
            <tr>
              <td>{{ d.invoiceNumber }}</td>
              <td>{{ d.funderName }}</td>
              <td>£{{ d.disputedAmount | number: '1.2-2' }}</td>
              <td>{{ d.reasonCode }}</td>
              <td>{{ d.status }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
    }
  `,
})
export class DisputesWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  rows = signal<DisputeRow[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal(true);

  ngOnInit(): void {
    this.http
      .get<DisputeRow[]>('/api/disputes')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (r) => this.rows.set(r),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load disputes.')),
      });
  }
}
