import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import {
  InvoiceTemplate,
  UpsertInvoiceTemplateRequest,
} from '../../models/invoice-template.model';

@Component({
  selector: 'app-invoice-template-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
  ],
  templateUrl: './invoice-template-form.html',
})
export class InvoiceTemplateFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  templateId: number | null = null;
  isEditMode = false;

  readonly categories = signal<{ id: number; name: string }[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly isLoading = signal(false);
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
    isActive: [true],
  });

  ngOnInit(): void {
    this.loadCategories();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.templateId = Number(id);
      this.isEditMode = true;
      this.loadTemplate();
    }
  }

  private loadCategories(): void {
    this.http.get<{ id: number; name: string }[]>('/api/invoice-categories?activeOnly=true').subscribe({
      next: (x) => this.categories.set(x),
      error: (error) =>
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load categories.')),
    });
  }

  private loadTemplate(): void {
    if (this.templateId === null) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http
      .get<InvoiceTemplate>(`/api/invoice-templates/${this.templateId}`)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (template) => {
          this.form.patchValue({
            name: template.name,
            invoiceCategoryId: template.invoiceCategoryId,
            headerText1: template.headerText1 ?? '',
            footerText: template.footerText ?? '',
            bankAccountName: template.bankAccountName ?? '',
            sortCode: template.sortCode ?? '',
            accountNumber: template.accountNumber ?? '',
            contactName: template.contactName ?? '',
            contactEmail: template.contactEmail ?? '',
            emailSubjectTemplate:
              template.emailSubjectTemplate ?? 'Invoice {{InvoiceNumber}}',
            emailBodyTemplate: template.emailBodyTemplate ?? 'Please find the invoice attached.',
            isActive: template.isActive,
          });
        },
        error: (error) => {
          logApiFailure(error);
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice template.'));
        },
      });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.isSaving.set(true);

    const raw = this.form.getRawValue();
    const payload: UpsertInvoiceTemplateRequest = {
      name: raw.name,
      invoiceCategoryId: raw.invoiceCategoryId,
      headerText1: raw.headerText1 || null,
      footerText: raw.footerText || null,
      bankAccountName: raw.bankAccountName || null,
      sortCode: raw.sortCode || null,
      accountNumber: raw.accountNumber || null,
      contactName: raw.contactName || null,
      contactEmail: raw.contactEmail || null,
      emailSubjectTemplate: raw.emailSubjectTemplate,
      emailBodyTemplate: raw.emailBodyTemplate,
      isActive: raw.isActive,
    };

    const request$ =
      this.isEditMode && this.templateId !== null
        ? this.http.put<InvoiceTemplate>(`/api/invoice-templates/${this.templateId}`, payload)
        : this.http.post<InvoiceTemplate>('/api/invoice-templates', payload);

    request$.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: () => {
        void this.router.navigate(['/invoice-templates']);
      },
      error: (error) => {
        logApiFailure(error);
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to save invoice template.'));
      },
    });
  }
}
