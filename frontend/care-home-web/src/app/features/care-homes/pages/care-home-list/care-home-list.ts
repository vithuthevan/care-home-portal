import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';

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

@Component({
  selector: 'app-care-home-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
  ],
  templateUrl: './care-home-list.html',
})
export class CareHomeList implements OnInit {
  private readonly careHomeService = inject(CareHomeService);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly totalCount = signal(0);
  readonly filterCompanyId = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  page = 1;
  pageSize = 50;

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

  loadCareHomes(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const companyId = this.filterCompanyId() || undefined;
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
        this.careHomeService.deactivateCareHome(careHome.id).subscribe({
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
