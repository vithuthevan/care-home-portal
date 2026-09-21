import { DecimalPipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { PagedResult } from '../../../../core/models';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';

interface PaymentRow {
  publicId: string;
  receivedDate: string;
  reference?: string | null;
  payerName?: string | null;
  amount: number;
  allocatedAmount: number;
  unappliedAmount: number;
  status: string;
}

interface PaymentDetail extends PaymentRow {
  notes?: string | null;
  allocations: PaymentAllocationRow[];
}

interface PaymentAllocationRow {
  publicId: string;
  invoicePublicId: string;
  invoiceNumber: string;
  residentName?: string | null;
  careHomeName: string;
  allocatedAmount: number;
  outstandingAfter: number;
  isReversed: boolean;
}

@Component({
  selector: 'app-payments-workspace',
  imports: [
    FormsModule,
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTabsModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    DisplayDatePipe,
    StatusBadgeComponent,
    TablePaginationComponent,
  ],
  templateUrl: './payments-workspace.html',
})
export class PaymentsWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);

  loading = signal(true);
  errorMessage = signal<string | null>(null);
  rows = signal<PaymentRow[]>([]);
  totalCount = signal(0);
  page = 1;
  pageSize = 25;
  tabIndex = 0;

  selected = signal<PaymentDetail | null>(null);
  detailLoading = signal(false);

  newAmount: number | null = null;
  newReference = '';
  newReceivedDate = new Date().toISOString().slice(0, 10);
  saving = signal(false);

  ngOnInit(): void {
    this.load();
  }

  onTabChange(index: number): void {
    this.tabIndex = index;
    this.page = 1;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    let params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    if (this.tabIndex === 1) {
      params = params.set('status', 'Allocated');
    } else if (this.tabIndex === 2) {
      params = params.set('unappliedOnly', 'true');
    } else if (this.tabIndex === 3) {
      params = params.set('status', 'Reversed');
    }

    this.http
      .get<PagedResult<PaymentRow>>('/api/payments', { params })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (res) => {
          this.rows.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load payments.')),
      });
  }

  openDetail(publicId: string): void {
    this.detailLoading.set(true);
    this.selected.set(null);
    this.http
      .get<PaymentDetail>(`/api/payments/${publicId}`)
      .pipe(finalize(() => this.detailLoading.set(false)))
      .subscribe({
        next: (detail) => this.selected.set(detail),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load payment.')),
      });
  }

  closeDetail(): void {
    this.selected.set(null);
  }

  recordPayment(): void {
    if (!this.newAmount || this.newAmount <= 0) {
      return;
    }
    this.saving.set(true);
    this.http
      .post<PaymentDetail>('/api/payments', {
        amount: this.newAmount,
        reference: this.newReference || null,
        receivedDate: this.newReceivedDate,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.newAmount = null;
          this.newReference = '';
          this.load();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to record payment.')),
      });
  }
}
