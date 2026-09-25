import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
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

interface FundingContractView {
  id: number;
  fundingAuthorityId: number;
  fundingAuthorityPublicId?: string;
  fundingAuthorityName: string;
  invoiceCategoryName: string;
  nominalCode: string;
  invoiceTemplateId?: number | null;
  invoiceTemplateName?: string | null;
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

interface ResidentInvoiceRow {
  id: number;
  publicId?: string;
  invoiceNumber: string;
  invoiceDate: string;
  periodStart?: string;
  periodEnd?: string;
  totalAmount: number;
  status: string;
  paymentStatus: string;
  collectionStatus?: string;
  outstandingAmount?: number;
}

@Component({
  selector: 'app-client-profile',
  imports: [
    RouterLink,
    DecimalPipe,
    DisplayDatePipe,
    MatButtonModule,
    MatTabsModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    CurrencyDisplayComponent,
    SectionHeaderComponent,
    LabeledStatusComponent,
    EmptyStateComponent,
    EntitySummaryStripComponent,
  ],
  templateUrl: './client-profile.html',
  styleUrl: './client-profile.scss',
})
export class ClientProfilePage implements OnInit {
  readonly entityRouteKey = entityRouteKey;

  companyRouteKey(client: Client): string {
    return entityRouteKey({ id: client.companyId ?? 0, publicId: client.companyPublicId });
  }

  careHomeRouteKey(client: Client): string {
    return entityRouteKey({ id: client.careHomeId, publicId: client.careHomePublicId });
  }

  invoiceRouteKey(invoice: ResidentInvoiceRow): string {
    return entityRouteKey({ id: invoice.id, publicId: invoice.publicId });
  }

  private readonly route = inject(ActivatedRoute);
  private readonly clients = inject(ClientService);
  private readonly displayDate = new DisplayDatePipe();
  private readonly http = inject(HttpClient);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);

  readonly client = signal<Client | null>(null);
  readonly contracts = signal<FundingContractView[]>([]);
  readonly invoices = signal<ResidentInvoiceRow[]>([]);
  readonly isLoading = signal(false);
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

  readonly profileSubtitle = computed(() => {
    const client = this.client();
    if (!client) {
      return '';
    }
    return `Resident · ${client.referenceNumber} · ${client.careHomeName}`;
  });

  readonly summaryStrip = computed((): EntitySummaryItem[] => {
    const contract = this.primaryContract();
    const rate = this.currentRate();
    const outstanding = this.outstandingAmount();
    const period = this.billingPeriodLabel();
    const fundingConfigured = Boolean(contract);

    return [
      {
        label: 'Funding',
        value: fundingConfigured
          ? (contract!.fundingAuthorityName ?? '—')
          : 'Funding not configured',
        hint: fundingConfigured
          ? contract!.invoiceCategoryName
          : 'No funding contract or current rate is available for billing.',
        tone: fundingConfigured ? 'default' : 'attention',
      },
      {
        label: 'Current rate',
        value: rate ? `£${rate.amount.toFixed(2)}` : fundingConfigured ? 'No rate set' : '—',
        hint: rate
          ? `${this.rateSuffix(rate.frequency).replace(/^\s*/, '')} · effective from ${this.displayDate.transform(rate.effectiveFrom)}`
          : fundingConfigured
            ? 'Add a rate before billing'
            : undefined,
        tone: rate ? 'default' : fundingConfigured ? 'attention' : 'default',
      },
      {
        label: 'Current billing period',
        value: period,
        hint: 'Suggested month for the next billing run',
      },
      {
        label: 'Outstanding',
        value: `£${outstanding.toFixed(2)}`,
        tone: outstanding > 0 ? 'attention' : 'success',
        hint: outstanding > 0 ? 'Unpaid invoice balance' : 'No outstanding balance',
      },
    ];
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const key = params.get('id') ?? '';
      this.applyTabFromQuery();
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
              { label: `${client.firstName} ${client.lastName}`.trim() },
            ]);
            this.loadContracts();
            this.loadInvoices();
          },
          error: (error) =>
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to load resident.')),
        });
    });

    this.route.queryParamMap.subscribe(() => this.applyTabFromQuery());
  }

  private applyTabFromQuery(): void {
    const tab = (this.route.snapshot.queryParamMap.get('tab') ?? '').toLowerCase();
    const index =
      tab === 'funding' ? 1 : tab === 'billing' ? 2 : tab === 'invoices' ? 3 : 0;
    this.selectedTabIndex.set(index);
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

  formatInvoicePeriod(invoice: ResidentInvoiceRow): string {
    if (!invoice.periodStart || !invoice.periodEnd) {
      return '—';
    }
    return `${this.displayDate.transform(invoice.periodStart)} – ${this.displayDate.transform(invoice.periodEnd)}`;
  }

  currentRateForContract(contract: FundingContractView): FundingRateView | null {
    if (!contract.rates?.length) {
      return null;
    }
    const sorted = [...contract.rates].sort((a, b) =>
      a.effectiveFrom < b.effectiveFrom ? 1 : a.effectiveFrom > b.effectiveFrom ? -1 : 0,
    );
    return sorted.find((r) => !r.effectiveTo) ?? sorted[0];
  }

  rateSuffix(frequency: string): string {
    const f = (frequency || '').toLowerCase();
    if (f === 'weekly') return '/ week';
    if (f === 'monthly') return '/ month';
    if (f === 'daily') return '/ day';
    return frequency ? `/ ${frequency}` : '';
  }

  openInvoicesTab(): void {
    this.selectedTabIndex.set(3);
  }

  billingQueryParams(client: Client): Record<string, string | number> {
    const period = this.suggestedBillingPeriod();
    const params: Record<string, string | number> = {
      careHome: entityRouteKey({ id: client.careHomeId, publicId: client.careHomePublicId }),
      client: entityRouteKey(client),
      clientName: `${client.firstName} ${client.lastName}`.trim(),
      periodStart: period.start,
      periodEnd: period.end,
    };
    if (client.companyId) {
      params['company'] = entityRouteKey({
        id: client.companyId,
        publicId: client.companyPublicId,
      });
    }
    return params;
  }

  billingPeriodLabel(): string {
    const start = new Date(this.suggestedBillingPeriod().start);
    return start.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
  }

  fundingContractLink(client: Client): (string | number)[] {
    return ['/clients', entityRouteKey(client), 'funding', 'new'];
  }

  fundingContractEditLink(client: Client, contractId: number): (string | number)[] {
    return ['/clients', entityRouteKey(client), 'funding', contractId, 'edit'];
  }

  fundingRateLink(client: Client, contractId?: number): (string | number)[] {
    return ['/clients', entityRouteKey(client), 'funding', 'rates', 'new'];
  }

  fundingRateQueryParams(contractId?: number): Record<string, number> | null {
    if (!contractId) {
      return null;
    }
    return { contractId };
  }

  fundingAuthorityEditLink(contract: FundingContractView): string[] | null {
    if (!contract.fundingAuthorityId) {
      return null;
    }
    return [
      '/funding-authorities',
      entityRouteKey({
        id: contract.fundingAuthorityId,
        publicId: contract.fundingAuthorityPublicId,
      }),
      'edit',
    ];
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
    this.http.get<{ items?: ResidentInvoiceRow[] }>('/api/invoices', {
      params: { clientId: current.id },
    }).subscribe({
      next: (x) => this.invoices.set(x.items ?? []),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoices.')),
    });
  }
}
