import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { CurrencyDisplayComponent } from '../../../../shared/ui/currency-display';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header';
import { LabeledStatusComponent } from '../../../../shared/ui/labeled-status';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { Client } from '../../models/client.model';
import { ClientService } from '../../services/client.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import {
  EntitySummaryItem,
  EntitySummaryStripComponent,
} from '../../../../shared/ui/entity-summary-strip';
import { entityRouteKey } from '../../../../shared/routing/entity-route';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';

interface FundingContractView {
  id: number;
  fundingAuthorityName: string;
  invoiceCategoryName: string;
  nominalCode: string;
  contractStartDate: string;
  contractEndDate: string | null;
  status: string;
  rates?: FundingRateView[];
}

interface FundingRateView {
  id: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  frequency: string;
  amount: number;
}

@Component({
  selector: 'app-client-profile',
  imports: [
    RouterLink,
    FormsModule,
    DecimalPipe,
    DisplayDatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTabsModule,
    MatIconModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    CurrencyDisplayComponent,
    SectionHeaderComponent,
    LabeledStatusComponent,
    EmptyStateComponent,
    EntitySummaryStripComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './client-profile.html',
  styleUrl: './client-profile.scss',
})
export class ClientProfilePage implements OnInit {
  readonly entityRouteKey = entityRouteKey;

  companyRouteKey(client: Client): string {
    return entityRouteKey({ id: client.companyId, publicId: client.companyPublicId });
  }

  careHomeRouteKey(client: Client): string {
    return entityRouteKey({ id: client.careHomeId, publicId: client.careHomePublicId });
  }

  private readonly route = inject(ActivatedRoute);
  private readonly clients = inject(ClientService);
  private readonly displayDate = new DisplayDatePipe();
  private readonly http = inject(HttpClient);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);

  readonly client = signal<Client | null>(null);
  readonly contracts = signal<FundingContractView[]>([]);
  readonly invoices = signal<any[]>([]);
  readonly authorities = signal<any[]>([]);
  readonly categories = signal<any[]>([]);
  readonly nominals = signal<any[]>([]);
  readonly isLoading = signal(false);
  readonly isSavingContract = signal(false);
  readonly isSavingRate = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedTabIndex = signal(0);

  readonly primaryContract = computed(() => {
    const list = this.contracts();
    return list.find((c) => c.status === 'Active') ?? list[0] ?? null;
  });

  readonly currentRate = computed(() => {
    const contract = this.primaryContract();
    if (!contract?.rates?.length) {
      return null;
    }
    const sorted = [...contract.rates].sort((a, b) =>
      a.effectiveFrom < b.effectiveFrom ? 1 : a.effectiveFrom > b.effectiveFrom ? -1 : 0,
    );
    return sorted.find((r) => !r.effectiveTo) ?? sorted[0];
  });

  readonly outstandingAmount = computed(() =>
    this.invoices()
      .filter((inv) => inv.status !== 'Void')
      .reduce((sum, inv) => {
        const outstanding =
          inv.outstandingAmount !== undefined && inv.outstandingAmount !== null
            ? Number(inv.outstandingAmount)
            : inv.paymentStatus === 'NotPaid'
              ? Number(inv.totalAmount) || 0
              : 0;
        return sum + outstanding;
      }, 0),
  );

  readonly summaryStrip = computed((): EntitySummaryItem[] => {
    const contract = this.primaryContract();
    const rate = this.currentRate();
    const outstanding = this.outstandingAmount();
    const period = this.billingPeriodLabel();
    return [
      {
        label: 'Funding',
        value: contract?.fundingAuthorityName ?? 'Not set',
        hint: contract?.invoiceCategoryName,
        tone: 'finance',
      },
      {
        label: 'Weekly rate',
        value: rate ? `£${rate.amount.toFixed(2)}` : '—',
        hint: rate ? this.rateSuffix(rate.frequency).replace(/^\s*/, '') : 'Add a rate to bill',
      },
      {
        label: 'Current period',
        value: period,
        hint: 'Suggested billing month',
      },
      {
        label: 'Outstanding',
        value: `£${outstanding.toFixed(2)}`,
        tone: outstanding > 0 ? 'attention' : 'success',
        hint: outstanding > 0 ? 'Unpaid invoices' : 'All clear',
      },
    ];
  });

  newContract = {
    fundingAuthorityId: 0,
    invoiceCategoryId: 0,
    nominalCodeId: 0,
    contractStartDate: '',
    contractEndDate: '',
  };
  newRate = {
    contractId: 0,
    effectiveFrom: '',
    effectiveTo: '',
    frequency: 'Weekly',
    amount: 0,
    notes: '',
  };

  ngOnInit(): void {
    this.http.get<any[]>('/api/funding-authorities?activeOnly=true').subscribe({
      next: (x) => this.authorities.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding authorities.')),
    });
    this.http.get<any[]>('/api/invoice-categories?activeOnly=true').subscribe({
      next: (x) => this.categories.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice categories.')),
    });
    this.http.get<any[]>('/api/nominal-codes?activeOnly=true').subscribe({
      next: (x) => this.nominals.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load nominal codes.')),
    });

    this.route.paramMap.subscribe((params) => {
      const key = params.get('id') ?? '';
      this.selectedTabIndex.set(0);
      this.isLoading.set(true);
      this.errorMessage.set(null);
      this.client.set(null);
      this.clients
        .getClient(key)
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: (client) => {
            this.client.set(client);
            this.breadcrumbs.set([
              { label: 'Residents', routerLink: '/clients' },
              {
                label: `${client.firstName} ${client.lastName} — ${client.referenceNumber}`.trim(),
              },
            ]);
            this.loadContracts();
            this.loadInvoices();
          },
          error: (error) =>
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to load resident.')),
        });
    });
  }

  formatContractRange(contract: FundingContractView): string {
    const end = contract.contractEndDate
      ? this.displayDate.transform(contract.contractEndDate)
      : 'Open ended';
    return `${this.displayDate.transform(contract.contractStartDate)} → ${end}`;
  }

  formatRateRange(rate: FundingRateView): string {
    const end = rate.effectiveTo ? this.displayDate.transform(rate.effectiveTo) : 'Open ended';
    return `${this.displayDate.transform(rate.effectiveFrom)} → ${end}`;
  }

  rateSuffix(frequency: string): string {
    const f = (frequency || '').toLowerCase();
    if (f === 'weekly') return '/ week';
    if (f === 'monthly') return '/ month';
    if (f === 'daily') return '/ day';
    return frequency ? `/ ${frequency}` : '';
  }

  openFundingTab(): void {
    this.selectedTabIndex.set(1);
  }

  openInvoicesTab(): void {
    this.selectedTabIndex.set(3);
  }

  billingQueryParams(client: Client): Record<string, string | number> {
    const period = this.suggestedBillingPeriod();
    return {
      careHomeId: client.careHomeId,
      clientId: client.id,
      clientName: `${client.firstName} ${client.lastName}`.trim(),
      periodStart: period.start,
      periodEnd: period.end,
    };
  }

  billingPeriodLabel(): string {
    const start = new Date(this.suggestedBillingPeriod().start);
    return start.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
  }

  private suggestedBillingPeriod(): { start: string; end: string } {
    const today = new Date();
    const start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
    const end = new Date(today.getFullYear(), today.getMonth(), 0);
    return {
      start: this.toDateInput(start),
      end: this.toDateInput(end),
    };
  }

  private toDateInput(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  loadContracts(): void {
    const current = this.client();
    if (!current) return;
    this.http.get<FundingContractView[]>(`/api/clients/${current.id}/funding-contracts`).subscribe({
      next: (x) => this.contracts.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding contracts.')),
    });
  }

  loadInvoices(): void {
    const current = this.client();
    if (!current) return;
    this.http.get<any>('/api/invoices', { params: { clientId: current.id } }).subscribe({
      next: (x) => this.invoices.set(x.items ?? []),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoices.')),
    });
  }

  private resetNewContract(): void {
    this.newContract = {
      fundingAuthorityId: 0,
      invoiceCategoryId: 0,
      nominalCodeId: 0,
      contractStartDate: '',
      contractEndDate: '',
    };
  }

  private resetNewRate(): void {
    this.newRate = {
      contractId: 0,
      effectiveFrom: '',
      effectiveTo: '',
      frequency: 'Weekly',
      amount: 0,
      notes: '',
    };
  }

  saveContract(): void {
    const current = this.client();
    if (!current || this.isSavingContract()) return;
    this.isSavingContract.set(true);
    this.http
      .post(`/api/clients/${current.id}/funding-contracts`, {
        ...this.newContract,
        contractEndDate: this.newContract.contractEndDate || null,
      })
      .pipe(finalize(() => this.isSavingContract.set(false)))
      .subscribe({
        next: () => {
          this.resetNewContract();
          this.loadContracts();
          this.openFundingTab();
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(
              error,
              'Unable to save the funding contract. Please check the contract dates and try again.',
            ),
          ),
      });
  }

  addRate(): void {
    if (this.newRate.amount <= 0) {
      this.errorMessage.set('Rate amount must be greater than zero.');
      return;
    }
    if (this.isSavingRate()) {
      return;
    }

    this.isSavingRate.set(true);
    this.http
      .post(`/api/funding-contracts/${this.newRate.contractId}/rates`, {
        effectiveFrom: this.newRate.effectiveFrom,
        effectiveTo: this.newRate.effectiveTo || null,
        frequency: this.newRate.frequency,
        amount: this.newRate.amount,
        notes: this.newRate.notes,
        closePreviousOpenEnded: true,
      })
      .pipe(finalize(() => this.isSavingRate.set(false)))
      .subscribe({
        next: () => {
          this.resetNewRate();
          this.loadContracts();
          this.openFundingTab();
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(error, 'Unable to add rate. Check the dates and amount.'),
          ),
      });
  }
}
