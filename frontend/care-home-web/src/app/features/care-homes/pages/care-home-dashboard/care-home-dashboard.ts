import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { CareHomeLocation } from '../../models/care-home.model';
import { CareHomeService } from '../../services/care-home.service';
import { Client } from '../../../clients/models/client.model';
import { ClientService } from '../../../clients/services/client.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { LabeledStatusComponent } from '../../../../shared/ui/labeled-status';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import {
  EntitySummaryItem,
  EntitySummaryStripComponent,
} from '../../../../shared/ui/entity-summary-strip';
import { entityRouteKey } from '../../../../shared/routing/entity-route';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';

interface CareHomeDashboardInvoiceRow {
  id: number;
  publicId?: string;
  invoiceNumber: string;
  clientName?: string;
  periodStart?: string;
  periodEnd?: string;
  totalAmount: number;
  netBilledAmount?: number;
  paymentStatus: string;
}

interface CareHomeDashboardData {
  careHomeId: number;
  name: string;
  capacity: number;
  occupied: number;
  available: number;
  outstandingAmount: number;
  managerName?: string | null;
  recentInvoices: CareHomeDashboardInvoiceRow[];
}

@Component({
  selector: 'app-care-home-dashboard',
  imports: [
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    LabeledStatusComponent,
    EntitySummaryStripComponent,
    DisplayDatePipe,
  ],
  templateUrl: './care-home-dashboard.html',
})
export class CareHomeDashboardPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly homes = inject(CareHomeService);
  private readonly clientsApi = inject(ClientService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  private readonly displayDate = new DisplayDatePipe();
  readonly auth = inject(AuthService);
  readonly entityRouteKey = entityRouteKey;

  readonly data = signal<CareHomeDashboardData | null>(null);
  readonly home = signal<CareHomeLocation | null>(null);
  readonly residents = signal<Client[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly summaryStrip = computed((): EntitySummaryItem[] => {
    const dash = this.data();
    if (!dash) {
      return [];
    }
    const outstanding = Number(dash.outstandingAmount) || 0;
    return [
      {
        label: 'Capacity',
        value: String(dash.capacity ?? '—'),
        hint: 'Registered beds',
        tone: 'default',
      },
      {
        label: 'Occupied',
        value: String(dash.occupied ?? 0),
        hint: 'Current residents',
        tone: 'default',
      },
      {
        label: 'Available',
        value: String(dash.available ?? 0),
        hint: 'Remaining capacity',
        tone: dash.available > 0 ? 'default' : 'attention',
      },
      {
        label: 'Outstanding',
        value: `£${outstanding.toFixed(2)}`,
        hint: 'Accounts receivable',
        tone: outstanding > 0 ? 'attention' : 'success',
      },
    ];
  });

  billingQueryParams(): Record<string, string> {
    const home = this.home();
    if (!home) {
      return {};
    }
    return { careHome: entityRouteKey(home) };
  }

  residentsQueryParams(): Record<string, string> {
    const home = this.home();
    return home ? { careHome: entityRouteKey(home) } : {};
  }

  invoicesQueryParams(): Record<string, string> {
    const home = this.home();
    return home ? { careHome: entityRouteKey(home) } : {};
  }

  addResidentQueryParams(): Record<string, number> {
    const id = this.home()?.id;
    return id ? { careHomeId: id } : {};
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const key = params.get('id') ?? '';
      this.isLoading.set(true);
      this.errorMessage.set(null);
      this.data.set(null);
      this.home.set(null);
      this.residents.set([]);
      this.homes.getCareHome(key).subscribe({
        next: (home) => {
          this.home.set(home);
          this.breadcrumbs.set([
            { label: 'Care Homes', routerLink: '/care-homes' },
            { label: home.name },
          ]);
          const numericId = home.id;
          this.clientsApi
            .getClients(undefined, numericId, false, 1, 200, { status: 'Current' })
            .subscribe({
              next: (page) => this.residents.set(page.items),
            });
          this.http
            .get<CareHomeDashboardData>(`/api/dashboard/care-homes/${numericId}`)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
              next: (data) => this.data.set(data),
              error: (error) =>
                this.errorMessage.set(
                  getApiErrorMessage(error, 'Unable to load care home dashboard.'),
                ),
            });
        },
        error: (error) => {
          this.isLoading.set(false);
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care home.'));
        },
      });
    });
  }

  portalSettingsLink(): string[] {
    const home = this.home();
    if (!home) {
      return ['/care-homes'];
    }
    return ['/care-homes', entityRouteKey(home), 'settings'];
  }

  editHomeLink(): string[] {
    const home = this.home();
    if (!home) {
      return ['/care-homes'];
    }
    return ['/care-homes', entityRouteKey(home), 'edit'];
  }

  profileLine(): string {
    const home = this.home();
    if (!home) {
      return 'Care home occupancy and recent invoices.';
    }
    return `${home.code} · ${home.companyName}`;
  }

  formatInvoicePeriod(row: CareHomeDashboardInvoiceRow): string {
    if (!row.periodStart || !row.periodEnd) {
      return '—';
    }
    return `${this.displayDate.transform(row.periodStart)} – ${this.displayDate.transform(row.periodEnd)}`;
  }

  invoiceLink(row: CareHomeDashboardInvoiceRow): string[] {
    return ['/invoices', entityRouteKey({ id: row.id, publicId: row.publicId })];
  }

  navigateToClient(client: Client): void {
    void this.router.navigate(['/clients', entityRouteKey(client)]);
  }

  navigateToInvoice(row: CareHomeDashboardInvoiceRow): void {
    void this.router.navigate(this.invoiceLink(row));
  }
}
