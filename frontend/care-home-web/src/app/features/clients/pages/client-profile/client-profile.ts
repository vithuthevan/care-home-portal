import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { Client } from '../../models/client.model';
import { ClientService } from '../../services/client.service';

@Component({
  selector: 'app-client-profile',
  imports: [
    RouterLink,
    FormsModule,
    DecimalPipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './client-profile.html',
})
export class ClientProfilePage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clients = inject(ClientService);
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);

  readonly client = signal<Client | null>(null);
  readonly contracts = signal<any[]>([]);
  readonly invoices = signal<any[]>([]);
  readonly authorities = signal<any[]>([]);
  readonly categories = signal<any[]>([]);
  readonly nominals = signal<any[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  tab: 'details' | 'contracts' | 'rates' | 'invoices' = 'details';
  newContract = {
    fundingAuthorityId: 0,
    invoiceCategoryId: 0,
    nominalCodeId: 0,
    contractStartDate: '',
    contractEndDate: '',
  };
  newRate = {
    contractId: 0,
    effectiveFrom: '',
    effectiveTo: '',
    frequency: 'Weekly',
    amount: 0,
    notes: '',
  };

  ngOnInit(): void {
    this.http.get<any[]>('/api/funding-authorities?activeOnly=true').subscribe({
      next: (x) => this.authorities.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding authorities.')),
    });
    this.http.get<any[]>('/api/invoice-categories?activeOnly=true').subscribe({
      next: (x) => this.categories.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice categories.')),
    });
    this.http.get<any[]>('/api/nominal-codes?activeOnly=true').subscribe({
      next: (x) => this.nominals.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load nominal codes.')),
    });

    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      this.isLoading.set(true);
      this.errorMessage.set(null);
      this.client.set(null);
      this.clients
        .getClient(id)
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: (client) => {
            this.client.set(client);
            this.loadContracts();
            this.loadInvoices();
          },
          error: (error) =>
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to load client.')),
        });
    });
  }

  loadContracts(): void {
    const current = this.client();
    if (!current) return;
    this.http.get<any[]>(`/api/clients/${current.id}/funding-contracts`).subscribe({
      next: (x) => this.contracts.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding contracts.')),
    });
  }

  loadInvoices(): void {
    const current = this.client();
    if (!current) return;
    this.http.get<any>('/api/invoices', { params: { clientId: current.id } }).subscribe({
      next: (x) => this.invoices.set(x.items ?? []),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoices.')),
    });
  }

  saveContract(): void {
    const current = this.client();
    if (!current) return;
    this.http.post(`/api/clients/${current.id}/funding-contracts`, this.newContract).subscribe({
      next: () => this.loadContracts(),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to save contract.')),
    });
  }

  addRate(): void {
    if (this.newRate.amount <= 0) {
      this.errorMessage.set('Rate amount must be greater than zero.');
      return;
    }

    this.http
      .post(`/api/funding-contracts/${this.newRate.contractId}/rates`, {
        effectiveFrom: this.newRate.effectiveFrom,
        effectiveTo: this.newRate.effectiveTo || null,
        frequency: this.newRate.frequency,
        amount: this.newRate.amount,
        notes: this.newRate.notes,
        closePreviousOpenEnded: true,
      })
      .subscribe({
        next: () => this.loadContracts(),
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Unable to add rate.')),
      });
  }
}
