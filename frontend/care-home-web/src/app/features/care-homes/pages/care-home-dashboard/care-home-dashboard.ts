import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

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
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import {
  EntitySummaryItem,
  EntitySummaryStripComponent,
} from '../../../../shared/ui/entity-summary-strip';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-care-home-dashboard',
  imports: [
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    EntitySummaryStripComponent,
  ],
  templateUrl: './care-home-dashboard.html',
})
export class CareHomeDashboardPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly homes = inject(CareHomeService);
  private readonly clientsApi = inject(ClientService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);
  readonly data = signal<any | null>(null);
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
        hint: 'Current at this home',
        tone: 'success',
      },
      {
        label: 'Available',
        value: String(dash.available ?? 0),
        hint: 'Places remaining',
        tone: dash.available > 0 ? 'success' : 'attention',
      },
      {
        label: 'Outstanding',
        value: `£${outstanding.toFixed(2)}`,
        hint: outstanding > 0 ? 'Unpaid invoices' : 'Billing up to date',
        tone: outstanding > 0 ? 'attention' : 'success',
      },
    ];
  });

  billingQueryParams(): Record<string, number> {
    const id = this.data()?.careHomeId ?? this.home()?.id;
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
            .get(`/api/dashboard/care-homes/${numericId}`)
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
}
