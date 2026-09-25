import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { getApiErrorMessage } from '../../../../core/api-error';

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
  imports: [DecimalPipe, PageHeaderComponent, ApiErrorComponent],
  template: `
    <app-page-header title="Disputes" subtitle="Invoice disputes and funder queries" />
    @if (errorMessage()) {
      <app-api-error [message]="errorMessage()!" />
    }
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
  `,
})
export class DisputesWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  rows = signal<DisputeRow[]>([]);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.http.get<DisputeRow[]>('/api/disputes').subscribe({
      next: (r) => this.rows.set(r),
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load disputes.')),
    });
  }
}
