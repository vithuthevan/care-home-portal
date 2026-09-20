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
import { billingExceptionLabel } from '../../../../shared/ui/billing-exception';
import { Company } from '../../../companies/models/company.model';
import { CompanyService } from '../../../companies/services/company.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { InvoiceCategory } from '../../../invoice-categories/models/invoice-category.model';
import { InvoiceCategoryService } from '../../../invoice-categories/services/invoice-category.service';
import { ClientService } from '../../../clients/services/client.service';
import { Client } from '../../../clients/models/client.model';

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
  readonly preview = signal<any | null>(null);
  readonly generateResult = signal<any | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly isWorking = signal(false);
  readonly contextClientName = signal<string | null>(null);

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
    return this.generateResult() ? 3 : 1;
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
    this.isWorking.set(true);
    this.http
      .post('/api/billing/preview', this.body())
      .pipe(finalize(() => this.isWorking.set(false)))
      .subscribe({
        next: (result) => this.preview.set(result),
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Preview failed.')),
      });
  }

  generate(): void {
    if (!this.preview()?.canGenerate) {
      return;
    }
    this.isWorking.set(true);
    this.http
      .post('/api/billing/generate', this.body())
      .pipe(finalize(() => this.isWorking.set(false)))
      .subscribe({
        next: (result) => {
          this.generateResult.set(result);
          this.toast.success('Invoice generated successfully.');
          this.runPreview();
        },
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Generation failed.')),
      });
  }

  exceptionLabel(code: string, message: string): string {
    return billingExceptionLabel(code, message);
  }

  hasFullyBilledException(previewData: { exceptions?: { code: string }[] }): boolean {
    return (previewData.exceptions ?? []).some((item) => item.code === 'ALREADY_FULLY_BILLED');
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
    return `${this.displayDatePipe.transform(this.periodStart)} to ${this.displayDatePipe.transform(this.periodEnd)}`;
  }

  private applyQueryContext(): void {
    const params = this.route.snapshot.queryParamMap;
    const careHomeId = Number(params.get('careHomeId') || 0);
    const companyId = Number(params.get('companyId') || 0);
    const clientId = Number(params.get('clientId') || 0);
    const periodStart = this.toDateInput(params.get('periodStart'));
    const periodEnd = this.toDateInput(params.get('periodEnd'));

    if (careHomeId) {
      this.careHomeId = careHomeId;
      const home = this.careHomes().find((item) => item.id === careHomeId);
      if (home) {
        this.companyId = home.companyId;
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
    if (clientId) {
      this.selectedClientIds = [clientId];
      const client = this.clients().find((item) => item.id === clientId);
      this.contextClientName.set(
        client ? `${client.firstName} ${client.lastName}`.trim() : params.get('clientName'),
      );
    } else {
      this.selectedClientIds = [];
      this.contextClientName.set(null);
    }
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
