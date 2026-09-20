import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { PagedResult } from '../../../../core/models';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';

@Component({
  selector: 'app-sage-export',
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
  ],
  templateUrl: './sage-export.html',
})
export class SageExportPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  dateFrom = '';
  dateTo = '';
  readonly preview = signal<any | null>(null);
  readonly batches = signal<any[]>([]);
  readonly totalCount = signal(0);
  readonly errorMessage = signal<string | null>(null);
  page = 1;
  pageSize = 50;

  ngOnInit(): void {
    this.loadBatches();
  }

  loadBatches(): void {
    const params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    this.http.get<PagedResult<any>>('/api/sage-exports', { params }).subscribe({
      next: (x) => {
        this.batches.set(x.items);
        this.totalCount.set(x.totalCount);
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Unable to load exports.')),
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

  runPreview(): void {
    this.errorMessage.set(null);
    this.http
      .post('/api/sage-exports/preview', { dateFrom: this.dateFrom, dateTo: this.dateTo })
      .subscribe({
        next: (preview) => this.preview.set(preview),
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Preview failed.')),
      });
  }

  exportNow(): void {
    this.errorMessage.set(null);
    this.http
      .post<any>('/api/sage-exports', { dateFrom: this.dateFrom, dateTo: this.dateTo })
      .subscribe({
        next: () => this.loadBatches(),
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Export failed.')),
      });
  }

  download(id: number): void {
    this.http.get(`/api/sage-exports/${id}/file`, { responseType: 'blob' }).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `sage-export-${id}.csv`;
      a.click();
    });
  }
}
