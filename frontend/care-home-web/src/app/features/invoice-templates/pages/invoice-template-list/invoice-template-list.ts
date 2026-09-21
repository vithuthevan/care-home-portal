import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge';
import { FilterBarComponent } from '../../../../shared/ui/filter-bar';

@Component({
  selector: 'app-invoice-template-list',
  imports: [
    RouterLink,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    StatusBadgeComponent,
    FilterBarComponent,
  ],
  templateUrl: './invoice-template-list.html',
})
export class InvoiceTemplateListPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  readonly items = signal<any[]>([]);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.http.get<any[]>('/api/invoice-templates').subscribe({
      next: (x) => this.items.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load templates.')),
    });
  }
}
