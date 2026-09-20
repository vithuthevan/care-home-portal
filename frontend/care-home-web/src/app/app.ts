import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

import { AuthService } from './core/auth.service';
import { BreadcrumbService } from './shared/ui/breadcrumb.service';

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
  readonly operationsOpen = signal(true);
  readonly billingSetupOpen = signal(true);
  readonly billingOpen = signal(true);
  readonly reportingOpen = signal(true);
  readonly adminOpen = signal(true);

  constructor() {
    if (typeof window !== 'undefined') {
      const query = window.matchMedia('(max-width: 1024px)');
      this.isMobile.set(query.matches);
      this.menuOpen.set(!query.matches);
      query.addEventListener('change', (event) => {
        this.isMobile.set(event.matches);
        this.menuOpen.set(!event.matches);
      });
    }

    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.breadcrumbs.setFromUrl(this.router.url);
    });
    this.breadcrumbs.setFromUrl(this.router.url);
  }

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
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
}
