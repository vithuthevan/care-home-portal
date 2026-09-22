import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { CurrencyDisplayComponent } from '../../../../shared/ui/currency-display';
import { ToastService } from '../../../../shared/ui/toast.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { RouterLink } from '@angular/router';
import { Client } from '../../../clients/models/client.model';
import { ClientService } from '../../../clients/services/client.service';
import { PagedResult } from '../../../../core/models';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';

@Component({
  selector: 'app-credit-note-workspace',
  imports: [
    FormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatButtonModule,
    DisplayDatePipe,
    PageHeaderComponent,
    ApiErrorComponent,
    SectionHeaderComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    LoadingStateComponent,
    CurrencyDisplayComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './credit-note-workspace.html',
})
export class CreditNoteWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly clientsApi = inject(ClientService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  private readonly displayDate = new DisplayDatePipe();
  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly residents = signal<Client[]>([]);
  readonly preview = signal<any | null>(null);
  readonly notes = signal<any[]>([]);
  readonly notesTotalCount = signal(0);
  notesPage = 1;
  notesPageSize = 20;
  readonly errorMessage = signal<string | null>(null);
  readonly isWorking = signal(false);
  readonly sourceInvoiceNumber = signal<string | null>(null);
  readonly sourceResidentName = signal<string | null>(null);
  readonly sourceClientReference = signal<string | null>(null);

  selectedClientId: number | null = null;
  periodStart = '';
  periodEnd = '';
  reason = '';
  readonly residentQuery = signal('');

  readonly filteredResidents = computed(() => {
    const q = this.residentQuery().trim().toLowerCase();
    const list = this.residents();
    if (!q) {
      return list;
    }
    return list.filter(
      (client) =>
        this.residentName(client).toLowerCase().includes(q) ||
        client.referenceNumber.toLowerCase().includes(q) ||
        (client.sageId || '').toLowerCase().includes(q),
    );
  });

  ngOnInit(): void {
    this.loadNotes();
    this.clientsApi.getClients(undefined, undefined, false, 1, 200).subscribe({
      next: (page) => {
        this.residents.set(page.items);
        this.applyQueryContext();
      },
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load residents.')),
    });
    this.route.queryParamMap.subscribe(() => this.applyQueryContext());
  }

  residentName(client: Client): string {
    return `${client.firstName} ${client.lastName}`.trim();
  }

  displayResident = (value: Client | string | null): string => {
    if (!value) {
      return '';
    }
    if (typeof value === 'string') {
      return value;
    }
    return this.residentName(value);
  };

  sourcePeriodLabel(): string | null {
    if (!this.periodStart || !this.periodEnd) {
      return null;
    }
    return `${this.displayDate.transform(this.periodStart)} – ${this.displayDate.transform(this.periodEnd)}`;
  }

  residentContextLabel(): string | null {
    const name = this.sourceResidentName();
    const ref = this.sourceClientReference();
    if (!name) {
      return null;
    }
    return ref ? `${name} — ${ref}` : name;
  }

  onResidentSelected(client: Client | null): void {
    if (!client) {
      this.selectedClientId = null;
      this.residentQuery.set('');
      return;
    }
    this.selectedClientId = client.id;
    this.residentQuery.set(this.residentDisplay(client));
    this.sourceResidentName.set(this.residentName(client));
    this.sourceClientReference.set(client.referenceNumber);
  }

  residentDisplay(client: Client): string {
    const name = this.residentName(client);
    return client.referenceNumber ? `${name} — ${client.referenceNumber}` : name;
  }

  onResidentQueryChange(value: string | Client | null): void {
    if (value && typeof value === 'object') {
      this.onResidentSelected(value);
      return;
    }
    const text = value || '';
    this.residentQuery.set(text);
    const match = this.residents().find(
      (client) => this.residentName(client).toLowerCase() === text.trim().toLowerCase(),
    );
    this.selectedClientId = match?.id ?? null;
  }

  runPreview(): void {
    this.errorMessage.set(null);
    this.isWorking.set(true);
    this.http
      .post('/api/credit-notes/preview', this.body())
      .pipe(finalize(() => this.isWorking.set(false)))
      .subscribe({
        next: (preview) => this.preview.set(preview),
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Preview failed.')),
      });
  }

  generate(): void {
    this.errorMessage.set(null);
    this.isWorking.set(true);
    this.http
      .post('/api/credit-notes/generate', this.body())
      .pipe(finalize(() => this.isWorking.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Credit note generated successfully.');
          this.loadNotes();
        },
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Generate failed.')),
      });
  }

  pdf(id: number): void {
    this.http.get(`/api/credit-notes/${id}/pdf`, { responseType: 'blob' }).subscribe((blob) => {
      window.open(URL.createObjectURL(blob), '_blank');
    });
  }

  private body() {
    return {
      clientId: this.selectedClientId || null,
      periodStart: this.periodStart,
      periodEnd: this.periodEnd,
      reason: this.reason,
      creditNoteDate: this.periodEnd,
    };
  }

  private applyQueryContext(): void {
    const params = this.route.snapshot.queryParamMap;
    const clientId = Number(params.get('clientId') || 0);
    const invoiceNumber = params.get('invoiceNumber')?.trim() || '';
    const clientName = params.get('clientName')?.trim() || '';
    const clientReference = params.get('clientReference')?.trim() || '';
    const periodStart = this.toDateInput(params.get('periodStart'));
    const periodEnd = this.toDateInput(params.get('periodEnd'));

    if (invoiceNumber) {
      this.sourceInvoiceNumber.set(invoiceNumber);
      this.breadcrumbs.set([
        { label: 'Billing', routerLink: '/billing' },
        { label: 'Credit notes', routerLink: '/credit-notes' },
        { label: invoiceNumber },
      ]);
    } else {
      this.breadcrumbs.set([
        { label: 'Billing', routerLink: '/billing' },
        { label: 'Credit notes' },
      ]);
    }

    if (clientReference) {
      this.sourceClientReference.set(clientReference);
    }
    if (periodStart) {
      this.periodStart = periodStart;
    }
    if (periodEnd) {
      this.periodEnd = periodEnd;
    }

    if (clientId) {
      this.selectedClientId = clientId;
      const match = this.residents().find((client) => client.id === clientId);
      if (match) {
        this.residentQuery.set(this.residentDisplay(match));
        this.sourceResidentName.set(this.residentName(match));
        if (!clientReference) {
          this.sourceClientReference.set(match.referenceNumber);
        }
      } else if (clientName) {
        const label = clientReference ? `${clientName} — ${clientReference}` : clientName;
        this.residentQuery.set(label);
        this.sourceResidentName.set(clientName);
      }
    } else if (clientName) {
      this.sourceResidentName.set(clientName);
      this.residentQuery.set(clientReference ? `${clientName} — ${clientReference}` : clientName);
    }
  }

  private toDateInput(value: string | null): string {
    if (!value) {
      return '';
    }
    return value.length >= 10 ? value.slice(0, 10) : value;
  }

  private loadNotes(): void {
    const params = new HttpParams()
      .set('page', this.notesPage)
      .set('pageSize', this.notesPageSize);
    this.http.get<PagedResult<any>>('/api/credit-notes', { params }).subscribe({
      next: (x) => {
        this.notes.set(x.items);
        this.notesTotalCount.set(x.totalCount);
      },
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load credit notes.')),
    });
  }

  onNotesPageChange(page: number): void {
    this.notesPage = page;
    this.loadNotes();
  }

  onNotesPageSizeChange(size: number): void {
    this.notesPageSize = size;
    this.notesPage = 1;
    this.loadNotes();
  }
}
