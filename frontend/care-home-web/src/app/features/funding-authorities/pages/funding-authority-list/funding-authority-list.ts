import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { FundingAuthority } from '../../models/funding-authority.model';
import { FundingAuthorityService } from '../../services/funding-authority.service';
import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';

@Component({
  selector: 'app-funding-authority-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
  ],
  templateUrl: './funding-authority-list.html',
})
export class FundingAuthorityList implements OnInit {
  private readonly fundingAuthorityService = inject(FundingAuthorityService);
  private readonly confirm = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);

  readonly fundingAuthorities = signal<FundingAuthority[]>([]);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadFundingAuthorities();
  }

  loadFundingAuthorities(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.fundingAuthorityService
      .getFundingAuthorities()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (fundingAuthorities) => this.fundingAuthorities.set(fundingAuthorities),

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load funding authorities.'));
        },
      });
  }

  deactivateFundingAuthority(authority: FundingAuthority): void {
    this.confirm
      .confirm({
        title: 'Deactivate funding authority',
        message: `Deactivate ${authority.name}? It will no longer be available for new contracts.`,
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.fundingAuthorityService.deactivateFundingAuthority(authority.id).subscribe({
          next: () => this.loadFundingAuthorities(),
          error: (error) =>
            this.errorMessage.set(
              getApiErrorMessage(error, 'Unable to deactivate funding authority.'),
            ),
        });
      });
  }
}
