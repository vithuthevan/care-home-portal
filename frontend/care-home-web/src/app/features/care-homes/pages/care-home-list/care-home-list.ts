import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

import { CareHomeLocation } from '../../models/care-home.model';
import { CareHomeService } from '../../services/care-home.service';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-care-home-list',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
    FilterBarComponent,
  ],
  templateUrl: './care-home-list.html',
})
export class CareHomeList implements OnInit {
  readonly entityRouteKey = entityRouteKey;
  private readonly careHomeService = inject(CareHomeService);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly totalCount = signal(0);
  readonly filterCompanyId = signal(0);
  readonly searchText = signal('');
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  page = 1;
  pageSize = 20;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    const companyId = Number(this.route.snapshot.queryParamMap.get('companyId') || 0);
    if (companyId) {
      this.filterCompanyId.set(companyId);
    }
    this.route.queryParamMap.subscribe((params) => {
      this.filterCompanyId.set(Number(params.get('companyId') || 0));
      this.page = 1;
      this.loadCareHomes();
    });
    this.loadCareHomes();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadCareHomes();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadCareHomes();
  }

  onSearchChange(value: string): void {
    this.searchText.set(value);
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadCareHomes();
    }, 300);
  }

  hasActiveFilters(): boolean {
    return !!this.searchText().trim() || !!this.filterCompanyId();
  }

  loadCareHomes(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const companyId = this.filterCompanyId() || undefined;
    const search = this.searchText().trim().toLowerCase();
    if (search) {
      this.careHomeService
        .getCareHomes()
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: (homes) => {
            const filtered = homes.filter((home) => {
              const matchesCompany = !companyId || home.companyId === companyId;
              const haystack = `${home.name} ${home.code} ${home.companyName}`.toLowerCase();
              return matchesCompany && haystack.includes(search);
            });
            this.totalCount.set(filtered.length);
            const start = (this.page - 1) * this.pageSize;
            this.careHomes.set(filtered.slice(start, start + this.pageSize));
          },
          error: (error) => {
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care homes.'));
          },
        });
      return;
    }
    this.careHomeService
      .getCareHomesPaged(this.page, this.pageSize, companyId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.careHomes.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care homes.'));
        },
      });
  }

  deactivateCareHome(careHome: CareHomeLocation): void {
    this.confirm
      .confirm({
        title: 'Deactivate care home',
        message: `Deactivate ${careHome.name}? It will no longer be available for new admissions or billing.`,
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.careHomeService.deactivateCareHome(entityRouteKey(careHome)).subscribe({
          next: () => {
            this.toast.success('Care home deactivated successfully.');
            this.loadCareHomes();
          },
          error: (error) => {
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to deactivate care home.'));
          },
        });
      });
  }
}
