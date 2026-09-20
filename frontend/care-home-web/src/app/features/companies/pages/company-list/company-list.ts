import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';

import { Company } from '../../models/company.model';
import { CompanyService } from '../../services/company.service';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';

@Component({
  selector: 'app-company-list',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    FilterBarComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
  ],
  templateUrl: './company-list.html',
})
export class CompanyList implements OnInit {
  private readonly companyService = inject(CompanyService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly companies = signal<Company[]>([]);
  readonly totalCount = signal(0);
  readonly searchText = signal('');
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  page = 1;
  pageSize = 50;

  ngOnInit(): void {
    this.loadCompanies();
  }

  applySearch(): void {
    this.page = 1;
    this.loadCompanies();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadCompanies();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.companyService
      .getCompaniesPaged(this.page, this.pageSize, this.searchText())
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.companies.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load companies.'));
        },
      });
  }

  deactivateCompany(company: Company): void {
    this.confirm
      .confirm({
        title: 'Deactivate company',
        message: `Deactivate ${company.name}? It will no longer be available for new work.`,
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.companyService.deactivateCompany(company.id).subscribe({
          next: () => {
            this.toast.success('Company deactivated successfully.');
            this.loadCompanies();
          },
          error: (error) => {
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to deactivate company.'));
          },
        });
      });
  }
}
