import { Component, inject, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { DataTransferPreview, DataTransferService } from '../data-transfer/data-transfer.service';
import { ToastService } from './toast.service';
import { getApiErrorMessage } from '../../core/api-error';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-import-export-toolbar',
  imports: [MatButtonModule, MatMenuModule, MatIconModule],
  template: `
    <div class="flex flex-wrap items-center gap-2">
      <button mat-stroked-button type="button" [matMenuTriggerFor]="exportMenu">
        <mat-icon class="!text-base !w-4 !h-4 mr-1">download</mat-icon>
        Export
      </button>
      <mat-menu #exportMenu="matMenu">
        <button mat-menu-item type="button" (click)="export('csv')">CSV</button>
        <button mat-menu-item type="button" (click)="export('xlsx')">Excel (.xlsx)</button>
      </mat-menu>

      @if (showImport()) {
        <button mat-stroked-button type="button" [matMenuTriggerFor]="templateMenu">
          Template
        </button>
        <mat-menu #templateMenu="matMenu">
          <button mat-menu-item type="button" (click)="template('csv')">CSV template</button>
          <button mat-menu-item type="button" (click)="template('xlsx')">Excel template</button>
        </mat-menu>

        <label class="inline-flex">
          <input
            #fileInput
            type="file"
            class="sr-only"
            accept=".csv,.xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,text/csv"
            (change)="onFileSelected($event)"
          />
          <button mat-stroked-button type="button" (click)="fileInput.click()" [disabled]="importing()">
            Import
          </button>
        </label>
      }
    </div>

    @if (preview(); as p) {
      <section class="panel p-3 mt-3 w-full">
        <p class="mt-0 mb-2 text-sm">
          Preview: {{ p.validCount }} valid, {{ p.invalidCount }} invalid ({{ p.fileName }})
        </p>
        <div class="max-h-48 overflow-auto text-sm mb-3">
          @for (row of p.rows; track row.rowNumber) {
            <div [class.text-red-600]="!row.isValid">
              Row {{ row.rowNumber }}:
              @if (row.isValid) {
                OK
              } @else {
                {{ row.error }}
              }
            </div>
          }
        </div>
        <div class="flex gap-2">
          <button
            mat-flat-button
            color="primary"
            type="button"
            [disabled]="p.invalidCount > 0 || importing()"
            (click)="confirmImport()"
          >
            Commit import
          </button>
          <button mat-stroked-button type="button" (click)="clearPreview()">Cancel</button>
        </div>
      </section>
    }
  `,
})
export class ImportExportToolbarComponent {
  private readonly dataTransfer = inject(DataTransferService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);

  readonly entity = input.required<string>();
  readonly importEnabled = input(true);

  readonly imported = output<void>();

  readonly preview = signal<DataTransferPreview | null>(null);
  readonly importing = signal(false);

  showImport(): boolean {
    return this.importEnabled() && this.auth.canManageOrganisation();
  }

  export(format: 'csv' | 'xlsx'): void {
    this.dataTransfer.downloadExport(this.entity(), format);
  }

  template(format: 'csv' | 'xlsx'): void {
    this.dataTransfer.downloadTemplate(this.entity(), format);
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    (event.target as HTMLInputElement).value = '';
    if (!file) {
      return;
    }

    this.importing.set(true);
    this.dataTransfer.previewImport(this.entity(), file).subscribe({
      next: (p) => {
        this.preview.set(p);
        this.importing.set(false);
      },
      error: (err) => {
        this.importing.set(false);
        this.toast.error(getApiErrorMessage(err, 'Import preview failed.'));
      },
    });
  }

  confirmImport(): void {
    const p = this.preview();
    if (!p) {
      return;
    }

    this.importing.set(true);
    this.dataTransfer.confirmImport(this.entity(), p).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.preview.set(null);
        this.toast.success(`Import complete: ${result.created} created, ${result.updated} updated.`);
        this.imported.emit();
      },
      error: (err) => {
        this.importing.set(false);
        this.toast.error(getApiErrorMessage(err, 'Import failed.'));
      },
    });
  }

  clearPreview(): void {
    this.preview.set(null);
  }
}
