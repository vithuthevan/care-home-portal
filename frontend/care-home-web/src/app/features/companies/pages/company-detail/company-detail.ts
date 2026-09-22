import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { Company } from '../../models/company.model';
import { CompanyService } from '../../services/company.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-company-detail',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    IconActionButtonComponent,
  ],
  templateUrl: './company-detail.html',
})
export class CompanyDetail implements OnInit {
  readonly entityRouteKey = entityRouteKey;
  private readonly route = inject(ActivatedRoute);
  private readonly companyService = inject(CompanyService);
  private readonly careHomeService = inject(CareHomeService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);

  readonly company = signal<Company | null>(null);
  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const key = params.get('id') ?? '';
      this.load(key);
    });
  }

  private load(key: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.companyService
      .getCompany(key)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (company) => {
          this.company.set(company);
          this.breadcrumbs.set([
            { label: 'Companies', routerLink: '/companies' },
            { label: company.name },
          ]);
          this.careHomeService.getCareHomesPaged(1, 100, company.id).subscribe({
            next: (page) => this.careHomes.set(page.items),
          });
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load company.')),
      });
  }
}
