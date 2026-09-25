import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import {
  WorkflowStepsComponent,
  WorkflowStep,
} from '../../../../shared/ui/workflow-steps';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ToastService } from '../../../../shared/ui/toast.service';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';
import {
  billingExceptionHeadline,
  billingExceptionLabel,
} from '../../../../shared/ui/billing-exception';
import { entityRouteKey } from '../../../../shared/routing/entity-route';
import { Company } from '../../../companies/models/company.model';
import { CompanyService } from '../../../companies/services/company.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { InvoiceCategory } from '../../../invoice-categories/models/invoice-category.model';
import { InvoiceCategoryService } from '../../../invoice-categories/services/invoice-category.service';
import { ClientService } from '../../../clients/services/client.service';
import { Client } from '../../../clients/models/client.model';

interface BillingExceptionView {
  severity?: string;
  code: string;
  message: string;
  clientId?: number | null;
  clientName?: string | null;
}

interface BillingPreviewLineView {
  clientId: number;
  clientName: string;
  serviceFrom: string;
  serviceTo: string;
  rate: number;
  frequency: string;
  amount: number;
}

interface BillingCoverageView {
  clientId: number;
  clientName: string;
  clientFundingContractId: number;
  alreadyBilledPeriods: { start: string; end: string; days: number }[];
  remainingBillablePeriods: { start: string; end: string; days: number }[];
  skippedAlreadyBilledDays: number;
}

interface BillingPreviewView {
  requestedPeriodStart: string;
  requestedPeriodEnd: string;
  lines?: BillingPreviewLineView[];
  exceptions?: BillingExceptionView[];
  coverage?: BillingCoverageView[];
  totalAmount: number;
  canGenerate: boolean;
}

export interface BillingReviewRow {
  clientId: number | null;
  clientName: string;
  periodLabel: string;
  rateLabel: string;
  amount: number;
  statusLabel: string;
  statusClass: string;
  reason: string | null;
}

