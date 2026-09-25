import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { DisplayDateTimePipe } from '../../../../shared/format/display-date-time.pipe';
import { CurrencyDisplayComponent } from '../../../../shared/ui/currency-display';
import { LabeledStatusComponent } from '../../../../shared/ui/labeled-status';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';
import { CompanyService } from '../../../companies/services/company.service';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { FundingAuthorityService } from '../../../funding-authorities/services/funding-authority.service';
import { ClientService } from '../../../clients/services/client.service';
import { Company } from '../../../companies/models/company.model';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { FundingAuthority } from '../../../funding-authorities/models/funding-authority.model';
import { Client } from '../../../clients/models/client.model';

const SHARED_COLUMN_LABELS: Record<string, string> = {
  clientName: 'Resident',
  referenceNumber: 'Reference',
  careHomeName: 'Care Home',
  companyName: 'Company',
  status: 'Status',
  careType: 'Care Type',
  admissionDate: 'Admission Date',
  clientStatus: 'Resident Status',
  fundingAuthority: 'Funding Authority',
  category: 'Category',
  frequency: 'Frequency',
  amount: 'Amount',
  effectiveFrom: 'Effective From',
  effectiveTo: 'Effective To',
  invoiceNumber: 'Invoice',
  invoiceDate: 'Invoice Date',
  paymentStatus: 'Payment Status',
  totalAmount: 'Amount',
  capacity: 'Capacity',
  currentClients: 'Current Residents',
  availableBeds: 'Available Beds',
  notes: 'Notes',
  loggedAt: 'Logged At',
  severity: 'Severity',
  code: 'Code',
  message: 'Message',
  dueDate: 'Due Date',
  isDue: 'Overdue',
};

