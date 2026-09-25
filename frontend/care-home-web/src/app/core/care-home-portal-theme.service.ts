import { inject, Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { AppThemeId, ThemeService } from '../shared/ui/theme.service';
import { CareHomeService } from '../features/care-homes/services/care-home.service';

const PORTAL_PATH = /^\/care-homes\/([^/]+)\/(dashboard|settings)(\/|$)/;

@Injectable({ providedIn: 'root' })
export class CareHomePortalThemeService {
  private readonly router = inject(Router);
  private readonly theme = inject(ThemeService);
  private readonly careHomes = inject(CareHomeService);
  private activeCareHomeKey: string | null = null;

  init(): void {
    this.theme.applyDefaultAccent();
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      void this.syncFromUrl(this.router.url);
    });
    void this.syncFromUrl(this.router.url);
  }

  private async syncFromUrl(url: string): Promise<void> {
    const path = url.split('?')[0] ?? url;
    const match = PORTAL_PATH.exec(path);
    if (!match) {
      this.activeCareHomeKey = null;
      this.theme.applyDefaultAccent();
      return;
    }

    const key = match[1];
    if (key === this.activeCareHomeKey) {
      return;
    }

    this.activeCareHomeKey = key;
    this.careHomes.getCareHome(key).subscribe({
      next: (home) => {
        const accent = (home.portalAccentTheme as AppThemeId | null) ?? 'green';
        this.theme.apply(accent);
      },
      error: () => this.theme.applyDefaultAccent(),
    });
  }
}
