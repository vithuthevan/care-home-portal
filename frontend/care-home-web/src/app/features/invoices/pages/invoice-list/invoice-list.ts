import { DecimalPipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { LabeledStatusComponent } from '../../../../shared/ui/labeled-status';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { ToastService } from '../../../../shared/ui/toast.service';
import { PagedResult } from '../../../../core/models';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { MatIconModule } from '@angular/material/icon';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-invoice-list',
  imports: [
    FormsModule,
    RouterLink,
    DecimalPipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    DisplayDatePipe,
    LabeledStatusComponent,
    StatusBadgeComponent,
    FilterBarComponent,
    TablePaginationComponent,
    IconActionButtonComponent,
    MatIconModule,
  ],
  templateUrl: './invoice-list.html',
})
export class InvoiceListPage implements OnInit {
  readonly entityRouteKey = entityRouteKey;
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly items = signal<any[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly bulkMessage = signal<string | null>(null);
  page = 1;
  pageSize = 20;
  invoiceNumber = '';
  status = '';
  paymentStatus = '';
  selected = new Set<number>();
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const payment = params.get('paymentStatus');
    const number = params.get('invoiceNumber');
    if (payment) {
      this.paymentStatus = payment;
    }
    if (number) {
      this.invoiceNumber = number;
    }
    this.load();
  }

  onSearchChange(): void {
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 300);
  }

  selectedItems(): any[] {
    return this.items().filter((item) => this.selected.has(item.id));
  }

  canMarkPaid(): boolean {
    const selected = this.selectedItems();
    return selected.length > 0 && selected.some((item) => item.paymentStatus !== 'Paid');
  }

  canMarkUnpaid(): boolean {
    const selected = this.selectedItems();
    return selected.length > 0 && selected.some((item) => item.paymentStatus !== 'NotPaid');
  }

  hasActiveFilters(): boolean {
    return !!(this.invoiceNumber || this.status || this.paymentStatus);
  }

  clearFilters(): void {
    this.invoiceNumber = '';
    this.status = '';
    this.paymentStatus = '';
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

  formatPeriod(item: { periodStart?: string; periodEnd?: string }): string {
    const pipe = new DisplayDatePipe();
    if (!item.periodStart || !item.periodEnd) {
      return '—';
    }
    return `${pipe.transform(item.periodStart)} – ${pipe.transform(item.periodEnd)}`;
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    if (this.invoiceNumber) params = params.set('invoiceNumber', this.invoiceNumber);
    if (this.status) params = params.set('status', this.status);
    if (this.paymentStatus) params = params.set('paymentStatus', this.paymentStatus);
    this.http
      .get<PagedResult<any>>('/api/invoices', { params })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoices.')),
      });
  }

  toggle(id: number, checked: boolean): void {
    if (checked) this.selected.add(id);
    else this.selected.delete(id);
  }

  bulkSend(): void {
    this.http.post<any>('/api/invoices/bulk-send', { invoiceIds: [...this.selected] }).subscribe({
      next: (result) => {
        this.bulkMessage.set(
          `Succeeded ${result.succeeded}, failed ${result.failed}, skipped ${result.skipped}.`,
        );
        this.toast.success('Email queued/sent successfully.');
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Bulk send failed.')),
    });
  }

  bulkPay(status: string): void {
    this.http
      .post('/api/invoices/bulk-payment-status', {
        invoiceIds: [...this.selected],
        paymentStatus: status,
      })
      .subscribe({
        next: () => this.load(),
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Bulk payment update failed.')),
      });
  }
}
