import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { DisplayDateTimePipe } from '../../../../shared/format/display-date-time.pipe';
import { PagedResult } from '../../../../core/models';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';

interface EmailSendLogRow {
  id: number;
  attemptedAt: string;
  documentType: string;
  documentId: number;
  recipient?: string | null;
  success: boolean;
  simulated: boolean;
  errorMessage?: string | null;
}

@Component({
  selector: 'app-email-send-log-list',
  imports: [
    RouterLink,
    FormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    DisplayDateTimePipe,
    TablePaginationComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './email-send-log-list.html',
})
export class EmailSendLogListPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly items = signal<EmailSendLogRow[]>([]);
  readonly resendingId = signal<number | null>(null);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  documentType = '';
  successFilter = '';
  page = 1;
  pageSize = 25;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let params = new HttpParams()
      .set('page', this.page)
      .set('pageSize', this.pageSize);
    if (this.documentType) {
      params = params.set('documentType', this.documentType);
    }
    if (this.successFilter === 'ok') {
      params = params.set('success', 'true');
    } else if (this.successFilter === 'failed') {
      params = params.set('success', 'false');
    }

    this.http
      .get<PagedResult<EmailSendLogRow>>('/api/email-send-logs', { params })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.items.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load email delivery log.')),
      });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.load();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.load();
  }

  canResend(row: EmailSendLogRow): boolean {
    return (
      this.auth.canWrite() &&
      (row.documentType === 'Invoice' || row.documentType === 'CreditNote')
    );
  }

  resend(row: EmailSendLogRow): void {
    if (!this.canResend(row) || this.resendingId() !== null) {
      return;
    }
    this.resendingId.set(row.id);
    this.http
      .post<{ simulated?: boolean }>(`/api/email-send-logs/${row.id}/resend`, {})
      .pipe(finalize(() => this.resendingId.set(null)))
      .subscribe({
        next: () => {
          this.toast.success('Resend completed.');
          this.load();
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Resend failed.')),
      });
  }

  statusLabel(row: EmailSendLogRow): string {
    if (!row.success) {
      return 'Failed';
    }
    if (row.simulated) {
      return 'Simulated';
    }
    return 'Sent';
  }
}
