import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { InvoiceTemplate } from '../../models/invoice-template.model';
import {
  deactivateInvoiceTemplateMessage,
  invoiceTemplateUsageLabel,
} from '../../../../shared/format/master-data-usage';

@Component({
  selector: 'app-invoice-template-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
  ],
  templateUrl: './invoice-template-list.html',
})
export class InvoiceTemplateListPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly confirm = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);
  readonly items = signal<InvoiceTemplate[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.http
      .get<InvoiceTemplate[]>('/api/invoice-templates')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (x) => this.items.set(x),
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice templates.')),
      });
  }

  usageLabel = invoiceTemplateUsageLabel;

  deactivateTemplate(template: InvoiceTemplate): void {
    this.confirm
      .confirm({
        title: 'Deactivate invoice template?',
        message: deactivateInvoiceTemplateMessage(template.name, template.usage),
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.http.delete(`/api/invoice-templates/${template.id}`).subscribe({
          next: () => this.loadTemplates(),
          error: (error) =>
            this.errorMessage.set(
              getApiErrorMessage(error, 'Unable to deactivate invoice template.'),
            ),
        });
      });
  }
}
