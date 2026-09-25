import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';

interface BankAccount {
  publicId: string;
  name: string;
  currency: string;
}

interface ColumnMapping {
  [key: string]: string | undefined;
  date?: string;
  valueDate?: string;
  amount?: string;
  debit?: string;
  credit?: string;
  reference?: string;
  description?: string;
  counterparty?: string;
  externalId?: string;
  currency?: string;
}

interface ImportPreviewRow {
  rowNumber: number;
  isValid: boolean;
  error?: string;
  isDuplicate: boolean;
  transactionDate?: string;
  amount?: number;
  reference?: string;
}

interface ImportPreview {
  fileName: string;
  contentChecksum: string;
  headers: string[];
  columnMapping: ColumnMapping;
  rows: ImportPreviewRow[];
  validCount: number;
  invalidCount: number;
  duplicateCount: number;
  fileAlreadyImported: boolean;
}

interface WorkspaceTxn {
  publicId: string;
  transactionDate: string;
  amount: number;
  reference?: string;
  description?: string;
  counterparty?: string;
  status: string;
  topSuggestion?: {
    publicId: string;
    totalScore: number;
    confidenceBand: string;
    factors: { label: string; detail: string; points: number }[];
    lines: { invoicePublicId: string; invoiceNumber: string; suggestedAmount: number }[];
  };
}

interface WorkspaceSummary {
  unreconciled: number;
  suggested: number;
  highConfidence: number;
  needsReview: number;
  transactions: WorkspaceTxn[];
}

interface ManualInvoice {
  invoicePublicId: string;
  invoiceNumber: string;
  residentName?: string;
  funderName: string;
  careHomeName: string;
  outstandingAmount: number;
}

interface ManualAllocation {
  invoicePublicId: string;
  invoiceNumber: string;
  amount: number;
}

interface MappingTemplate {
  publicId: string;
  name: string;
  columnMapping: ColumnMapping;
}

