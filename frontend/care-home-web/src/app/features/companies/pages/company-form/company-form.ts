import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';

import { CompanyService } from '../../services/company.service';
import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ToastService } from '../../../../shared/ui/toast.service';

@Component({
  selector: 'app-company-form',
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
  templateUrl: './company-form.html',
})
export class CompanyForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly companyService = inject(CompanyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  companyId: number | null = null;
  isEditMode = false;
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    isActive: [true],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.companyId = Number(id);
      this.isEditMode = true;
      this.loadCompany();
    }
  }

  private loadCompany(): void {
    if (this.companyId === null) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.companyService
      .getCompany(this.companyId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (company) => {
          this.form.patchValue({
            name: company.name,
            isActive: company.isActive,
          });
        },
        error: (error) => {
          logApiFailure(error);
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load company.'));
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
    const formValue = this.form.getRawValue();

    if (this.isEditMode && this.companyId !== null) {
      this.companyService
        .updateCompany(this.companyId, {
          name: formValue.name,
          isActive: formValue.isActive,
        })
        .pipe(finalize(() => this.isSaving.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('Company updated successfully.');
            this.router.navigate(['/companies']);
          },
          error: (error) => {
            this.errorMessage.set(getApiErrorMessage(error, 'Unable to update company.'));
          },
        });
      return;
    }

    this.companyService
      .createCompany({ name: formValue.name })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (company) => {
          this.toast.success('Company created successfully.');
          void this.router.navigate(['/companies', company.id]);
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to create company.'));
        },
      });
  }
}
