import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, isDevMode, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { LabeledStatusComponent } from '../../../../shared/ui/labeled-status';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-invoice-detail',
  imports: [
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    DisplayDatePipe,
    LabeledStatusComponent,
  ],
  templateUrl: './invoice-detail.html',
})
export class InvoiceDetailPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);
  readonly entityRouteKey = entityRouteKey;
  readonly invoice = signal<any | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  readonly infoHint = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isPdfLoading = signal(false);
  readonly isSending = signal(false);
  readonly isPaying = signal(false);

  /** Legacy manual flag only — hidden when real payment allocations drive collection status. */
  showLegacyPaymentStatusActions(): boolean {
    const inv = this.invoice();
    if (!inv || !this.auth.canWrite()) {
      return false;
    }
    const paid = inv.paidAmount ?? 0;
    return paid <= 0 && inv.paymentStatus !== 'Paid';
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const key = params.get('id') ?? '';
      this.loadInvoice(key);
    });
  }

  private loadInvoice(key: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.info.set(null);
    this.infoHint.set(null);
    this.invoice.set(null);

    this.http
      .get(`/api/invoices/${key}`)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (invoice: any) => {
          this.invoice.set(invoice);
          this.breadcrumbs.set([
            { label: 'Billing', routerLink: '/billing' },
            { label: 'Invoices', routerLink: '/invoices' },
            { label: invoice.invoiceNumber || 'Invoice' },
          ]);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice.')),
      });
  }

  pdf(): void {
    const current = this.invoice();
    if (!current) {
      return;
    }

    this.isPdfLoading.set(true);
    this.http
      .get(`/api/invoices/${current.id}/pdf`, { responseType: 'blob' })
      .pipe(finalize(() => this.isPdfLoading.set(false)))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          window.open(url, '_blank');
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to download PDF.')),
      });
  }

  send(): void {
    const current = this.invoice();
    if (!current || this.isSending()) {
      return;
    }

    this.isSending.set(true);
    this.http
      .post<{ simulated?: boolean }>(`/api/invoices/${current.id}/send`, {})
      .pipe(finalize(() => this.isSending.set(false)))
      .subscribe({
        next: (result) => {
          const simulated = result?.simulated === true;
          const message = simulated
            ? 'Invoice email workflow completed successfully.'
            : 'Invoice email processed successfully.';
          this.info.set(message);
          this.infoHint.set(
            simulated && isDevMode()
              ? 'Development note: delivery is simulated; the send is recorded and audited.'
              : null,
          );
          this.toast.success(message);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Email could not be sent.')),
      });
  }

  confirmPay(status: string): void {
    const current = this.invoice();
    if (!current || current.paymentStatus === status) {
      return;
    }

    const number = current.invoiceNumber || 'this invoice';
    const message =
      status === 'Paid'
        ? `Mark ${number} as paid without recording a payment? This updates the invoice payment flag only — it does not create a payment or allocation. To record cash received, use Payments.`
        : `Mark ${number} as not paid? This updates the invoice payment flag only.`;
    this.confirm
      .confirm({
        title: 'Legacy: payment status flag',
        message,
        confirmLabel: status === 'Paid' ? 'Mark as paid' : 'Mark as not paid',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.pay(status);
      });
  }

  private pay(status: string): void {
    const current = this.invoice();
    if (!current) {
      return;
    }

    this.isPaying.set(true);
    this.http
      .post(`/api/invoices/${current.id}/payment-status`, { paymentStatus: status })
      .pipe(finalize(() => this.isPaying.set(false)))
      .subscribe({
        next: () => {
          this.invoice.set({ ...current, paymentStatus: status });
          this.toast.success('Payment status updated.');
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Payment update failed.')),
      });
  }

  careHomeDashboardLink(inv: {
    careHomeId?: number;
    careHomePublicId?: string;
  }): string[] | null {
    if (!inv.careHomeId) {
      return null;
    }
    return [
      '/care-homes',
      entityRouteKey({ id: inv.careHomeId, publicId: inv.careHomePublicId }),
      'dashboard',
    ];
  }

  companyDetailLink(inv: { companyId?: number; companyPublicId?: string }): string[] | null {
    if (!inv.companyId) {
      return null;
    }
    return ['/companies', entityRouteKey({ id: inv.companyId, publicId: inv.companyPublicId })];
  }

  fundingAuthorityEditLink(inv: {
    fundingAuthorityId?: number;
    fundingAuthorityPublicId?: string;
  }): string[] | null {
    if (!inv.fundingAuthorityId) {
      return null;
    }
    return [
      '/funding-authorities',
      entityRouteKey({ id: inv.fundingAuthorityId, publicId: inv.fundingAuthorityPublicId }),
      'edit',
    ];
  }

  paymentsHandoffQueryParams(inv: {
    invoiceNumber?: string;
    publicId?: string;
  }): Record<string, string> {
    const params: Record<string, string> = {};
    if (inv.invoiceNumber) {
      params['search'] = inv.invoiceNumber;
    }
    if (inv.publicId) {
      params['invoicePublicId'] = inv.publicId;
    }
    return params;
  }

  lineAmountHint(line: {
    amountBasis?: string | null;
    eligibleDays?: number;
    rateAmount?: number;
    rateFrequency?: string;
  }): string | null {
    if (line.amountBasis?.trim()) {
      return line.amountBasis.trim();
    }
    if (line.eligibleDays === undefined || line.eligibleDays === null) {
      return null;
    }
    const rate =
      line.rateAmount != null ? `£${line.rateAmount.toFixed(2)} ${line.rateFrequency || ''}`.trim() : '';
    if (rate) {
      return `Line amount reflects ${line.eligibleDays} eligible day(s) at ${rate} (as calculated by billing).`;
    }
    return `Line amount reflects ${line.eligibleDays} eligible day(s) (as calculated by billing).`;
  }

  clientProfileLink(line: { clientId?: number; clientPublicId?: string }): string[] | null {
    if (!line.clientId) {
      return null;
    }
    return ['/clients', entityRouteKey({ id: line.clientId, publicId: line.clientPublicId })];
  }

  creditNoteQueryParams(inv: {
    id: number;
    invoiceNumber?: string;
    periodStart?: string;
    periodEnd?: string;
    lines?: { clientId?: number; clientName?: string; clientReference?: string }[];
  }): Record<string, string | number> {
    const line = inv.lines?.[0];
    const params: Record<string, string | number> = { invoiceId: inv.id };
    if (inv.invoiceNumber) {
      params['invoiceNumber'] = inv.invoiceNumber;
    }
    if (line?.clientId) {
      params['clientId'] = line.clientId;
    }
    if (line?.clientName) {
      params['clientName'] = line.clientName;
    }
    if (line?.clientReference) {
      params['clientReference'] = line.clientReference;
    }
    const start = this.toDateParam(inv.periodStart);
    const end = this.toDateParam(inv.periodEnd);
    if (start) {
      params['periodStart'] = start;
    }
    if (end) {
      params['periodEnd'] = end;
    }
    return params;
  }

  private toDateParam(value: string | undefined): string {
    if (!value) {
      return '';
    }
    return value.length >= 10 ? value.slice(0, 10) : value;
  }

  voidInvoice(): void {
    const current = this.invoice();
    if (!current) {
      return;
    }

    this.confirm
      .confirm({
        title: 'Void invoice',
        message: 'Void this invoice? The record is retained.',
        confirmLabel: 'Void',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.http.post(`/api/invoices/${current.id}/void`, {}).subscribe({
          next: () => {
            this.invoice.set({ ...current, status: 'Void' });
            this.toast.success('Invoice voided.');
          },
          error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Void failed.')),
        });
      });
  }
}
