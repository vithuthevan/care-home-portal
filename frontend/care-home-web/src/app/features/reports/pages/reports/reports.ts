import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
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
  report = 'client-census';
  from = '';
  to = '';
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
      amount: 'Total amount',
      totalAmount: 'Total amount',
      paymentStatus: 'Payment status',
      status: 'Status',
    },
    'invoices-by-care-home': {
      invoiceNumber: 'Invoice',
      invoiceDate: 'Invoice Date',
      clientName: 'Resident',
      careHomeName: 'Care Home',
      category: 'Category',
      amount: 'Amount',
      paymentStatus: 'Payment status',
      status: 'Status',
    },
    'income-by-category': {
      category: 'Category',
      amount: 'Amount',
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
  }

  load(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    let params = new HttpParams();
    if (this.from) params = params.set('from', this.from);
    if (this.to) params = params.set('to', this.to);
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
    let params = new HttpParams().set('format', format);
    if (this.from) params = params.set('from', this.from);
    if (this.to) params = params.set('to', this.to);
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
    return rows[0] ? Object.keys(rows[0]) : [];
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
