import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { finalize } from 'rxjs';

import { CareHomeService } from '../../services/care-home.service';
import { CareHomeLocation } from '../../models/care-home.model';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ToastService } from '../../../../shared/ui/toast.service';
import { APP_THEME_OPTIONS, AppThemeId, ThemeService } from '../../../../shared/ui/theme.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-care-home-portal-settings',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
  ],
  templateUrl: './care-home-portal-settings.html',
})
export class CareHomePortalSettingsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly careHomeService = inject(CareHomeService);
  private readonly themeService = inject(ThemeService);
  private readonly breadcrumbs = inject(BreadcrumbService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly themeOptions = APP_THEME_OPTIONS;

  readonly home = signal<CareHomeLocation | null>(null);
  readonly selectedTheme = signal<AppThemeId>('green');
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  private routeKey = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.routeKey = params.get('id') ?? '';
      this.load();
    });
  }

  private load(): void {
    if (!this.routeKey) {
      return;
    }
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.careHomeService
      .getCareHome(this.routeKey)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (home) => {
          this.home.set(home);
          const accent = (home.portalAccentTheme as AppThemeId | null) ?? 'green';
          this.selectedTheme.set(accent);
          this.themeService.apply(accent);
          this.breadcrumbs.set([
            { label: 'Care Homes', routerLink: '/care-homes' },
            { label: home.name, routerLink: ['/care-homes', entityRouteKey(home), 'dashboard'] },
            { label: 'Portal settings' },
          ]);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care home.')),
      });
  }

  selectTheme(themeId: AppThemeId): void {
    this.selectedTheme.set(themeId);
    this.themeService.apply(themeId);
  }

  save(): void {
    const current = this.home();
    if (!current || this.isSaving() || !this.auth.canWrite()) {
      return;
    }
    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.careHomeService
      .updatePortalAppearance(entityRouteKey(current), this.selectedTheme())
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (updated) => {
          this.home.set(updated);
          this.toast.success('Portal appearance saved.');
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to save portal appearance.')),
      });
  }

  dashboardLink(): string[] {
    const current = this.home();
    if (!current) {
      return ['/care-homes'];
    }
    return ['/care-homes', entityRouteKey(current), 'dashboard'];
  }
}
