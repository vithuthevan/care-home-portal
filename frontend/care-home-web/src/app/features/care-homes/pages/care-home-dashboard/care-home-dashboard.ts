import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
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

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      this.isLoading.set(true);
      this.errorMessage.set(null);
      this.data.set(null);
      this.home.set(null);
      this.residents.set([]);
      this.homes.getCareHome(id).subscribe({
        next: (home) => {
          this.home.set(home);
          this.breadcrumbs.set([
            { label: 'Care Homes', routerLink: '/care-homes' },
            { label: home.name },
          ]);
        },
      });
      this.clientsApi.getClients(undefined, id, false, 1, 200, { status: 'Current' }).subscribe({
        next: (page) => this.residents.set(page.items),
      });
      this.http
        .get(`/api/dashboard/care-homes/${id}`)
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: (data) => this.data.set(data),
          error: (error) =>
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care home dashboard.')),
        });
    });
  }

  profileLine(): string {
    const home = this.home();
    if (!home) {
      return 'Care home occupancy and recent invoices.';
    }
    return `${home.code} · ${home.companyName}`;
  }
}
