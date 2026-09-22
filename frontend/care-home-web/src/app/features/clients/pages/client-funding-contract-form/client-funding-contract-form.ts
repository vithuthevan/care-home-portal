import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ToastService } from '../../../../shared/ui/toast.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';
import { Client } from '../../models/client.model';
import { ClientService } from '../../services/client.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-client-funding-contract-form',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './client-funding-contract-form.html',
})
export class ClientFundingContractForm implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly clients = inject(ClientService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly client = signal<Client | null>(null);
  readonly authorities = signal<{ id: number; name: string }[]>([]);
  readonly categories = signal<{ id: number; name: string }[]>([]);
  readonly nominals = signal<{ id: number; code: string; name: string }[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  clientRouteKey = '';

  model = {
    fundingAuthorityId: 0,
    invoiceCategoryId: 0,
    nominalCodeId: 0,
    contractStartDate: '',
    contractEndDate: '',
  };

  ngOnInit(): void {
    this.clientRouteKey = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadLookups();
    this.loadClient();
  }

  private loadLookups(): void {
    this.http.get<{ id: number; name: string }[]>('/api/funding-authorities?activeOnly=true').subscribe({
      next: (x) => this.authorities.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding authorities.')),
    });
    this.http.get<{ id: number; name: string }[]>('/api/invoice-categories?activeOnly=true').subscribe({
      next: (x) => this.categories.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice categories.')),
    });
    this.http.get<{ id: number; code: string; name: string }[]>('/api/nominal-codes?activeOnly=true').subscribe({
      next: (x) => this.nominals.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load nominal codes.')),
    });
  }

  private loadClient(): void {
    this.isLoading.set(true);
    this.clients
      .getClient(this.clientRouteKey)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (client) => {
          this.client.set(client);
          this.breadcrumbs.set([
            { label: 'Residents', routerLink: '/clients' },
            {
              label: `${client.firstName} ${client.lastName}`.trim(),
              routerLink: ['/clients', entityRouteKey(client)],
            },
            { label: 'Add funding contract' },
          ]);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load resident.')),
      });
  }

  save(): void {
    const resident = this.client();
    if (!resident || this.isSaving()) {
      return;
    }
    if (
      !this.model.fundingAuthorityId ||
      !this.model.invoiceCategoryId ||
      !this.model.nominalCodeId ||
      !this.model.contractStartDate
    ) {
      this.errorMessage.set('Complete all required contract fields before saving.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.http
      .post(`/api/clients/${resident.id}/funding-contracts`, {
        fundingAuthorityId: this.model.fundingAuthorityId,
        invoiceCategoryId: this.model.invoiceCategoryId,
        nominalCodeId: this.model.nominalCodeId,
        contractStartDate: this.model.contractStartDate,
        contractEndDate: this.model.contractEndDate || null,
      })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Funding contract saved.');
          void this.router.navigate(['/clients', this.clientRouteKey], {
            queryParams: { tab: 'funding' },
          });
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(
              error,
              'Unable to save the funding contract. Check dates and try again.',
            ),
          ),
      });
  }
}
