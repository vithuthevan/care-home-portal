import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { InvoiceCategory } from '../../models/invoice-category.model';
import { InvoiceCategoryService } from '../../services/invoice-category.service';
import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';

@Component({
  selector: 'app-invoice-category-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
    FilterBarComponent,
    EmptyStateComponent,
  ],
  templateUrl: './invoice-category-list.html',
})
export class InvoiceCategoryList implements OnInit {
  private readonly invoiceCategoryService = inject(InvoiceCategoryService);
  private readonly confirm = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);

  readonly invoiceCategories = signal<InvoiceCategory[]>([]);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadInvoiceCategories();
  }

  loadInvoiceCategories(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.invoiceCategoryService
      .getInvoiceCategories()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (invoiceCategories) => this.invoiceCategories.set(invoiceCategories),

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice categories.'));
        },
      });
  }

  deactivateInvoiceCategory(category: InvoiceCategory): void {
    this.confirm
      .confirm({
        title: 'Deactivate invoice category',
        message: `Deactivate ${category.name}? It will no longer be available for new billing.`,
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.invoiceCategoryService.deactivateInvoiceCategory(category.id).subscribe({
          next: () => this.loadInvoiceCategories(),
          error: (error) =>
            this.errorMessage.set(
              getApiErrorMessage(error, 'Unable to deactivate invoice category.'),
            ),
        });
      });
  }
}
