import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  readonly entityTypeOptions: { value: string; label: string }[] = [
    { value: 'Client', label: 'Resident' },
    { value: 'Invoice', label: 'Invoice' },
    { value: 'ClientFundingContract', label: 'Funding contract' },
    { value: 'FundingRate', label: 'Funding rate' },
    { value: 'CreditNote', label: 'Credit note' },
    { value: 'CareHome', label: 'Care home' },
    { value: 'Company', label: 'Company' },
    { value: 'FundingAuthority', label: 'Funding authority' },
    { value: 'User', label: 'User' },
  ];
  page = 1;
  pageSize = 50;

  ngOnInit(): void {
    this.load();
  }

  actorLabel(item: { userId?: string | null }): string {
    return item.userId?.trim() || 'System';
  }

  humanAction(action: string): string {
    if (!action) {
      return 'Update';
    }
    return action.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    if (this.entityType) params = params.set('entityType', this.entityType);
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
