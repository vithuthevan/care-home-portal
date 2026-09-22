import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { getApiErrorMessage } from '../../core/api-error';
import { AuthService } from '../../core/auth.service';
import { COMMERCIAL_REVENUE_ENABLED } from '../../core/commercial-revenue.feature';
import { PageHeaderComponent } from '../../shared/ui/page-header';
import { ApiErrorComponent } from '../../shared/ui/api-error';
import { LoadingStateComponent } from '../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../shared/ui/status-badge';
import { LabeledStatusComponent } from '../../shared/ui/labeled-status';
import { KpiCardComponent } from '../../shared/ui/kpi-card';
import { EmptyStateComponent } from '../../shared/ui/empty-state';
import { SectionHeaderComponent } from '../../shared/ui/section-header';
import { entityRouteKey } from '../../shared/routing/entity-route';
import {
  billingExceptionHeadline,
  billingExceptionLabel,
} from '../../shared/ui/billing-exception';

interface DashboardBillingExceptionDto {
  code: string;
  message: string;
}

interface DashboardDto {
  totalCareHomes: number;
  currentClients: number;
  availableBeds: number;
  upcomingBillingCount: number;
  outstandingInvoices: number;
  outstandingAmount: number;
  invoicesGenerated: number;
  occupancyByHome: {
    careHomeId: number;
    publicId: string;
    careHomeName: string;
    capacity: number;
    occupied: number;
    available: number;
  }[];
  recentInvoices: {
    id: number;
    publicId: string;
    invoiceNumber: string;
    careHomeName: string;
    totalAmount: number;
    status: string;
    paymentStatus: string;
  }[];
  billingExceptions: DashboardBillingExceptionDto[];
  upcomingInvoices: {
    careHomeName: string;
    fundingAuthorityName: string;
    billingFrequency: string;
  }[];
  setupHints: string[];
}

export interface SetupHintAction {
  label: string;
  link: string | readonly string[];
}

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    LabeledStatusComponent,
    KpiCardComponent,
    EmptyStateComponent,
    SectionHeaderComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  readonly commercialRevenueEnabled = COMMERCIAL_REVENUE_ENABLED;
  readonly outstandingKpiLink = COMMERCIAL_REVENUE_ENABLED ? '/receivables' : '/invoices';
  readonly outstandingKpiQueryParams = COMMERCIAL_REVENUE_ENABLED
    ? null
    : { paymentStatus: 'NotPaid' };
  private readonly router = inject(Router);
  readonly dashboard = signal<DashboardDto | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly today = new Date();

  readonly entityRouteKey = entityRouteKey;

  readonly contextLine = computed(() => {
    const parts: string[] = [];
    const tenant = this.auth.currentUser()?.tenantName?.trim();
    if (tenant) {
      parts.push(tenant);
    }
    parts.push(
      new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric' }).format(this.today),
    );
    return parts.join(' • ');
  });

  readonly attentionCount = computed(() => {
    const data = this.dashboard();
    if (!data) {
      return 0;
    }
    let count = 0;
    if (data.outstandingInvoices > 0) {
      count += 1;
    }
    count += data.setupHints.length;
    count += data.billingExceptions.length;
    return count;
  });

  ngOnInit(): void {
    if (this.auth.isPlatformAdmin() && !this.auth.currentUser()?.tenantPublicId) {
      void this.router.navigate(['/platform/tenants']);
      return;
    }

    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.http
      .get<DashboardDto>('/api/dashboard')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (data) => this.dashboard.set(data),
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load dashboard.')),
      });
  }

  outstandingReceivablesValue(data: DashboardDto): string {
    return `£${data.outstandingAmount.toLocaleString('en-GB', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  }

  outstandingReceivablesHint(data: DashboardDto): string {
    const n = data.outstandingInvoices;
    if (n === 0) {
      return 'No invoices outstanding';
    }
    return `${n} invoice${n === 1 ? '' : 's'} outstanding`;
  }

  invoicesGeneratedHint(): string {
    return 'Issued in your organisation';
  }

  setupHintAction(hint: string): SetupHintAction | null {
    const lower = hint.toLowerCase();
    if (lower.includes('company')) {
      return { label: 'Add company', link: '/companies/new' };
    }
    if (lower.includes('care home')) {
      return { label: 'Add care home', link: '/care-homes/new' };
    }
    if (lower.includes('funding authorit')) {
      return { label: 'Add funding authority', link: '/funding-authorities/new' };
    }
    if (lower.includes('nominal')) {
      return { label: 'Add nominal codes', link: '/nominal-codes' };
    }
    if (lower.includes('invoice template')) {
      return { label: 'Configure templates', link: '/invoice-templates' };
    }
    if (lower.includes('client') || lower.includes('resident')) {
      return { label: 'Add residents', link: '/clients/new' };
    }
    if (lower.includes('funding contract') || lower.includes('rates')) {
      return { label: 'Review billing setup', link: '/billing' };
    }
    return null;
  }

  setupHintTitle(hint: string): string {
    if (hint.toLowerCase().includes('invoice template')) {
      return 'Billing setup incomplete';
    }
    if (hint.toLowerCase().includes('nominal')) {
      return 'Nominal codes missing';
    }
    if (hint.toLowerCase().includes('funding contract')) {
      return 'Funding not configured';
    }
    return 'Setup required';
  }

  exceptionTitle(item: DashboardBillingExceptionDto): string {
    return billingExceptionHeadline(item.code);
  }

  exceptionDetail(item: DashboardBillingExceptionDto): string {
    return billingExceptionLabel(item.code, item.message);
  }

  careHomeDashboardLink(row: DashboardDto['occupancyByHome'][number]): string[] {
    return ['/care-homes', entityRouteKey({ id: row.careHomeId, publicId: row.publicId }), 'dashboard'];
  }

  invoiceDetailLink(row: DashboardDto['recentInvoices'][number]): string[] {
    return ['/invoices', entityRouteKey({ id: row.id, publicId: row.publicId })];
  }

  billingMonthLabel(): string {
    return new Intl.DateTimeFormat('en-GB', { month: 'long' }).format(this.today) + ' billing';
  }

  formatFrequency(value: string): string {
    const trimmed = value?.trim();
    if (!trimmed) {
      return '—';
    }
    return trimmed.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}
