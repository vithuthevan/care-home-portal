import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';

import { Client } from '../../models/client.model';
import { ClientService } from '../../services/client.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { DisplayDatePipe } from '../../../../shared/format/display-date.pipe';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';
import { ConfirmDialogService } from '../../../../shared/ui/confirm-dialog.service';
import { ToastService } from '../../../../shared/ui/toast.service';

@Component({
  selector: 'app-client-list',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    DisplayDatePipe,
    StatusBadgeComponent,
    FilterBarComponent,
  ],
  templateUrl: './client-list.html',
})
export class ClientList implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly router = inject(Router);
  private readonly careHomeService = inject(CareHomeService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly clients = signal<Client[]>([]);
  readonly totalCount = signal(0);
  readonly careHomes = signal<CareHomeLocation[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  searchText = '';
  selectedCareHomeId = 0;
  showArchived = false;

  ngOnInit(): void {
    this.loadCareHomes();
    this.loadClients();
  }

  loadCareHomes(): void {
    this.careHomeService.getCareHomes().subscribe({
      next: (careHomes) => this.careHomes.set(careHomes.filter((x) => x.isActive)),
    });
  }

  loadClients(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.clientService
      .getClients(
        this.searchText.trim() || undefined,
        this.selectedCareHomeId || undefined,
        this.showArchived,
      )
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.clients.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load clients.'));
        },
      });
  }

  search(): void {
    this.loadClients();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedCareHomeId = 0;
    this.loadClients();
  }

  openClient(id: number): void {
    void this.router.navigate(['/clients', id]);
  }

  archiveClient(client: Client): void {
    this.confirm
      .confirm({
        title: 'Archive client',
        message: `Archive ${client.firstName} ${client.lastName}? The record is retained but hidden from the default list.`,
        confirmLabel: 'Archive',
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.clientService.archiveClient(client.id).subscribe({
          next: () => {
            this.toast.success('Client archived successfully.');
            this.loadClients();
          },
          error: (error) => {
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to archive client.'));
          },
        });
      });
  }
}
