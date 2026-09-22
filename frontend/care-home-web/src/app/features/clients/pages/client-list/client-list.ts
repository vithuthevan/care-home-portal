import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, merge } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';

import { Client } from '../../models/client.model';
import { ClientService } from '../../services/client.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { CompanyService } from '../../../companies/services/company.service';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-client-list',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    DisplayDatePipe,
    StatusBadgeComponent,
    FilterBarComponent,
    TablePaginationComponent,
    IconActionButtonComponent,
    MatTooltipModule,
  ],
  templateUrl: './client-list.html',
})
export class ClientList implements OnInit {
  readonly entityRouteKey = entityRouteKey;
  private readonly clientService = inject(ClientService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly careHomeService = inject(CareHomeService);
  private readonly companyService = inject(CompanyService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly clients = signal<Client[]>([]);
  readonly totalCount = signal(0);
  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly companyContext = signal<{ name: string; routeKey: string } | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  searchText = '';
  selectedCareHomeId = 0;
  selectedCompanyId = 0;
  companyFilterKey: string | null = null;
  showArchived = false;
  page = 1;
  pageSize = 20;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    const careHomeId = Number(this.route.snapshot.queryParamMap.get('careHomeId') || 0);
    if (careHomeId) {
      this.selectedCareHomeId = careHomeId;
    }
    this.loadCareHomes();

    merge(this.route.queryParamMap).subscribe((params) => {
      const companyKey = params.get('company');
      const legacyCompanyId = Number(params.get('companyId') || 0);
      const careHomeId = Number(params.get('careHomeId') || 0);
      this.page = 1;

      if (careHomeId && !companyKey && !legacyCompanyId) {
        this.applyCareHomeFilter(careHomeId);
        return;
      }

      if (companyKey) {
        this.applyCompanyFilterKey(companyKey);
        return;
      }

      if (legacyCompanyId) {
        this.applyCompanyFilterKey(String(legacyCompanyId));
        return;
      }

      this.companyContext.set(null);
      this.companyFilterKey = null;
      this.selectedCompanyId = 0;
      this.breadcrumbs.set([{ label: 'Residents' }]);
      this.loadClients();
    });
  }

  private applyCareHomeFilter(careHomeId: number): void {
    this.selectedCareHomeId = careHomeId;
    this.careHomeService.getCareHomes().subscribe({
      next: (homes) => {
        const home = homes.find((h) => h.id === careHomeId);
        if (home) {
          this.breadcrumbs.set([
            { label: 'Care Homes', routerLink: '/care-homes' },
            { label: home.name, routerLink: ['/care-homes', entityRouteKey(home), 'dashboard'] },
            { label: 'Residents' },
          ]);
        } else {
          this.breadcrumbs.set([{ label: 'Residents' }]);
        }
        this.loadClients();
      },
      error: () => {
        this.breadcrumbs.set([{ label: 'Residents' }]);
        this.loadClients();
      },
    });
  }

  private applyCompanyFilterKey(key: string): void {
    this.companyFilterKey = key;
    this.companyService.getCompany(key).subscribe({
      next: (company) => {
        const routeKey = entityRouteKey(company);
        this.companyContext.set({ name: company.name, routeKey });
        this.selectedCompanyId = company.id;
        this.breadcrumbs.set([
          { label: 'Companies', routerLink: '/companies' },
          { label: company.name, routerLink: ['/companies', routeKey] },
          { label: 'Residents' },
        ]);
        this.loadClients();
      },
      error: () => {
        this.companyContext.set(null);
        this.companyFilterKey = null;
        this.selectedCompanyId = 0;
        this.breadcrumbs.set([{ label: 'Residents' }]);
        this.errorMessage.set('Unable to load the selected company.');
        this.loadClients();
      },
    });
  }

  loadCareHomes(): void {
    this.careHomeService.getCareHomes().subscribe({
      next: (careHomes) => this.careHomes.set(careHomes.filter((x) => x.isActive)),
    });
  }

  loadClients(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.clientService
      .getClients(
        this.searchText.trim() || undefined,
        this.selectedCareHomeId || undefined,
        this.showArchived,
        this.page,
        this.pageSize,
        {
          company: this.companyFilterKey || undefined,
          companyId: this.companyFilterKey ? undefined : this.selectedCompanyId || undefined,
        },
      )
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.clients.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load residents.'));
        },
      });
  }

  onSearchChange(_value: string): void {
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
    this.searchTimer = setTimeout(() => this.search(), 300);
  }

  hasActiveFilters(): boolean {
    return !!(
      this.searchText.trim() ||
      this.selectedCareHomeId ||
      this.selectedCompanyId ||
      this.companyFilterKey ||
      this.showArchived
    );
  }

  search(): void {
    this.page = 1;
    this.loadClients();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedCareHomeId = 0;
    this.selectedCompanyId = 0;
    this.companyFilterKey = null;
    this.companyContext.set(null);
    this.page = 1;
    if (this.route.snapshot.queryParamMap.get('company') || this.route.snapshot.queryParamMap.get('companyId')) {
      void this.router.navigate(['/clients']);
      return;
    }
    if (this.route.snapshot.queryParamMap.get('careHomeId')) {
      void this.router.navigate(['/clients']);
      return;
    }
    this.breadcrumbs.set([{ label: 'Residents' }]);
    this.loadClients();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadClients();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadClients();
  }

  openClient(client: Client): void {
    void this.router.navigate(['/clients', entityRouteKey(client)]);
  }

  addResidentQueryParams(): Record<string, number> {
    return this.selectedCareHomeId ? { careHomeId: this.selectedCareHomeId } : {};
  }

  archiveClient(client: Client): void {
    this.confirm
      .confirm({
        title: 'Archive resident',
        message: `Archive ${client.firstName} ${client.lastName}? The record is retained but hidden from the default list.`,
        confirmLabel: 'Archive',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.clientService.archiveClient(client.id).subscribe({
          next: () => {
            this.toast.success('Resident archived successfully.');
            this.loadClients();
          },
          error: (error) => {
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to archive client.'));
          },
        });
      });
  }
}
