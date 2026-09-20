import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';
import { IconActionButtonComponent } from '../../../../shared/ui/icon-action-button';
import { TablePaginationComponent } from '../../../../shared/ui/table-pagination';
import { PagedResult } from '../../../../core/models';

@Component({
  selector: 'app-user-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    StatusBadgeComponent,
    IconActionButtonComponent,
    TablePaginationComponent,
  ],
  templateUrl: './user-list.html',
})
export class UserListPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly homesApi = inject(CareHomeService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly users = signal<any[]>([]);
  readonly totalCount = signal(0);
  readonly homes = signal<CareHomeLocation[]>([]);
  readonly errorMessage = signal<string | null>(null);
  page = 1;
  pageSize = 50;

  ngOnInit(): void {
    this.loadUsers();
    this.homesApi.getCareHomes().subscribe((x) => this.homes.set(x));
  }

  loadUsers(): void {
    const params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    this.http.get<PagedResult<any>>('/api/users', { params }).subscribe({
      next: (page) => {
        this.users.set(page.items);
        this.totalCount.set(page.totalCount);
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Unable to load users.')),
    });
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadUsers();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.loadUsers();
  }

  homeNames(ids: number[] | undefined): string {
    if (!ids?.length) {
      return 'All accessible homes';
    }
    const names = this.homes()
      .filter((home) => ids.includes(home.id))
      .map((home) => home.name);
    return names.join(', ') || String(ids.length);
  }

  deactivate(id: string): void {
    this.confirm
      .confirm({
        title: 'Deactivate user',
        message: 'Deactivate this user? They will no longer be able to sign in.',
        confirmLabel: 'Deactivate',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.http.post(`/api/users/${id}/deactivate`, {}).subscribe({
          next: () => {
            this.toast.success('User deactivated successfully.');
            this.loadUsers();
          },
        });
      });
  }
}