@Component({
  selector: 'app-reports',
  imports: [
    FormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    PageHeaderComponent,
    SectionHeaderComponent,
    FilterBarComponent,
    ApiErrorComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    DisplayDatePipe,
    DisplayDateTimePipe,
    CurrencyDisplayComponent,
    LabeledStatusComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './reports.html',
})
export class ReportsPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly companyService = inject(CompanyService);
  private readonly careHomeService = inject(CareHomeService);
  private readonly fundingAuthorityService = inject(FundingAuthorityService);
  private readonly clientService = inject(ClientService);
  report = 'client-census';
  from = '';
  to = '';
  selectedCompanyId = 0;
  selectedCareHomeId = 0;
  selectedClientStatus = '';
  selectedFundingAuthorityId = 0;
  selectedCategoryId = 0;
  selectedClientId = 0;
  selectedContractId = 0;
  readonly companies = signal<Company[]>([]);
  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly fundingAuthorities = signal<FundingAuthority[]>([]);
  readonly categories = signal<{ id: number; name: string }[]>([]);
  readonly clients = signal<Client[]>([]);
  readonly contracts = signal<{ id: number; label: string }[]>([]);
  readonly rows = signal<any[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly hasRun = signal(false);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const report = params.get('report');
    const from = params.get('from');
    const to = params.get('to');
    if (report && this.reportMeta[report]) {
      this.report = report;
    }
    if (from) {
      this.from = from.length >= 10 ? from.slice(0, 10) : from;
    }
    if (to) {
      this.to = to.length >= 10 ? to.slice(0, 10) : to;
    }

    this.companyService.getCompanies().subscribe({
      next: (items) => this.companies.set(items),
    });
    this.careHomeService.getCareHomes().subscribe({
      next: (items) => this.careHomes.set(items),
    });
    this.fundingAuthorityService.getFundingAuthorities().subscribe({
      next: (items) => this.fundingAuthorities.set(items),
    });
    this.http.get<{ id: number; name: string }[]>('/api/invoice-categories?activeOnly=true').subscribe({
      next: (items) => this.categories.set(items),
    });
    this.clientService.getClients(undefined, undefined, false, 1, 500).subscribe({
      next: (page) => this.clients.set(page.items),
    });
  }

  showDateFilter(): boolean {
    return ['invoices-by-client', 'invoices-by-care-home', 'income-by-category'].includes(this.report);
  }

  showCompanyFilter(): boolean {
    return ['client-census', 'current-rates', 'occupancy'].includes(this.report);
  }

  showCareHomeFilter(): boolean {
    return ['client-census', 'current-rates', 'invoices-by-care-home'].includes(this.report);
  }

  showClientStatusFilter(): boolean {
    return this.report === 'current-rates';
  }

  showFundingAuthorityFilter(): boolean {
    return this.report === 'current-rates';
  }

  showCategoryFilter(): boolean {
    return this.report === 'current-rates';
  }

  showClientFilter(): boolean {
    return this.report === 'invoices-by-client';
  }

  showContractFilter(): boolean {
    return this.report === 'rate-history';
  }

  onClientChange(): void {
    this.selectedContractId = 0;
    this.contracts.set([]);
    if (!this.selectedClientId) {
      return;
    }
    this.http
      .get<{ id: number; fundingAuthorityName: string; status: string }[]>(
        `/api/clients/${this.selectedClientId}/funding-contracts`,
      )
      .subscribe({
        next: (items) =>
          this.contracts.set(
            items.map((item) => ({
              id: item.id,
              label: `${item.fundingAuthorityName} (${item.status})`,
            })),
          ),
      });
  }

  private reportParams(): HttpParams {
    let params = new HttpParams();
    if (this.showDateFilter() && this.from) {
      params = params.set('from', this.from);
    }
    if (this.showDateFilter() && this.to) {
      params = params.set('to', this.to);
    }
    if (this.showCompanyFilter() && this.selectedCompanyId > 0) {
      params = params.set('companyId', this.selectedCompanyId);
    }
    if (this.showCareHomeFilter() && this.selectedCareHomeId > 0) {
      params = params.set('careHomeId', this.selectedCareHomeId);
    }
    if (this.showClientStatusFilter() && this.selectedClientStatus) {
      params = params.set('clientStatus', this.selectedClientStatus);
    }
    if (this.showFundingAuthorityFilter() && this.selectedFundingAuthorityId > 0) {
      params = params.set('fundingAuthorityId', this.selectedFundingAuthorityId);
    }
    if (this.showCategoryFilter() && this.selectedCategoryId > 0) {
      params = params.set('categoryId', this.selectedCategoryId);
    }
    if (this.showClientFilter() && this.selectedClientId > 0) {
      params = params.set('clientId', this.selectedClientId);
    }
    if (this.showContractFilter() && this.selectedContractId > 0) {
      params = params.set('contractId', this.selectedContractId);
    }
    return params;
  }

  private readonly reportMeta: Record<string, { title: string; description: string }> = {
    'client-census': {
      title: 'Resident census',
      description: 'See who is in care across homes for a point-in-time view.',
    },
    'current-rates': {
      title: 'Current rates',
      description: 'Review the funding rate currently applied to each resident.',
    },
    'invoices-by-client': {
      title: 'Invoices by resident',
      description: 'Review invoices generated for each resident during a selected period.',
    },
    'invoices-by-care-home': {
      title: 'Invoices by care home',
      description: 'See invoice totals grouped by care home.',
    },
    'income-by-category': {
      title: 'Income by category',
      description: 'Understand billed income by invoice category.',
    },
    occupancy: {
      title: 'Occupancy / availability',
      description: 'Capacity and occupancy across care homes.',
    },
    'rate-history': {
      title: 'Funding rate history',
      description: 'Historical funding rates for audit and reconciliation.',
    },
    'billing-exceptions': {
      title: 'Billing exceptions',
      description: 'Residents or contracts that blocked or complicated billing.',
    },
    outstanding: {
      title: 'Payment status / outstanding',
      description: 'See invoices that are currently marked as unpaid.',
    },
  };

  private readonly reportColumns: Record<string, Record<string, string>> = {
    'client-census': {
      clientName: 'Resident',
      referenceNumber: 'Reference',
      careHomeName: 'Care Home',
      status: 'Status',
      careType: 'Care Type',
      admissionDate: 'Admission Date',
    },
    'current-rates': {
      companyName: 'Company',
      careHomeName: 'Care Home',
      clientName: 'Resident',
      clientStatus: 'Resident Status',
      fundingAuthority: 'Funding Authority',
      category: 'Category',
      frequency: 'Frequency',
      amount: 'Amount',
      effectiveFrom: 'Effective From',
      effectiveTo: 'Effective To',
    },
    'invoices-by-client': {
      invoiceNumber: 'Invoice',
      invoiceDate: 'Invoice Date',
      periodStart: 'Billing period start',
      periodEnd: 'Billing period end',
      clientName: 'Resident',
      careHomeName: 'Care Home',
      category: 'Category',
      amount: 'Net billed',
      totalAmount: 'Net billed',
      paymentStatus: 'Payment status',
      status: 'Status',
    },
    'invoices-by-care-home': {
      invoiceNumber: 'Invoice',
      invoiceDate: 'Invoice Date',
      clientName: 'Resident',
      careHomeName: 'Care Home',
      category: 'Category',
      amount: 'Net billed',
      paymentStatus: 'Payment status',
      status: 'Status',
    },
    'income-by-category': {
      category: 'Category',
      amount: 'Net billed',
    },
    occupancy: {
      careHomeName: 'Care Home',
      companyName: 'Company',
      capacity: 'Capacity',
      currentClients: 'Current Residents',
      availableBeds: 'Available Beds',
    },
    'rate-history': {
      clientName: 'Resident',
      fundingAuthority: 'Funding Authority',
      effectiveFrom: 'Effective From',
      effectiveTo: 'Effective To',
      frequency: 'Frequency',
      amount: 'Amount',
      notes: 'Notes',
    },
    'billing-exceptions': {
      loggedAt: 'Logged At',
      severity: 'Severity',
      code: 'Code',
      message: 'Message',
      clientName: 'Resident',
    },
    outstanding: {
      invoiceNumber: 'Invoice',
      invoiceDate: 'Invoice Date',
      dueDate: 'Due Date',
      careHomeName: 'Care Home',
      amount: 'Amount',
      paymentStatus: 'Payment status',
      isDue: 'Overdue',
    },
  };

  reportTitle(): string {
    return this.reportMeta[this.report]?.title ?? 'Report';
  }

  reportDescription(): string {
    return this.reportMeta[this.report]?.description ?? '';
  }

  onReportChange(): void {
    this.rows.set([]);
    this.hasRun.set(false);
    this.errorMessage.set(null);
    if (!this.showCompanyFilter()) {
      this.selectedCompanyId = 0;
    }
    if (!this.showCareHomeFilter()) {
      this.selectedCareHomeId = 0;
    }
    if (!this.showClientStatusFilter()) {
      this.selectedClientStatus = '';
    }
    if (!this.showFundingAuthorityFilter()) {
      this.selectedFundingAuthorityId = 0;
    }
    if (!this.showCategoryFilter()) {
      this.selectedCategoryId = 0;
    }
    if (!this.showClientFilter()) {
      this.selectedClientId = 0;
    }
    if (!this.showContractFilter()) {
      this.selectedContractId = 0;
    }
  }

  load(): void {
    this.errorMessage.set(null);
    if (this.report === 'income-by-category' && (!this.from || !this.to)) {
      this.errorMessage.set('From and to dates are required.');
      return;
    }
    this.isLoading.set(true);
    const params = this.reportParams();
    this.http
      .get<any[]>(`/api/reports/${this.report}`, { params })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (rows) => {
          this.hasRun.set(true);
          this.rows.set(rows);
        },
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Unable to load report.')),
      });
  }

  exportFormat(format: string): void {
    if (this.isLoading()) {
      return;
    }
    let params = this.reportParams().set('format', format);
    this.http
      .get(`/api/reports/${this.report}`, { params, responseType: 'blob' })
      .subscribe((blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.report}.${format === 'xlsx' ? 'xlsx' : format}`;
        a.click();
      });
  }

  keys(): string[] {
    const rows = this.rows();
    if (!rows[0]) {
      return [];
    }
    return Object.keys(rows[0]).filter((key) => !this.isHiddenColumn(key));
  }

  isHiddenColumn(key: string): boolean {
    return key.endsWith('PublicId');
  }

  cellRoute(row: Record<string, unknown>, key: string): string[] | null {
    const publicId = (value: unknown): string | null => {
      if (value == null || value === '') {
        return null;
      }
      return String(value);
    };

    if (key === 'invoiceNumber') {
      const id = publicId(row['invoicePublicId']);
      return id ? ['/invoices', id] : null;
    }
    if (key === 'clientName') {
      const id = publicId(row['clientPublicId']);
      return id ? ['/clients', id] : null;
    }
    if (key === 'careHomeName') {
      const id = publicId(row['careHomePublicId']);
      return id ? ['/care-homes', id, 'dashboard'] : null;
    }
    if (key === 'companyName') {
      const id = publicId(row['companyPublicId']);
      return id ? ['/companies', id] : null;
    }
    return null;
  }

  formatCellValue(row: Record<string, unknown>, key: string): string {
    const value = row[key];
    if (value == null || value === '') {
      return '—';
    }
    return String(value);
  }

  columnLabel(key: string): string {
    return this.reportColumns[this.report]?.[key] ?? SHARED_COLUMN_LABELS[key] ?? this.titleCase(key);
  }

  isCurrency(key: string): boolean {
    return key === 'amount' || key === 'totalAmount';
  }

  isDate(key: string): boolean {
    return (
      key === 'admissionDate' ||
      key === 'invoiceDate' ||
      key === 'periodStart' ||
      key === 'periodEnd' ||
      key === 'effectiveFrom' ||
      key === 'effectiveTo' ||
      key === 'dueDate'
    );
  }

  isDateTime(key: string): boolean {
    return key === 'loggedAt';
  }

  isBoolean(key: string): boolean {
    return key === 'isDue';
  }

  isPaymentStatus(key: string): boolean {
    return key === 'paymentStatus';
  }

  isNumeric(key: string): boolean {
    return this.isCurrency(key) || key === 'capacity' || key === 'currentClients' || key === 'availableBeds';
  }

  toNumber(value: unknown): number {
    return typeof value === 'number' ? value : Number(value) || 0;
  }

  formatBoolean(value: unknown): string {
    return value === true ? 'Yes' : value === false ? 'No' : '—';
  }

  private titleCase(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (letter) => letter.toUpperCase())
      .trim();
  }
}
