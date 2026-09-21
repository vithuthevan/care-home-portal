import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { PagedResult } from '../../../../core/models';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';

@Component({
  selector: 'app-misc-charges',
  imports: [
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    StatusBadgeComponent,
    TablePaginationComponent,
    FilterBarComponent,
    EmptyStateComponent,
  ],
  templateUrl: './misc-charges.html',
})
export class MiscChargesPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  readonly preview = signal<any | null>(null);
  readonly batches = signal<any[]>([]);
  readonly totalCount = signal(0);
  readonly errorMessage = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  page = 1;
  pageSize = 20;

  ngOnInit(): void {
    this.loadBatches();
  }

  loadBatches(): void {
    const params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    this.http.get<PagedResult<any>>('/api/misc-charges/imports', { params }).subscribe({
      next: (x) => {
        this.batches.set(x.items);
        this.totalCount.set(x.totalCount);
      },
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load import batches.')),
    });
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadBatches();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadBatches();
  }

  onFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.errorMessage.set(null);
    const data = new FormData();
    data.append('file', file);
    this.http.post('/api/misc-charges/import/preview', data).subscribe({
      next: (preview) => this.preview.set(preview),
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Preview failed.')),
    });
  }

  confirm(): void {
    this.errorMessage.set(null);
    this.http.post('/api/misc-charges/import/confirm', this.preview()).subscribe({
      next: () => {
        this.info.set('Import committed.');
        this.loadBatches();
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Import failed.')),
    });
  }
}
