import { DecimalPipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
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
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { PagedResult } from '../../../../core/models';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { MatIconModule } from '@angular/material/icon';
import { entityRouteKey } from '../../../../shared/routing/entity-route';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';

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
  private readonly router = inject(Router);
  private readonly careHomesApi = inject(CareHomeService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);
  readonly items = signal<any[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly isBulkPaying = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly bulkMessage = signal<string | null>(null);
  page = 1;
  pageSize = 20;
  invoiceNumber = '';
  status = '';
  paymentStatus = '';
  filterCareHomeId = 0;
  filterDateFrom = '';
  filterDateTo = '';
  filterInvoiceIds: number[] = [];
  selected = new Set<number>();
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.breadcrumbs.set([
      { label: 'Billing', routerLink: '/billing' },
      { label: 'Invoices' },
    ]);
    this.route.queryParamMap.subscribe((params) => {
      const payment = params.get('paymentStatus');
      const number = params.get('invoiceNumber');
      if (payment) {
        this.paymentStatus = payment;
      }
      if (number) {
        this.invoiceNumber = number;
      }
      const careHomeKey = params.get('careHome');
      const careHomeId = Number(params.get('careHomeId') || 0);
      const from = params.get('from');
      const to = params.get('to');
      const idsParam = params.get('ids');
      this.filterInvoiceIds = idsParam
        ? idsParam
            .split(',')
            .map((s) => Number(s.trim()))
            .filter((id) => id > 0)
        : [];
      if (from) {
        this.filterDateFrom = from.length >= 10 ? from.slice(0, 10) : from;
      }
      if (to) {
        this.filterDateTo = to.length >= 10 ? to.slice(0, 10) : to;
      }
      if (careHomeKey) {
        this.applyCareHomeFilterKey(careHomeKey);
      } else {
        this.filterCareHomeId = careHomeId;
        if (careHomeId) {
          this.applyCareHomeBreadcrumb(careHomeId);
        } else {
          this.breadcrumbs.set([
      { label: 'Billing', routerLink: '/billing' },
      { label: 'Invoices' },
    ]);
        }
      }
      this.page = 1;
      this.load();
    });
  }

  private applyCareHomeFilterKey(key: string): void {
    this.careHomesApi.getCareHome(key).subscribe({
      next: (home) => {
        this.filterCareHomeId = home.id;
        this.breadcrumbs.set([
          { label: 'Care Homes', routerLink: '/care-homes' },
          { label: home.name, routerLink: ['/care-homes', entityRouteKey(home), 'dashboard'] },
          { label: 'Invoices' },
        ]);
        this.load();
      },
      error: () => {
        this.filterCareHomeId = 0;
        this.breadcrumbs.set([
      { label: 'Billing', routerLink: '/billing' },
      { label: 'Invoices' },
    ]);
        this.load();
      },
    });
  }

  private applyCareHomeBreadcrumb(careHomeId: number): void {
    this.careHomesApi.getCareHomes().subscribe({
      next: (homes) => {
        const home = homes.find((h) => h.id === careHomeId);
        if (home) {
          this.breadcrumbs.set([
            { label: 'Care Homes', routerLink: '/care-homes' },
            { label: home.name, routerLink: ['/care-homes', entityRouteKey(home), 'dashboard'] },
            { label: 'Invoices' },
          ]);
        } else {
          this.breadcrumbs.set([
      { label: 'Billing', routerLink: '/billing' },
      { label: 'Invoices' },
    ]);
        }
      },
      error: () => this.breadcrumbs.set([{ label: 'Invoices' }]),
    });
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
    return !!(
      this.invoiceNumber ||
      this.status ||
      this.paymentStatus ||
      this.filterCareHomeId ||
      this.filterDateFrom ||
      this.filterDateTo ||
      this.filterInvoiceIds.length
    );
  }

  clearFilters(): void {
    this.invoiceNumber = '';
    this.status = '';
    this.paymentStatus = '';
    this.filterCareHomeId = 0;
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.filterInvoiceIds = [];
    this.page = 1;
    if (
      this.route.snapshot.queryParamMap.get('careHomeId') ||
      this.route.snapshot.queryParamMap.get('careHome') ||
      this.route.snapshot.queryParamMap.get('from') ||
      this.route.snapshot.queryParamMap.get('to') ||
      this.route.snapshot.queryParamMap.get('ids')
    ) {
      void this.router.navigate(['/invoices']);
      return;
    }
    this.breadcrumbs.set([
      { label: 'Billing', routerLink: '/billing' },
      { label: 'Invoices' },
    ]);
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
    if (this.filterCareHomeId) params = params.set('careHomeId', this.filterCareHomeId);
    if (this.filterDateFrom) params = params.set('from', this.filterDateFrom);
    if (this.filterDateTo) params = params.set('to', this.filterDateTo);
    if (this.filterInvoiceIds.length) {
      params = params.set('ids', this.filterInvoiceIds.join(','));
    }
    this.http
      .get<PagedResult<any>>('/api/invoices', { params })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          if (this.filterInvoiceIds.length) {
            this.selected = new Set(result.items.map((item) => item.id));
          }
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
    if (this.isBulkPaying()) {
      return;
    }

    const markingPaid = status === 'Paid';
    const count = this.selected.size;
    const message = markingPaid
      ? `Mark ${count} selected invoice(s) as paid? This updates payment status only. It does not record a bank receipt.`
      : `Mark ${count} selected invoice(s) as unpaid? This updates payment status only.`;
    this.confirm
      .confirm({
        title: 'Update payment status',
        message,
        confirmLabel: markingPaid ? 'Mark as paid' : 'Mark as unpaid',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.isBulkPaying.set(true);
        this.errorMessage.set(null);
        this.http
          .post('/api/invoices/bulk-payment-status', {
            invoiceIds: [...this.selected],
            paymentStatus: status,
          })
          .pipe(finalize(() => this.isBulkPaying.set(false)))
          .subscribe({
            next: () => {
              this.toast.success(
                markingPaid ? 'Selected invoices marked as paid.' : 'Selected invoices marked as unpaid.',
              );
              this.load();
            },
            error: (error) =>
              this.errorMessage.set(getApiErrorMessage(error, 'Payment update failed.')),
          });
      });
  }
}
