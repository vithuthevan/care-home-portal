import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { getApiErrorMessage } from '../../../../core/api-error';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';

interface PaymentDetail {
  publicId: string;
  receivedDate: string;
  reference?: string | null;
  payerName?: string | null;
  amount: number;
  allocatedAmount: number;
  unappliedAmount: number;
  status: string;
  source: string;
  currency: string;
  notes?: string | null;
  createdAt: string;
  reversedAt?: string | null;
  reversalReason?: string | null;
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

interface AllocationCandidate {
  invoicePublicId: string;
  invoiceNumber: string;
  residentName?: string | null;
  funderName: string;
  careHomeName: string;
  outstandingAmount: number;
  defaultAllocationAmount: number;
}

interface SelectedAllocation {
  invoicePublicId: string;
  invoiceNumber: string;
  amount: number;
  outstandingAmount: number;
}

@Component({
  selector: 'app-payment-detail',
  imports: [
    FormsModule,
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    DisplayDatePipe,
    StatusBadgeComponent,
  ],
  templateUrl: './payment-detail.html',
})
export class PaymentDetailPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly breadcrumbs = inject(BreadcrumbService);

  loading = signal(true);
  errorMessage = signal<string | null>(null);
  detail = signal<PaymentDetail | null>(null);

  candidatesLoading = signal(false);
  candidates = signal<AllocationCandidate[]>([]);
  selected = signal<Record<string, SelectedAllocation>>({});

  allocateLoading = signal(false);
  reversePaymentLoading = signal(false);
  invoiceSearch = '';

  readonly selectedTotal = computed(() =>
    Object.values(this.selected()).reduce((sum, row) => sum + (row.amount || 0), 0),
  );

  readonly remainingAfterSelection = computed(() => {
    const d = this.detail();
    if (!d) {
      return 0;
    }
    return Math.max(0, d.unappliedAmount - this.selectedTotal());
  });

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((query) => {
      const search = query.get('search');
      if (search) {
        this.invoiceSearch = search;
      }
    });

    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.load(id);
      }
    });
  }

  load(publicId: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.http
      .get<PaymentDetail>(`/api/payments/${publicId}`)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (d) => {
          this.detail.set(d);
          this.selected.set({});
          this.breadcrumbs.set([
            { label: 'Payments', routerLink: '/payments' },
            { label: d.reference || 'Payment' },
          ]);
          const search = this.route.snapshot.queryParamMap.get('search');
          if (search && !this.invoiceSearch) {
            this.invoiceSearch = search;
          }
          if (d.unappliedAmount > 0 && d.status !== 'Reversed') {
            this.loadCandidates(publicId);
          }
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load payment.')),
      });
  }

  loadCandidates(publicId: string): void {
    this.candidatesLoading.set(true);
    let url = `/api/payments/${publicId}/allocation-candidates`;
    if (this.invoiceSearch.trim()) {
      url += `?search=${encodeURIComponent(this.invoiceSearch.trim())}`;
    }
    this.http
      .get<AllocationCandidate[]>(url)
      .pipe(finalize(() => this.candidatesLoading.set(false)))
      .subscribe({
        next: (rows) => this.candidates.set(rows),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load invoices.')),
      });
  }

  toggleCandidate(row: AllocationCandidate, checked: boolean): void {
    const map = { ...this.selected() };
    if (!checked) {
      delete map[row.invoicePublicId];
      this.selected.set(map);
      return;
    }
    map[row.invoicePublicId] = {
      invoicePublicId: row.invoicePublicId,
      invoiceNumber: row.invoiceNumber,
      amount: row.defaultAllocationAmount,
      outstandingAmount: row.outstandingAmount,
    };
    this.selected.set(map);
  }

  isSelected(publicId: string): boolean {
    return publicId in this.selected();
  }

  amountFor(publicId: string): number {
    return this.selected()[publicId]?.amount ?? 0;
  }

  updateAmount(publicId: string, value: number): void {
    const map = { ...this.selected() };
    const row = map[publicId];
    if (!row) {
      return;
    }
    map[publicId] = { ...row, amount: value };
    this.selected.set(map);
  }

  submitAllocation(): void {
    const d = this.detail();
    if (!d) {
      return;
    }
    const lines = Object.values(this.selected()).filter((x) => x.amount > 0);
    if (lines.length === 0) {
      return;
    }
    if (this.selectedTotal() > d.unappliedAmount) {
      this.errorMessage.set('Selected total exceeds unapplied payment balance.');
      return;
    }

    this.allocateLoading.set(true);
    this.http
      .post<PaymentDetail>(`/api/payments/${d.publicId}/allocations`, {
        allocations: lines.map((l) => ({
          invoicePublicId: l.invoicePublicId,
          amount: l.amount,
        })),
      })
      .pipe(finalize(() => this.allocateLoading.set(false)))
      .subscribe({
        next: (updated) => {
          this.detail.set(updated);
          this.selected.set({});
          this.toast.success('Allocation saved.');
          if (updated.unappliedAmount > 0) {
            this.loadCandidates(updated.publicId);
          } else {
            this.candidates.set([]);
          }
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Allocation failed.')),
      });
  }

  reverseAllocation(alloc: PaymentAllocationRow): void {
    const d = this.detail();
    if (!d || alloc.isReversed) {
      return;
    }
    this.confirm
      .confirm({
        title: 'Reverse allocation',
        message: `Reverse £${alloc.allocatedAmount.toFixed(2)} from ${alloc.invoiceNumber}?`,
        confirmLabel: 'Reverse',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.http
          .post<PaymentDetail>(
            `/api/payments/${d.publicId}/allocations/${alloc.publicId}/reverse`,
            { reason: 'UI correction' },
          )
          .subscribe({
            next: (updated) => {
              this.detail.set(updated);
              this.toast.success('Allocation reversed.');
              this.loadCandidates(updated.publicId);
            },
            error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Reversal failed.')),
          });
      });
  }

  confirmReversePayment(): void {
    const d = this.detail();
    if (!d || d.status === 'Reversed') {
      return;
    }
    this.confirm
      .confirm({
        title: 'Reverse payment',
        message:
          'Reversing this payment will restore outstanding balances on affected invoices. This cannot be undone from the invoice screen alone.',
        confirmLabel: 'Reverse payment',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.reversePaymentLoading.set(true);
        this.http
          .post<PaymentDetail>(`/api/payments/${d.publicId}/reverse`, { reason: 'User reversed payment' })
          .pipe(finalize(() => this.reversePaymentLoading.set(false)))
          .subscribe({
            next: (updated) => {
              this.detail.set(updated);
              this.candidates.set([]);
              this.toast.success('Payment reversed.');
            },
            error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Payment reversal failed.')),
          });
      });
  }
}
