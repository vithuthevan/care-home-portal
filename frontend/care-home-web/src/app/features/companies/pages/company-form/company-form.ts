import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, of, switchMap } from 'rxjs';
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
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

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
  private readonly breadcrumbs = inject(BreadcrumbService);
  readonly auth = inject(AuthService);

  companyRouteKey: string | null = null;
  isEditMode = false;
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly logoPreviewUrl = signal<string | null>(null);
  private logoFile: File | null = null;

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    address: ['', Validators.maxLength(300)],
    phone: ['', Validators.maxLength(30)],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    isActive: [true],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.companyRouteKey = id;
      this.isEditMode = true;
      this.breadcrumbs.set([
        { label: 'Companies', routerLink: '/companies' },
        { label: 'Edit company' },
      ]);
      this.loadCompany();
      return;
    }
    this.breadcrumbs.set([
      { label: 'Companies', routerLink: '/companies' },
      { label: 'Add company' },
    ]);
  }

  private loadCompany(): void {
    if (this.companyRouteKey === null) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.companyService
      .getCompany(this.companyRouteKey)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (company) => {
          this.form.patchValue({
            name: company.name,
            address: company.address ?? '',
            phone: company.phone ?? '',
            email: company.email ?? '',
            isActive: company.isActive,
          });
          if (company.logoPath && this.companyRouteKey) {
            this.companyService.getLogo(this.companyRouteKey).subscribe({
              next: (blob) => this.setLogoPreview(URL.createObjectURL(blob)),
            });
          }
          this.breadcrumbs.set([
            { label: 'Companies', routerLink: '/companies' },
            { label: company.name, routerLink: ['/companies', entityRouteKey(company)] },
            { label: 'Edit company' },
          ]);
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
    const profile = {
      name: formValue.name,
      address: formValue.address.trim() || null,
      phone: formValue.phone.trim() || null,
      email: formValue.email.trim() || null,
    };

    if (this.isEditMode && this.companyRouteKey !== null) {
      this.companyService
        .updateCompany(this.companyRouteKey, {
          ...profile,
          isActive: formValue.isActive,
        })
        .pipe(
          switchMap((company) => this.uploadLogo(entityRouteKey(company))),
          finalize(() => this.isSaving.set(false)),
        )
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
      .createCompany(profile)
      .pipe(
        switchMap((company) => this.uploadLogo(entityRouteKey(company))),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          this.form.reset({ name: '', address: '', phone: '', email: '', isActive: true });
          this.clearLogo();
          this.toast.success('Company created successfully.');
          void this.router.navigate(['/companies']);
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to create company.'));
        },
      });
  }

  onLogoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }
    this.logoFile = file;
    this.setLogoPreview(URL.createObjectURL(file));
  }

  private uploadLogo(key: string) {
    if (!this.logoFile) {
      return of(null);
    }
    return this.companyService.uploadLogo(key, this.logoFile);
  }

  private setLogoPreview(url: string): void {
    const current = this.logoPreviewUrl();
    if (current?.startsWith('blob:')) {
      URL.revokeObjectURL(current);
    }
    this.logoPreviewUrl.set(url);
  }

  private clearLogo(): void {
    this.logoFile = null;
    this.setLogoPreview('');
    this.logoPreviewUrl.set(null);
  }
}
