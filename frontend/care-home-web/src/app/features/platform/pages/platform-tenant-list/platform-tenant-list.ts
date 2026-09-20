import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

import { getApiErrorMessage } from '../../../../core/api-error';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';

interface TenantRow {
  id: number;
  publicId: string;
  name: string;
  tradingName?: string | null;
  email?: string | null;
  isActive: boolean;
}

@Component({
  selector: 'app-platform-tenant-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
  ],
  templateUrl: './platform-tenant-list.html',
})
export class PlatformTenantListPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly tenants = signal<TenantRow[]>([]);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.http.get<TenantRow[]>('/api/platform/tenants').subscribe({
      next: (tenants) => this.tenants.set(tenants),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load organisations.')),
    });
  }
}
