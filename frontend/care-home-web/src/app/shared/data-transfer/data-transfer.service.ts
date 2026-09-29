import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface DataTransferPreviewRow {
  rowNumber: number;
  isValid: boolean;
  error?: string | null;
  values: Record<string, string>;
}

export interface DataTransferPreview {
  entity: string;
  fileName: string;
  rows: DataTransferPreviewRow[];
  validCount: number;
  invalidCount: number;
}

export interface DataTransferCommitResult {
  created: number;
  updated: number;
  message?: string | null;
}

@Injectable({ providedIn: 'root' })
export class DataTransferService {
  private readonly http = inject(HttpClient);

  downloadTemplate(entity: string, format: 'csv' | 'xlsx'): void {
    this.download(`/api/data-transfer/${entity}/template?format=${format}`, `${entity}-template.${format}`);
  }

  downloadExport(entity: string, format: 'csv' | 'xlsx'): void {
    this.download(`/api/data-transfer/${entity}/export?format=${format}`, `${entity}-export.${format}`);
  }

  previewImport(entity: string, file: File): Observable<DataTransferPreview> {
    const data = new FormData();
    data.append('file', file);
    return this.http.post<DataTransferPreview>(`/api/data-transfer/${entity}/import/preview`, data);
  }

  confirmImport(entity: string, preview: DataTransferPreview): Observable<DataTransferCommitResult> {
    return this.http.post<DataTransferCommitResult>(`/api/data-transfer/${entity}/import/confirm`, preview);
  }

  private download(url: string, fileName: string): void {
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(link.href);
      },
    });
  }
}
