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
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { KpiCardComponent } from '../../../../shared/ui/kpi-card';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

interface ReceivablesSummary {
  totalOutstanding: number;
  totalOverdue: number;
  dueThisWeek: number;
  days90Plus: number;
  ageing: ReceivablesAgeing;
  openInvoiceCount: number;
}

interface ReceivablesAgeing {
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  days90Plus: number;
}

interface ReceivableInvoiceRow {
  invoiceId: number;
  publicId: string;
  invoiceNumber: string;
  careHomeName: string;
  funderName: string;
  residentName?: string | null;
  invoiceDate: string;
  dueDate: string;
  originalAmount: number;
  creditedAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  daysOverdue: number;
  paymentStatus: string;
}

interface FunderSummary {
  fundingAuthorityId: number;
  funderName: string;
  totalOutstanding: number;
  ageing: ReceivablesAgeing;
}

interface CareHomeSummary {
  careHomeId: number;
  careHomeName: string;
  totalOutstanding: number;
  totalOverdue: number;
}

@Component({
  selector: 'app-receivables-workspace',
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
    FilterBarComponent,
    DisplayDatePipe,
    StatusBadgeComponent,
    TablePaginationComponent,
    KpiCardComponent,
  ],
  templateUrl: './receivables-workspace.html',
})
export class ReceivablesWorkspacePage implements OnInit {
  readonly entityRouteKey = entityRouteKey;
  private readonly http = inject(HttpClient);

  readonly summary = signal<ReceivablesSummary | null>(null);
  readonly invoices = signal<ReceivableInvoiceRow[]>([]);
  readonly funders = signal<FunderSummary[]>([]);
  readonly careHomes = signal<CareHomeSummary[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  page = 1;
  pageSize = 25;
  invoiceNumber = '';
  paymentStatus = '';
  overdueOnly = false;
  selectedTab = 0;

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http
      .get<ReceivablesSummary>('/api/receivables/summary')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (data) => this.summary.set(data),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load receivables.')),
      });

    this.loadInvoices();
    this.http.get<FunderSummary[]>('/api/receivables/funders').subscribe({
      next: (rows) => this.funders.set(rows),
    });
    this.http.get<CareHomeSummary[]>('/api/receivables/care-homes').subscribe({
      next: (rows) => this.careHomes.set(rows),
    });
  }

  loadInvoices(): void {
    let params = new HttpParams()
      .set('page', String(this.page))
      .set('pageSize', String(this.pageSize))
      .set('openReceivablesOnly', 'true');

    if (this.invoiceNumber.trim()) {
      params = params.set('invoiceNumber', this.invoiceNumber.trim());
    }
    if (this.paymentStatus) {
      params = params.set('paymentStatus', this.paymentStatus);
    }
    if (this.overdueOnly) {
      params = params.set('overdueOnly', 'true');
    }

    this.http.get<PagedResult<ReceivableInvoiceRow>>('/api/receivables/invoices', { params }).subscribe({
      next: (page) => {
        this.invoices.set(page.items);
        this.totalCount.set(page.totalCount);
      },
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load invoices.')),
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadInvoices();
  }

  invoiceLink(row: ReceivableInvoiceRow): string[] {
    return ['/invoices', entityRouteKey({ id: row.invoiceId, publicId: row.publicId })];
  }
}