@Component({
  selector: 'app-banking-workspace',
  imports: [
    FormsModule,
    DecimalPipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTabsModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    DisplayDatePipe,
  ],
  templateUrl: './banking-workspace.html',
})
export class BankingWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);

  tabIndex = 0;
  errorMessage = signal<string | null>(null);
  accounts = signal<BankAccount[]>([]);
  selectedAccountId = '';

  preview = signal<ImportPreview | null>(null);
  columnMapping: ColumnMapping = {};
  mappingTemplates = signal<MappingTemplate[]>([]);
  csvContent = '';
  importLoading = signal(false);

  workspaceLoading = signal(false);
  workspace = signal<WorkspaceSummary | null>(null);
  expandedTxn = signal<string | null>(null);

  editorTxn = signal<WorkspaceTxn | null>(null);
  invoiceSearch = '';
  searchFundingAuthorityId: number | null = null;
  searchCareHomeId: number | null = null;
  invoiceResults = signal<ManualInvoice[]>([]);
  manualAllocations = signal<ManualAllocation[]>([]);

  ngOnInit(): void {
    this.loadAccounts();
    this.loadWorkspace();
    this.loadMappingTemplates();
  }

  loadAccounts(): void {
    this.http.get<BankAccount[]>('/api/banking/accounts').subscribe({
      next: (rows) => {
        this.accounts.set(rows);
        if (!this.selectedAccountId && rows.length > 0) {
          this.selectedAccountId = rows[0].publicId;
        }
      },
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load bank accounts.')),
    });
  }

  loadMappingTemplates(): void {
    const acct = this.selectedAccountId;
    const url = acct
      ? `/api/banking/import-mapping-templates?bankAccountPublicId=${acct}`
      : '/api/banking/import-mapping-templates';
    this.http.get<MappingTemplate[]>(url).subscribe({
      next: (rows) => this.mappingTemplates.set(rows),
      error: () => this.mappingTemplates.set([]),
    });
  }

  createAccount(): void {
    const name = prompt('Bank account name');
    if (!name?.trim()) {
      return;
    }
    this.http.post<BankAccount>('/api/banking/accounts', { name: name.trim() }).subscribe({
      next: () => {
        this.toast.success('Bank account created.');
        this.loadAccounts();
      },
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to create account.')),
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.selectedAccountId) {
      return;
    }
    this.importLoading.set(true);
    this.errorMessage.set(null);
    const reader = new FileReader();
    reader.onload = () => {
      this.csvContent = String(reader.result ?? '');
      const form = new FormData();
      form.append('bankAccountPublicId', this.selectedAccountId);
      form.append('file', file);
      if (Object.keys(this.columnMapping).length > 0) {
        form.append('columnMappingJson', JSON.stringify(this.columnMapping));
      }
      this.http
        .post<ImportPreview>('/api/banking/imports/preview', form)
        .pipe(finalize(() => this.importLoading.set(false)))
        .subscribe({
          next: (p) => {
            this.preview.set(p);
            this.columnMapping = { ...(p.columnMapping ?? {}) };
          },
          error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Preview failed.')),
        });
    };
    reader.readAsText(file);
  }

  applyTemplate(template: MappingTemplate): void {
    this.columnMapping = { ...template.columnMapping };
    this.toast.success(`Applied mapping template "${template.name}".`);
  }

  saveMappingTemplate(): void {
    const name = prompt('Template name');
    if (!name?.trim()) {
      return;
    }
    this.http
      .post<MappingTemplate>('/api/banking/import-mapping-templates', {
        name: name.trim(),
        bankAccountPublicId: this.selectedAccountId || null,
        columnMapping: this.columnMapping,
      })
      .subscribe({
        next: () => {
          this.toast.success('Mapping template saved.');
          this.loadMappingTemplates();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to save template.')),
      });
  }

  commitImport(): void {
    const p = this.preview();
    if (!p || !this.selectedAccountId || p.fileAlreadyImported) {
      return;
    }
    const accepted = p.rows.filter((r) => r.isValid && !r.isDuplicate).map((r) => r.rowNumber);
    this.importLoading.set(true);
    this.http
      .post('/api/banking/imports/commit', {
        bankAccountPublicId: this.selectedAccountId,
        fileName: p.fileName,
        contentChecksum: p.contentChecksum,
        columnMapping: this.columnMapping,
        acceptedRowNumbers: accepted,
        csvContent: this.csvContent,
      })
      .pipe(finalize(() => this.importLoading.set(false)))
      .subscribe({
        next: () => {
          this.preview.set(null);
          this.toast.success('Statement imported.');
          this.tabIndex = 2;
          this.loadWorkspace();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Import failed.')),
      });
  }

  loadWorkspace(): void {
    this.workspaceLoading.set(true);
    this.http
      .get<WorkspaceSummary>('/api/banking/reconciliation')
      .pipe(finalize(() => this.workspaceLoading.set(false)))
      .subscribe({
        next: (w) => this.workspace.set(w),
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load reconciliation.')),
      });
  }

  toggleExplain(publicId: string): void {
    this.expandedTxn.set(this.expandedTxn() === publicId ? null : publicId);
  }

  openManualEditor(txn: WorkspaceTxn): void {
    this.editorTxn.set(txn);
    this.manualAllocations.set(
      txn.topSuggestion?.lines.map((l) => ({
        invoicePublicId: l.invoicePublicId,
        invoiceNumber: l.invoiceNumber,
        amount: l.suggestedAmount,
      })) ?? [],
    );
    this.searchInvoices();
  }

  closeManualEditor(): void {
    this.editorTxn.set(null);
    this.manualAllocations.set([]);
    this.invoiceResults.set([]);
  }

  searchInvoices(): void {
    const txn = this.editorTxn();
    if (!txn) {
      return;
    }
    const params = new URLSearchParams();
    if (this.invoiceSearch.trim()) {
      params.set('q', this.invoiceSearch.trim());
    }
    params.set('amount', String(txn.amount));
    if (this.searchFundingAuthorityId) {
      params.set('fundingAuthorityId', String(this.searchFundingAuthorityId));
    }
    if (this.searchCareHomeId) {
      params.set('careHomeId', String(this.searchCareHomeId));
    }
    this.http.get<ManualInvoice[]>(`/api/banking/reconciliation/invoices/search?${params}`).subscribe({
      next: (rows) => this.invoiceResults.set(rows),
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Invoice search failed.')),
    });
  }

  addAllocation(inv: ManualInvoice): void {
    const txn = this.editorTxn();
    if (!txn) {
      return;
    }
    const existing = this.manualAllocations();
    if (existing.some((a) => a.invoicePublicId === inv.invoicePublicId)) {
      return;
    }
    const remaining =
      txn.amount - existing.reduce((sum, a) => sum + a.amount, 0);
    const amount = Math.min(inv.outstandingAmount, remaining);
    if (amount <= 0) {
      this.toast.error('Bank amount fully allocated.');
      return;
    }
    this.manualAllocations.set([
      ...existing,
      { invoicePublicId: inv.invoicePublicId, invoiceNumber: inv.invoiceNumber, amount },
    ]);
  }

  removeAllocation(invoicePublicId: string): void {
    this.manualAllocations.set(this.manualAllocations().filter((a) => a.invoicePublicId !== invoicePublicId));
  }

  allocationTotal(): number {
    return this.manualAllocations().reduce((s, a) => s + a.amount, 0);
  }

  confirmMatch(txn: WorkspaceTxn): void {
    const suggestionId = txn.topSuggestion?.publicId;
    if (!suggestionId) {
      return;
    }
    this.http
      .post(`/api/banking/reconciliation/transactions/${txn.publicId}/confirm`, {
        suggestionPublicId: suggestionId,
      })
      .subscribe({
        next: () => {
          this.toast.success('Reconciliation confirmed.');
          this.loadWorkspace();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Confirmation failed.')),
      });
  }

  confirmManual(): void {
    const txn = this.editorTxn();
    const allocations = this.manualAllocations();
    if (!txn || allocations.length === 0) {
      return;
    }
    this.http
      .post(`/api/banking/reconciliation/transactions/${txn.publicId}/confirm`, {
        manualAllocations: allocations.map((a) => ({
          invoicePublicId: a.invoicePublicId,
          amount: a.amount,
        })),
      })
      .subscribe({
        next: () => {
          this.toast.success('Manual reconciliation saved.');
          this.closeManualEditor();
          this.loadWorkspace();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Manual reconciliation failed.')),
      });
  }

  reverseReconciliation(txn: WorkspaceTxn): void {
    this.confirm
      .confirm({
        title: 'Reverse reconciliation',
        message: 'This reverses allocations and reopens the bank transaction.',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.http.post(`/api/banking/reconciliation/transactions/${txn.publicId}/reverse`, { reason: 'User correction' }).subscribe({
          next: () => {
            this.toast.success('Reconciliation reversed.');
            this.loadWorkspace();
          },
          error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Reversal failed.')),
        });
      });
  }

  ignore(txn: WorkspaceTxn): void {
    this.http.post(`/api/banking/reconciliation/transactions/${txn.publicId}/ignore`, {}).subscribe({
      next: () => this.loadWorkspace(),
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to ignore.')),
    });
  }

  createUnapplied(txn: WorkspaceTxn): void {
    this.http
      .post(`/api/banking/reconciliation/transactions/${txn.publicId}/unapplied-payment`, {})
      .subscribe({
        next: () => {
          this.toast.success('Unapplied payment created.');
          this.loadWorkspace();
        },
        error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to create payment.')),
      });
  }
}
