import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { ToastService } from '../../../../shared/ui/toast.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { Client } from '../../models/client.model';
import { ClientService } from '../../services/client.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

interface FundingContractView {
  id: number;
  fundingAuthorityName: string;
  invoiceCategoryName: string;
  contractStartDate: string;
  contractEndDate: string | null;
}

@Component({
  selector: 'app-client-funding-rate-form',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './client-funding-rate-form.html',
})
export class ClientFundingRateForm implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly clients = inject(ClientService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  private readonly toast = inject(ToastService);
  private readonly displayDate = new DisplayDatePipe();
  readonly auth = inject(AuthService);

  readonly client = signal<Client | null>(null);
  readonly contracts = signal<FundingContractView[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  clientRouteKey = '';

  model = {
    contractId: 0,
    effectiveFrom: '',
    effectiveTo: '',
    frequency: 'Weekly',
    amount: 0,
    notes: '',
  };

  ngOnInit(): void {
    this.clientRouteKey = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadClientAndContracts();
  }

  formatContractRange(contract: FundingContractView): string {
    const end = contract.contractEndDate
      ? this.displayDate.transform(contract.contractEndDate)
      : 'Open ended';
    return `${this.displayDate.transform(contract.contractStartDate)} → ${end}`;
  }

  private loadClientAndContracts(): void {
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
            { label: 'Add rate' },
          ]);
          this.loadContracts(client.id);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load resident.')),
      });
  }

  private loadContracts(clientId: number): void {
    this.http.get<FundingContractView[]>(`/api/clients/${clientId}/funding-contracts`).subscribe({
      next: (list) => {
        this.contracts.set(list);
        const preselect = Number(this.route.snapshot.queryParamMap.get('contractId') || 0);
        if (preselect && list.some((c) => c.id === preselect)) {
          this.model.contractId = preselect;
        } else if (list.length === 1) {
          this.model.contractId = list[0].id;
        }
      },
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding contracts.')),
    });
  }

  save(): void {
    if (this.isSaving()) {
      return;
    }
    if (!this.model.contractId || !this.model.effectiveFrom || !this.model.frequency) {
      this.errorMessage.set('Complete all required rate fields before saving.');
      return;
    }
    if (this.model.amount <= 0) {
      this.errorMessage.set('Rate amount must be greater than zero.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.http
      .post(`/api/funding-contracts/${this.model.contractId}/rates`, {
        effectiveFrom: this.model.effectiveFrom,
        effectiveTo: this.model.effectiveTo || null,
        frequency: this.model.frequency,
        amount: this.model.amount,
        notes: this.model.notes || null,
        closePreviousOpenEnded: true,
      })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Rate added.');
          void this.router.navigate(['/clients', this.clientRouteKey], {
            queryParams: { tab: 'funding' },
          });
        },
        error: (error) =>
          this.errorMessage.set(
            getApiErrorMessage(error, 'Unable to add rate. Check the dates and amount.'),
          ),
      });
  }
}
