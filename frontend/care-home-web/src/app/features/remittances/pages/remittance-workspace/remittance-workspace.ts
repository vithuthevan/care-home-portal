import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { getApiErrorMessage } from '../../../../core/api-error';
import { ToastService } from '../../../../shared/ui/toast.service';

interface RemittanceBatch {
  publicId: string;
  status: string;
  paymentReference?: string;
  sourceFileName?: string;
  lineCount: number;
  matchedLineCount: number;
}

interface RemittanceLine {
  publicId: string;
  lineNumber: number;
  status: string;
  invoiceReference?: string;
  paidAmount?: number;
  deductionAmount?: number;
  matchedInvoiceNumber?: string;
}

interface RemittanceDetail {
  publicId: string;
  status: string;
  lines: RemittanceLine[];
}

@Component({
  selector: 'app-remittance-workspace',
  imports: [
    DecimalPipe,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
  ],
  template: `
    <app-page-header title="Remittances" subtitle="Import funder remittance advice and apply payments" />
    @if (errorMessage()) {
      <app-api-error [message]="errorMessage()!" />
    }
    <section class="panel panel--flat mt-4">
      <input type="file" accept=".csv,.xlsx" (change)="onFile($event)" />
      @if (loading()) {
        <app-loading-state label="Importing..." />
      }
    </section>
    <div class="table-wrap mt-4">
      <table class="data-table">
        <thead>
          <tr>
            <th>File</th>
            <th>Status</th>
            <th>Lines</th>
            <th>Matched</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (b of batches(); track b.publicId) {
            <tr>
              <td>{{ b.sourceFileName || '—' }}</td>
              <td>{{ b.status }}</td>
              <td>{{ b.lineCount }}</td>
              <td>{{ b.matchedLineCount }}</td>
              <td>
                <button mat-button type="button" (click)="confirm(b.publicId)" [disabled]="b.status === 'Confirmed'">
                  Confirm
                </button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class RemittanceWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  batches = signal<RemittanceBatch[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.http.get<RemittanceBatch[]>('/api/remittances').subscribe({
      next: (rows) => this.batches.set(rows),
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load remittances.')),
    });
  }

  onFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }
    const form = new FormData();
    form.append('file', file);
    const url = file.name.toLowerCase().endsWith('.xlsx')
      ? '/api/remittances/import/xlsx'
      : '/api/remittances/import/csv';
    this.loading.set(true);
    this.http
      .post<RemittanceDetail>(url, form)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Remittance imported.');
          this.load();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Import failed.')),
      });
  }

  confirm(publicId: string): void {
    this.http.post(`/api/remittances/${publicId}/confirm`, {}).subscribe({
      next: () => {
        this.toast.success('Remittance confirmed.');
        this.load();
      },
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Confirm failed.')),
    });
  }
}
