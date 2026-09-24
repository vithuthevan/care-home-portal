import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { RouterLink } from '@angular/router';

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
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';
import { ToastService } from '../../../../shared/ui/toast.service';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { MatSelectModule } from '@angular/material/select';

interface SageExportBatch {
  id: number;
  exportedAt?: string;
  fileName?: string;
  status?: string;
  recordCount?: number;
}

@Component({
  selector: 'app-sage-export',
  imports: [
    FormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    PageHeaderComponent,
    ApiErrorComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
    AppDateFieldComponent,
  ],
  templateUrl: './sage-export.html',
})
export class SageExportPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  private readonly careHomeService = inject(CareHomeService);
  readonly auth = inject(AuthService);
  dateFrom = '';
  dateTo = '';
  selectedCareHomeId = 0;
  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly preview = signal<any | null>(null);
  readonly batches = signal<SageExportBatch[]>([]);
  readonly totalCount = signal(0);
  readonly errorMessage = signal<string | null>(null);
  readonly infoMessage = signal<{ text: string; tone: 'success' | 'warning' } | null>(null);
  readonly isPreviewing = signal(false);
  readonly isExporting = signal(false);
  readonly retryingId = signal<number | null>(null);
  page = 1;
  pageSize = 20;

  ngOnInit(): void {
    this.loadBatches();
    this.careHomeService.getCareHomes().subscribe({
      next: (items) => this.careHomes.set(items),
    });
  }

  private exportBody(): Record<string, string | number> {
    const body: Record<string, string | number> = {
      dateFrom: this.dateFrom,
      dateTo: this.dateTo,
    };
    if (this.selectedCareHomeId > 0) {
      body['careHomeId'] = this.selectedCareHomeId;
    }
    return body;
  }

  invoicesInScopeQueryParams(): Record<string, string | number> {
    const params: Record<string, string | number> = {};
    if (this.dateFrom) {
      params['from'] = this.dateFrom;
    }
    if (this.dateTo) {
      params['to'] = this.dateTo;
    }
    if (this.selectedCareHomeId > 0) {
      params['careHomeId'] = this.selectedCareHomeId;
    }
    return params;
  }

  isFileMissing(batch: SageExportBatch): boolean {
    return batch.status === 'FileMissing';
  }

  isRetrying(batch: SageExportBatch): boolean {
    return this.retryingId() === batch.id;
  }

  invoiceExportLabel(batch: SageExportBatch): string {
    return batch.status === 'FileMissing' ? 'Export recorded (file missing)' : 'Export recorded';
  }

  csvAvailabilityLabel(batch: SageExportBatch): string {
    return this.isFileMissing(batch) ? 'Unavailable' : 'Available';
  }

  loadBatches(): void {
    const params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    this.http.get<PagedResult<SageExportBatch>>('/api/sage-exports', { params }).subscribe({
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
    this.infoMessage.set(null);
    this.isPreviewing.set(true);
    this.http
      .post('/api/sage-exports/preview', this.exportBody())
      .pipe(finalize(() => this.isPreviewing.set(false)))
      .subscribe({
        next: (preview) => this.preview.set(preview),
        error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Validation failed.')),
      });
  }

  exportNow(): void {
    if (this.isExporting() || !this.preview()?.canExport) {
      return;
    }

    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.isExporting.set(true);
    this.http
      .post<SageExportBatch>('/api/sage-exports', this.exportBody())
      .pipe(finalize(() => this.isExporting.set(false)))
      .subscribe({
        next: (batch) => {
          this.loadBatches();
          if (this.isFileMissing(batch)) {
            this.infoMessage.set({
              tone: 'warning',
              text: 'Export recorded, but the CSV file is unavailable. Invoices in this export remain marked as exported. Use Retry CSV to regenerate the file.',
            });
          } else {
            this.infoMessage.set({
              tone: 'success',
              text: 'Sage 50 CSV exported successfully. The file is available to download from previous exports.',
            });
            this.toast.success('Sage CSV exported. The file is ready to download.');
          }
        },
        error: (error) => {
          this.loadBatches();
          this.errorMessage.set(getApiErrorMessage(error, 'Export failed.'));
        },
      });
  }

  download(batch: SageExportBatch): void {
    this.errorMessage.set(null);
    this.http.get(`/api/sage-exports/${batch.id}/file`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = batch.fileName || 'sage-export.csv';
        a.click();
      },
      error: (error) =>
        this.errorMessage.set(
          getApiErrorMessage(
            error,
            'The CSV file is unavailable. If the export was recorded, use Retry CSV.',
          ),
        ),
    });
  }

  retryFile(batch: SageExportBatch): void {
    if (!this.auth.canWrite() || this.retryingId() !== null) {
      return;
    }
    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.retryingId.set(batch.id);
    this.http
      .post<SageExportBatch>(`/api/sage-exports/${batch.id}/retry-file`, {})
      .pipe(finalize(() => this.retryingId.set(null)))
      .subscribe({
        next: (updated) => {
          this.loadBatches();
          if (this.isFileMissing(updated)) {
            this.infoMessage.set({
              tone: 'warning',
              text: 'Export recorded, but the CSV file is still unavailable. Try Retry CSV again.',
            });
          } else {
            this.infoMessage.set({
              tone: 'success',
              text: 'CSV file regenerated. The file is available to download.',
            });
            this.toast.success('CSV file is available to download.');
          }
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Could not regenerate the CSV file.')),
      });
  }
}
