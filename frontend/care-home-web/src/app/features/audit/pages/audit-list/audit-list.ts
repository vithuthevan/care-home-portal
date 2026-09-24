import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { DisplayDateTimePipe } from '../../../../shared/format/display-date-time.pipe';
import { PagedResult } from '../../../../core/models';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';

@Component({
  selector: 'app-audit-list',
  imports: [
    RouterLink,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    FilterBarComponent,
    EmptyStateComponent,
    DisplayDateTimePipe,
    TablePaginationComponent,
  ],
  templateUrl: './audit-list.html',
})
export class AuditListPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly items = signal<any[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  entityType = '';
  action = '';
  readonly entityTypeOptions: { value: string; label: string }[] = [
    { value: 'Client', label: 'Resident' },
    { value: 'Invoice', label: 'Invoice' },
    { value: 'ClientFundingContract', label: 'Funding contract' },
    { value: 'FundingRate', label: 'Funding rate' },
    { value: 'CreditNote', label: 'Credit note' },
    { value: 'NominalCode', label: 'Nominal code' },
    { value: 'InvoiceCategory', label: 'Invoice category' },
    { value: 'InvoiceTemplate', label: 'Invoice template' },
    { value: 'SageExport', label: 'Sage export' },
    { value: 'CareHome', label: 'Care home' },
    { value: 'Company', label: 'Company' },
    { value: 'FundingAuthority', label: 'Funding authority' },
    { value: 'Tenant', label: 'Organisation settings' },
    { value: 'User', label: 'User' },
  ];
  readonly actionOptions: { value: string; label: string }[] = [
    { value: 'Create', label: 'Create' },
    { value: 'Update', label: 'Update' },
    { value: 'Deactivate', label: 'Deactivate' },
    { value: 'Export', label: 'Export' },
    { value: 'RetryFile', label: 'Retry file' },
    { value: 'Generate', label: 'Generate' },
    { value: 'Void', label: 'Void' },
    { value: 'Send', label: 'Send' },
    { value: 'PaymentStatus', label: 'Payment status change' },
  ];
  page = 1;
  pageSize = 20;

  ngOnInit(): void {
    this.load();
  }

  actorLabel(item: { userDisplayName?: string | null; userId?: string | null }): string {
    return item.userDisplayName?.trim() || 'System';
  }

  entityLabel(item: { description?: string | null; entityId?: string | null }): string {
    return item.description?.trim() || '';
  }

  entityTypeLabel(entityType: string): string {
    const match = this.entityTypeOptions.find((option) => option.value === entityType);
    return match?.label || entityType.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  humanAction(action: string): string {
    const labels: Record<string, string> = {
      PaymentStatus: 'Payment status changed',
      RetryFile: 'Sage file regenerated',
      Generate: 'Generated',
    };
    if (labels[action]) {
      return labels[action];
    }
    if (!action) {
      return 'Update';
    }
    return action.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  entityReference(item: { entityType?: string; entityId?: string | null }): string | null {
    if (!item.entityId) {
      return null;
    }
    return `${this.entityTypeLabel(item.entityType || '')} #${item.entityId}`;
  }

  entityRoute(item: { entityType?: string; entityId?: string | null }): string[] | null {
    const id = item.entityId?.trim();
    if (!id) {
      return null;
    }
    switch (item.entityType) {
      case 'Invoice':
        return ['/invoices', id.split(',')[0] ?? id];
      case 'Client':
        return ['/clients', id];
      case 'CreditNote':
        return ['/credit-notes'];
      case 'Company':
        return ['/companies', id];
      case 'CareHome':
        return ['/care-homes', id, 'dashboard'];
      case 'FundingAuthority':
        return ['/funding-authorities', id, 'edit'];
      case 'InvoiceCategory':
        return ['/invoice-categories', id, 'edit'];
      case 'InvoiceTemplate':
        return ['/invoice-templates', id, 'edit'];
      case 'NominalCode':
        return ['/nominal-codes', id, 'edit'];
      case 'SageExport':
        return ['/sage-exports'];
      case 'Tenant':
        return ['/settings/organisation'];
      case 'User':
        return ['/users'];
      case 'Payment':
        return ['/payments', id];
      default:
        return null;
    }
  }

  entityRouteQueryParams(item: {
    entityType?: string;
    entityId?: string | null;
  }): Record<string, string> | null {
    if (item.entityType === 'CreditNote' && item.entityId?.trim()) {
      return { creditNoteId: item.entityId.trim() };
    }
    return null;
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    if (this.entityType) params = params.set('entityType', this.entityType);
    if (this.action) params = params.set('action', this.action);
    this.http
      .get<PagedResult<any>>('/api/audit', { params })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (x) => {
          this.items.set(x.items);
          this.totalCount.set(x.totalCount);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load audit log.')),
      });
  }

  onPageChange(page: number): void {
    this.page = page;
    this.load();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.load();
  }
}
