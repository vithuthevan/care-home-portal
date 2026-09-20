import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';

import { Company } from '../../models/company.model';
import { CompanyService } from '../../services/company.service';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';

@Component({
  selector: 'app-company-detail',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './company-detail.html',
})
export class CompanyDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly companyService = inject(CompanyService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);

  readonly company = signal<Company | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      this.load(id);
    });
  }

  private load(id: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.companyService
      .getCompany(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (company) => {
          this.company.set(company);
          this.breadcrumbs.set([
            { label: 'Companies', routerLink: '/companies' },
            { label: company.name },
          ]);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load company.')),
      });
  }
}
