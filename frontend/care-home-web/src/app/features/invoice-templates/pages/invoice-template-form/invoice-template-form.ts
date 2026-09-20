import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { ToastService } from '../../../../shared/ui/toast.service';

@Component({
  selector: 'app-invoice-template-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
  ],
  templateUrl: './invoice-template-form.html',
})
export class InvoiceTemplateFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly categories = signal<any[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly isSaving = signal(false);

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    invoiceCategoryId: [0, Validators.min(1)],
    headerText1: [''],
    footerText: [''],
    bankAccountName: [''],
    sortCode: [''],
    accountNumber: [''],
    contactName: [''],
    contactEmail: [''],
    emailSubjectTemplate: ['Invoice {{InvoiceNumber}}'],
    emailBodyTemplate: ['Please find the invoice attached.'],
  });

  ngOnInit(): void {
    this.http.get<any[]>('/api/invoice-categories?activeOnly=true').subscribe({
      next: (x) => this.categories.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load categories.')),
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.isSaving.set(true);
    this.http
      .post('/api/invoice-templates', this.form.getRawValue())
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Invoice template saved.');
          void this.router.navigate(['/invoice-templates']);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to save template.')),
      });
  }
}
