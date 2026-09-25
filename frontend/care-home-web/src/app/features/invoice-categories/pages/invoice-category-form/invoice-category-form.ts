import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { InvoiceCategoryService } from '../../services/invoice-category.service';
import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ConfigurationSourceBadgeComponent } from '../../../../shared/ui/configuration-source-badge';

@Component({
  selector: 'app-invoice-category-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatSelectModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    ConfigurationSourceBadgeComponent,
  ],
  templateUrl: './invoice-category-form.html',
})
export class InvoiceCategoryForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly invoiceCategoryService = inject(InvoiceCategoryService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  invoiceCategoryId: number | null = null;
  isSystemDefaultCategory = false;

  isEditMode = false;
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(30)]],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    groupingMode: ['PerFunder', Validators.required],
    isActive: [true],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.invoiceCategoryId = Number(id);
      this.isEditMode = true;
      this.loadInvoiceCategory();
    }
  }

  private loadInvoiceCategory(): void {
    if (this.invoiceCategoryId === null) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.invoiceCategoryService
      .getInvoiceCategory(this.invoiceCategoryId)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: (category) => {
          this.isSystemDefaultCategory = category.configurationSource === 'SystemDefault';
          this.form.patchValue({
            code: category.code,
            name: category.name,
            description: category.description ?? '',
            groupingMode: category.groupingMode || 'PerFunder',
            isActive: category.isActive,
          });
          if (this.isSystemDefaultCategory) {
            this.form.controls.code.disable();
          }
        },

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load invoice category.'));
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

    const value = this.form.getRawValue();

    const request = {
      code: (value.code || this.form.controls.code.value) as string,
      name: value.name,
      description: value.description,
      groupingMode: value.groupingMode,
    };

    if (this.isEditMode && this.invoiceCategoryId !== null) {
      this.invoiceCategoryService
        .updateInvoiceCategory(this.invoiceCategoryId, {
          ...request,
          isActive: value.isActive,
        })
        .pipe(
          finalize(() => {
            this.isSaving.set(false);
          }),
        )
        .subscribe({
          next: () => {
            this.router.navigate(['/invoice-categories']);
          },

          error: (error) => {
            logApiFailure(error);

            this.errorMessage.set(getApiErrorMessage(error, 'Unable to update invoice category.'));
          },
        });

      return;
    }

    this.invoiceCategoryService
      .createInvoiceCategory(request)
      .pipe(
        finalize(() => {
          this.isSaving.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.router.navigate(['/invoice-categories']);
        },

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to create invoice category.'));
        },
      });
  }
}