@Component({
  selector: 'app-billing-workspace',
  imports: [
    FormsModule,
    RouterLink,
    DecimalPipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    DisplayDatePipe,
    PageHeaderComponent,
    ApiErrorComponent,
    WorkflowStepsComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './billing-workspace.html',
})
export class BillingWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly companiesApi = inject(CompanyService);
  private readonly homesApi = inject(CareHomeService);
  private readonly categoriesApi = inject(InvoiceCategoryService);
  private readonly clientsApi = inject(ClientService);
  private readonly route = inject(ActivatedRoute);
  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly companies = signal<Company[]>([]);
  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly categories = signal<InvoiceCategory[]>([]);
  readonly clients = signal<Client[]>([]);
  readonly preview = signal<BillingPreviewView | null>(null);
  readonly generateResult = signal<{ invoiceCount: number; totalAmount: number } | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly isPreviewing = signal(false);
  readonly isGenerating = signal(false);
  readonly scopeEditing = signal(true);
  readonly contextClientName = signal<string | null>(null);

  readonly isWorking = computed(() => this.isPreviewing() || this.isGenerating());

  readonly workflowSteps: WorkflowStep[] = [
    { label: 'Scope', description: 'Company, home, period' },
    { label: 'Preview', description: 'Eligible residents' },
    { label: 'Review', description: 'Rates & exceptions' },
    { label: 'Generate', description: 'Create invoices' },
  ];

  readonly workflowActiveIndex = computed(() => {
    const preview = this.preview();
    if (!preview) {
      return 0;
    }
    if (!preview.canGenerate) {
      return 2;
    }
    return 3;
  });

  readonly workflowBlockedIndex = computed(() => {
    const preview = this.preview();
    if (preview && !preview.canGenerate) {
      return 3;
    }
    return null;
  });

  private readonly displayDatePipe = new DisplayDatePipe();

  companyId = 0;
  careHomeId = 0;
  invoiceCategoryId = 0;
  periodStart = '';
  periodEnd = '';
  selectedClientIds: number[] = [];

  ngOnInit(): void {
    this.companiesApi
      .getCompanies()
      .subscribe((x) => this.companies.set(x.filter((c) => c.isActive)));
    this.homesApi.getCareHomes().subscribe((x) => {
      this.careHomes.set(x.filter((h) => h.isActive));
      this.applyQueryContext();
    });
    this.categoriesApi
      .getInvoiceCategories()
      .subscribe((x) => this.categories.set(x.filter((c) => c.isActive)));
    this.clientsApi.getClients(undefined, undefined, false, 1, 200).subscribe((page) => {
      this.clients.set(page.items);
      this.applyQueryContext();
    });
    this.route.queryParamMap.subscribe(() => this.applyQueryContext());
  }

  runPreview(): void {
    this.errorMessage.set(null);
    this.generateResult.set(null);
    this.isPreviewing.set(true);
    this.http
      .post<BillingPreviewView>('/api/billing/preview', this.body())
      .pipe(finalize(() => this.isPreviewing.set(false)))
      .subscribe({
        next: (result) => {
          this.preview.set(result);
          this.scopeEditing.set(false);
        },
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Preview failed.')),
      });
  }

  generate(): void {
    if (!this.preview()?.canGenerate || this.isWorking()) {
      return;
    }
    this.errorMessage.set(null);
    this.isGenerating.set(true);
    this.http
      .post<{ invoiceCount: number; totalAmount: number }>('/api/billing/generate', this.body())
      .pipe(finalize(() => this.isGenerating.set(false)))
      .subscribe({
        next: (result) => {
          this.generateResult.set(result);
          this.toast.success('Invoice generated successfully.');
          this.runPreview();
        },
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Generation failed.')),
      });
  }

  enableScopeEditing(): void {
    this.scopeEditing.set(true);
  }

  exceptionLabel(code: string, message: string): string {
    return billingExceptionLabel(code, message);
  }

  exceptionHeadline(code: string): string {
    return billingExceptionHeadline(code);
  }

  blockingExceptions(previewData: BillingPreviewView): BillingExceptionView[] {
    return (previewData.exceptions ?? []).filter((item) => item.severity !== 'Info');
  }

  previewAttentionCount(previewData: BillingPreviewView): number {
    return this.blockingExceptions(previewData).length;
  }

  previewEligibleResidents(previewData: BillingPreviewView): number {
    const ids = new Set(
      (previewData.lines ?? [])
        .map((line) => line.clientId)
        .filter((id): id is number => typeof id === 'number' && id > 0),
    );
    return ids.size;
  }

  reviewRows(previewData: BillingPreviewView): BillingReviewRow[] {
    const rows: BillingReviewRow[] = [];
    const lineClientIds = new Set<number>();

    for (const line of previewData.lines ?? []) {
      lineClientIds.add(line.clientId);
      const error = this.primaryErrorForClient(previewData, line.clientId);
      const info = this.infoExceptionForClient(previewData, line.clientId);
      rows.push({
        clientId: line.clientId,
        clientName: line.clientName,
        periodLabel: this.formatLinePeriod(line),
        rateLabel: line.rate > 0 ? `£${line.rate.toFixed(2)} ${line.frequency}`.trim() : '—',
        amount: line.amount,
        statusLabel: error ? 'Cannot bill' : info ? this.exceptionHeadline(info.code) : 'Billable',
        statusClass: error
          ? 'billing-review-status--attention'
          : 'billing-review-status--ok',
        reason: error
          ? this.exceptionLabel(error.code, error.message)
          : info
            ? this.exceptionLabel(info.code, info.message)
            : null,
      });
    }

    for (const exception of this.blockingExceptions(previewData)) {
      if (!exception.clientId || lineClientIds.has(exception.clientId)) {
        continue;
      }
      rows.push({
        clientId: exception.clientId,
        clientName: exception.clientName ?? 'Resident',
        periodLabel: this.formatRequestedPeriod(previewData),
        rateLabel: '—',
        amount: 0,
        statusLabel: 'Cannot bill',
        statusClass: 'billing-review-status--attention',
        reason: this.exceptionLabel(exception.code, exception.message),
      });
    }

    return rows;
  }

  residentLink(clientId: number | null): string[] | null {
    if (!clientId) {
      return null;
    }
    const client = this.clients().find((item) => item.id === clientId);
    if (!client) {
      return null;
    }
    return ['/clients', entityRouteKey(client)];
  }

  showFixFundingAction(code: string): boolean {
    return code === 'MISSING_CONTRACT' || code === 'MISSING_RATE';
  }

  fixFundingLink(clientId: number | null, code: string): string[] | null {
    if (!clientId || !this.showFixFundingAction(code)) {
      return null;
    }
    const client = this.clients().find((item) => item.id === clientId);
    if (!client) {
      return null;
    }
    const key = entityRouteKey(client);
    if (code === 'MISSING_RATE') {
      return ['/clients', key, 'funding', 'rates', 'new'];
    }
    return ['/clients', key, 'funding', 'new'];
  }

  /** Query params to hand off billing scope to misc charges and back. */
  billingScopeQueryParams(): Record<string, string> {
    const params: Record<string, string> = {};
    if (this.companyId) {
      const company = this.companies().find((c) => c.id === this.companyId);
      if (company) {
        params['company'] = entityRouteKey(company);
      }
    }
    if (this.careHomeId) {
      const home = this.careHomes().find((h) => h.id === this.careHomeId);
      if (home) {
        params['careHome'] = entityRouteKey(home);
      }
    }
    if (this.periodStart) {
      params['periodStart'] = this.periodStart;
    }
    if (this.periodEnd) {
      params['periodEnd'] = this.periodEnd;
    }
    return params;
  }

  exceptionSetupLink(item: BillingExceptionView): string[] | null {
    const code = item.code;
    if (code === 'MISSING_TEMPLATE') {
      return ['/invoice-templates'];
    }
    if (code === 'MISSING_NOMINAL') {
      return ['/nominal-codes'];
    }
    if (code === 'MISSING_CATEGORY') {
      return ['/invoice-categories'];
    }
    const text = `${item.message ?? ''}`.toLowerCase();
    if (text.includes('miscellaneous') || text.includes('misc charge')) {
      return ['/misc-charges'];
    }
    if (text.includes('invoice template')) {
      return ['/invoice-templates'];
    }
    if (text.includes('nominal')) {
      return ['/nominal-codes'];
    }
    return null;
  }

  exceptionSetupActionLabel(item: BillingExceptionView): string {
    const link = this.exceptionSetupLink(item);
    if (!link) {
      return 'Fix setup';
    }
    if (link[0] === '/misc-charges') {
      return 'Import misc charges';
    }
    if (link[0] === '/invoice-templates') {
      return 'Configure templates';
    }
    if (link[0] === '/nominal-codes') {
      return 'Nominal codes';
    }
    if (link[0] === '/invoice-categories') {
      return 'Invoice categories';
    }
    return 'Fix setup';
  }

  isSingleResidentScope(): boolean {
    return this.selectedClientIds.length === 1;
  }

  contextCareHomeScope(): boolean {
    return this.careHomeId > 0 && !this.contextClientName();
  }

  hasFullyBilledException(previewData: BillingPreviewView): boolean {
    return (previewData.exceptions ?? []).some((item) => item.code === 'ALREADY_FULLY_BILLED');
  }

  generationBlockedMessage(previewData: BillingPreviewView): string {
    if (this.hasFullyBilledException(previewData)) {
      return 'Already fully billed for the selected scope and period. Adjust the period or scope, then preview again.';
    }
    if (this.previewAttentionCount(previewData) === 1) {
      return 'Resolve the outstanding billing exception before creating invoices.';
    }
    return 'Resolve the outstanding billing exceptions before creating invoices.';
  }

  attentionSummary(previewData: BillingPreviewView): string {
    const count = this.residentsRequiringAttention(previewData);
    if (count === 1) {
      return '1 resident requires attention';
    }
    return `${count} residents require attention`;
  }

  residentsRequiringAttention(previewData: BillingPreviewView): number {
    const ids = new Set<number>();
    for (const exception of this.blockingExceptions(previewData)) {
      if (exception.clientId) {
        ids.add(exception.clientId);
      }
    }
    if (ids.size > 0) {
      return ids.size;
    }
    return this.previewAttentionCount(previewData);
  }

  careHomesForSelect(): CareHomeLocation[] {
    const homes = this.careHomes();
    if (!this.companyId) {
      return homes;
    }
    return homes.filter((home) => home.companyId === this.companyId);
  }

  periodSummary(): string | null {
    if (!this.periodStart || !this.periodEnd) {
      return null;
    }
    return `${this.displayDatePipe.transform(this.periodStart)} → ${this.displayDatePipe.transform(this.periodEnd)}`;
  }

  billingPeriodHeading(): string {
    if (!this.periodStart) {
      return 'Billing preview';
    }
    const start = new Date(this.periodStart);
    if (Number.isNaN(start.getTime())) {
      return 'Billing preview';
    }
    return `${start.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' })} billing`;
  }

  selectedCompanyName(): string {
    return this.companies().find((c) => c.id === this.companyId)?.name ?? '';
  }

  selectedCareHomeName(): string {
    if (!this.careHomeId) {
      return '';
    }
    return this.careHomes().find((h) => h.id === this.careHomeId)?.name ?? '';
  }

  selectedCareHomeScopeLabel(): string {
    const home = this.selectedCareHomeName();
    if (home) {
      return home;
    }
    return 'All care homes';
  }

  selectedInvoiceCategoryName(): string {
    if (!this.invoiceCategoryId) {
      return 'All categories';
    }
    return this.categories().find((c) => c.id === this.invoiceCategoryId)?.name ?? '—';
  }

  selectedScopeLabel(): string {
    const home = this.selectedCareHomeName();
    if (home) {
      return home;
    }
    const company = this.selectedCompanyName();
    if (company) {
      return `${company} · all care homes`;
    }
    return 'Select company and care home';
  }

  eligibleResidentsLabel(previewData: BillingPreviewView): string {
    const count = this.previewEligibleResidents(previewData);
    if (count === 1) {
      return '1 eligible resident';
    }
    return `${count} eligible residents`;
  }

  private primaryErrorForClient(
    previewData: BillingPreviewView,
    clientId: number,
  ): BillingExceptionView | undefined {
    return this.blockingExceptions(previewData).find((item) => item.clientId === clientId);
  }

  private infoExceptionForClient(
    previewData: BillingPreviewView,
    clientId: number,
  ): BillingExceptionView | undefined {
    return (previewData.exceptions ?? []).find(
      (item) => item.severity === 'Info' && item.clientId === clientId,
    );
  }

  private formatRequestedPeriod(previewData: BillingPreviewView): string {
    return this.formatPeriodRange(
      previewData.requestedPeriodStart,
      previewData.requestedPeriodEnd,
    );
  }

  private formatLinePeriod(line: BillingPreviewLineView): string {
    return this.formatPeriodRange(line.serviceFrom, line.serviceTo);
  }

  private formatPeriodRange(start: string, end: string): string {
    const startMonth = this.formatMonthYear(start);
    const endMonth = this.formatMonthYear(end);
    if (startMonth && endMonth && startMonth === endMonth) {
      return startMonth;
    }
    if (startMonth && endMonth) {
      return `${this.displayDatePipe.transform(start)} → ${this.displayDatePipe.transform(end)}`;
    }
    return startMonth || endMonth || '—';
  }

  private formatMonthYear(value: string): string {
    if (!value) {
      return '';
    }
    const iso = value.length >= 10 ? value.slice(0, 10) : value;
    const parts = iso.split('-').map((p) => Number(p));
    if (parts.length !== 3 || parts.some((n) => !Number.isFinite(n))) {
      return '';
    }
    const [year, month, day] = parts;
    const date = new Date(year, month - 1, day);
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    return date.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' });
  }

  private applyQueryContext(): void {
    const params = this.route.snapshot.queryParamMap;
    const careHomeKey = params.get('careHome');
    const companyKey = params.get('company');
    const clientKey = params.get('client');
    const careHomeId = Number(params.get('careHomeId') || 0);
    const companyId = Number(params.get('companyId') || 0);
    const clientId = Number(params.get('clientId') || 0);
    const periodStart = this.toDateInput(params.get('periodStart'));
    const periodEnd = this.toDateInput(params.get('periodEnd'));

    if (careHomeKey) {
      const cached = this.careHomes().find((item) => entityRouteKey(item) === careHomeKey);
      if (cached) {
        this.careHomeId = cached.id;
        this.companyId = cached.companyId;
      } else {
        this.homesApi.getCareHome(careHomeKey).subscribe({
          next: (home) => {
            this.careHomeId = home.id;
            this.companyId = home.companyId;
          },
        });
      }
    } else if (careHomeId) {
      this.careHomeId = careHomeId;
      const home = this.careHomes().find((item) => item.id === careHomeId);
      if (home) {
        this.companyId = home.companyId;
      }
    } else if (companyKey) {
      const cached = this.companies().find((item) => entityRouteKey(item) === companyKey);
      if (cached) {
        this.companyId = cached.id;
      } else {
        this.companiesApi.getCompany(companyKey).subscribe({
          next: (company) => {
            this.companyId = company.id;
          },
        });
      }
    } else if (companyId) {
      this.companyId = companyId;
    }

    if (periodStart) {
      this.periodStart = periodStart;
    }
    if (periodEnd) {
      this.periodEnd = periodEnd;
    }

    if (clientKey) {
      const cached = this.clients().find((item) => entityRouteKey(item) === clientKey);
      if (cached) {
        this.applyClientContext(cached);
      } else {
        this.clientsApi.getClient(clientKey).subscribe({
          next: (client) => this.applyClientContext(client),
          error: () => {
            const name = params.get('clientName');
            this.contextClientName.set(name?.trim() || null);
          },
        });
      }
    } else if (clientId) {
      const client = this.clients().find((item) => item.id === clientId);
      if (client) {
        this.applyClientContext(client);
      } else {
        this.selectedClientIds = [clientId];
        this.contextClientName.set(params.get('clientName')?.trim() || null);
      }
    } else {
      this.selectedClientIds = [];
      this.contextClientName.set(null);
    }
  }

  private applyClientContext(client: Client): void {
    this.selectedClientIds = [client.id];
    this.careHomeId = client.careHomeId;
    this.companyId = client.companyId;
    this.contextClientName.set(`${client.firstName} ${client.lastName}`.trim());
  }

  private toDateInput(value: string | null): string {
    if (!value) {
      return '';
    }
    return value.length >= 10 ? value.slice(0, 10) : value;
  }

  private body() {
    return {
      companyId: this.companyId,
      careHomeId: this.careHomeId || null,
      invoiceCategoryId: this.invoiceCategoryId || null,
      periodStart: this.periodStart,
      periodEnd: this.periodEnd,
      clientIds: this.selectedClientIds.length ? this.selectedClientIds : null,
    };
  }
}
