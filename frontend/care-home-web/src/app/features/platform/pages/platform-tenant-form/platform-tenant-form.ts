import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { optionalEmail } from '../../../../shared/format/optional-email';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';

@Component({
  selector: 'app-platform-tenant-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
  ],
  templateUrl: './platform-tenant-form.html',
})
export class PlatformTenantFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  tenantId: number | null = null;
  isEditMode = false;
  readonly errorMessage = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly createdNotice = signal<{
    email: string;
    simulated: boolean;
    temporaryPassword?: string | null;
  } | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    tradingName: [''],
    registrationNumber: [''],
    address: [''],
    phone: [''],
    email: [''],
    website: [''],
    isActive: [true],
    adminEmail: ['', [Validators.required, Validators.email]],
    adminDisplayName: [''],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.tenantId = Number(id);
      this.isEditMode = true;
      this.form.controls.adminEmail.clearValidators();
      this.form.controls.adminEmail.updateValueAndValidity();
      this.isLoading.set(true);
      this.errorMessage.set(null);
      this.http
        .get<typeof this.form.value>(`/api/platform/tenants/${id}`)
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: (tenant) => this.form.patchValue(tenant),
          error: (error) =>
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to load organisation.')),
        });
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    const body = this.form.getRawValue();
    const payload = { ...body, email: optionalEmail(body.email) };
    const request$ = this.isEditMode
      ? this.http.put(`/api/platform/tenants/${this.tenantId}`, payload)
      : this.http.post<CreateOrganisationResponse>('/api/platform/tenants', payload);

    request$.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: (result) => {
        if (this.isEditMode) {
          void this.router.navigate(['/platform/tenants']);
          return;
        }

        const created = result as CreateOrganisationResponse;
        this.createdNotice.set({
          email: body.adminEmail,
          simulated: !!created.credentialsEmailSimulated,
          temporaryPassword: created.temporaryPassword,
        });
      },
      error: (error) => {
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to save organisation.'));
      },
    });
  }
}

interface CreateOrganisationResponse {
  credentialsEmailed?: boolean;
  credentialsEmailSimulated?: boolean;
  temporaryPassword?: string | null;
}
