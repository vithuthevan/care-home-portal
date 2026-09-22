import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AuthService } from './core/auth.service';
import { BreadcrumbService } from './shared/ui/breadcrumb.service';
import { ThemeService } from './shared/ui/theme.service';
import { CareHomePortalThemeService } from './core/care-home-portal-theme.service';
import { COMMERCIAL_REVENUE_ENABLED } from './core/commercial-revenue.feature';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly isMobile = signal(false);
  readonly menuOpen = signal(false);
  readonly breadcrumbs = inject(BreadcrumbService);
  readonly themeService = inject(ThemeService);
  private readonly careHomePortalTheme = inject(CareHomePortalThemeService);
  readonly operationsOpen = signal(true);
  readonly billingSetupOpen = signal(true);
  readonly billingOpen = signal(true);
  readonly showCommercialRevenueNav = COMMERCIAL_REVENUE_ENABLED;
  readonly revenueOpen = signal(true);
  readonly assuranceOpen = signal(true);
  readonly reportingOpen = signal(true);
  readonly adminOpen = signal(true);
  /** Desktop icon-only sidebar (not used on mobile drawer). */
  readonly sidebarCollapsed = signal(false);

  constructor() {
    this.themeService.init();
    this.careHomePortalTheme.init();
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem('carehome.sidebarCollapsed');
      if (stored === '1') {
        this.sidebarCollapsed.set(true);
      }
      const query = window.matchMedia('(max-width: 1024px)');
      this.isMobile.set(query.matches);
      this.menuOpen.set(!query.matches);
      query.addEventListener('change', (event) => {
        this.isMobile.set(event.matches);
        this.menuOpen.set(!event.matches);
        if (event.matches) {
          this.sidebarCollapsed.set(false);
        }
      });
    }

    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.breadcrumbs.setFromUrl(this.router.url);
    });
    this.breadcrumbs.setFromUrl(this.router.url);
  }

  toggleMenu(): void {
    if (this.isMobile()) {
      this.menuOpen.update((open) => !open);
      return;
    }
    this.toggleSidebarCollapsed();
  }

  toggleSidebarCollapsed(): void {
    this.sidebarCollapsed.update((v) => {
      const next = !v;
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('carehome.sidebarCollapsed', next ? '1' : '0');
      }
      return next;
    });
  }

  closeMenu(): void {
    if (this.isMobile()) {
      this.menuOpen.set(false);
    }
  }

  initials(): string {
    const name = this.auth.currentUser()?.displayName?.trim() || 'User';
    return name
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');
  }

  roleLabel(): string {
    return this.auth.currentUser()?.roles?.[0] || 'User';
  }

  setColorMode(mode: 'light' | 'dark'): void {
    this.themeService.setColorMode(mode);
  }
}
