import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { NominalCode } from '../../models/nominal-code.model';
import { NominalCodeService } from '../../services/nominal-code.service';
import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import {
  configurationSourceLabel,
  deactivateMasterDataMessage,
  masterDataUsageLabel,
} from '../../../../shared/format/master-data-usage';

@Component({
  selector: 'app-nominal-code-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
    EmptyStateComponent,
  ],
  templateUrl: './nominal-code-list.html',
})
export class NominalCodeList implements OnInit {
  private readonly nominalCodeService = inject(NominalCodeService);
  private readonly confirm = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);

  readonly nominalCodes = signal<NominalCode[]>([]);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadNominalCodes();
  }

  loadNominalCodes(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.nominalCodeService
      .getNominalCodes()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (nominalCodes) => this.nominalCodes.set(nominalCodes),

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load nominal codes.'));
        },
      });
  }

  usageLabel = masterDataUsageLabel;
  sourceLabel = configurationSourceLabel;

  deactivateNominalCode(nominalCode: NominalCode): void {
    this.confirm
      .confirm({
        title: 'Deactivate nominal code?',
        message: deactivateMasterDataMessage(nominalCode.name, nominalCode.usage),
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.nominalCodeService.deactivateNominalCode(nominalCode.id).subscribe({
          next: () => this.loadNominalCodes(),
          error: (error) =>
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to deactivate nominal code.')),
        });
      });
  }
}
