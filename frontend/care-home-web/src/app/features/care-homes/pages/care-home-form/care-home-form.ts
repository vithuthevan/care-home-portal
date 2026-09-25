import { Component, inject, OnInit, signal } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { finalize, map, Observable, of, switchMap } from 'rxjs';
import { CareHomeLocation } from '../../models/care-home.model';

import { Company } from '../../../companies/models/company.model';

import { CompanyService } from '../../../companies/services/company.service';

import { CareHomeService } from '../../services/care-home.service';

import { getApiErrorMessage, logApiFailure } from '../../../../core/api-error';
import { optionalEmail } from '../../../../shared/format/optional-email';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ToastService } from '../../../../shared/ui/toast.service';
import { BreadcrumbService } from '../../../../shared/ui/breadcrumb.service';
import { entityRouteKey } from '../../../../shared/routing/entity-route';

@Component({
  selector: 'app-care-home-form',

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

  templateUrl: './care-home-form.html',
})
export class CareHomeForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  private readonly careHomeService = inject(CareHomeService);

  private readonly companyService = inject(CompanyService);

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly breadcrumbs = inject(BreadcrumbService);

  careHomeRouteKey: string | null = null;

  readonly companies = signal<Company[]>([]);

  readonly assignedCompanyId = signal<number | null>(null);

  isEditMode = false;

  readonly isLoading = signal(false);

  readonly isSaving = signal(false);

  readonly errorMessage = signal<string | null>(null);

  readonly logoPreviewUrl = signal<string | null>(null);

  private logoFile: File | null = null;

  readonly form = this.formBuilder.nonNullable.group({
    companyId: [0],

    code: ['', [Validators.required, Validators.maxLength(30)]],

    name: ['', [Validators.required, Validators.maxLength(150)]],

    bedCapacity: [0, [Validators.required, Validators.min(0)]],

    address: ['', Validators.maxLength(200)],

    phone: ['', Validators.maxLength(30)],

    email: ['', [Validators.email, Validators.maxLength(150)]],

    managerName: ['', Validators.maxLength(150)],

    managerPhone: ['', Validators.maxLength(30)],

    managerEmail: ['', [Validators.email, Validators.maxLength(150)]],

    bankDetails: [''],

    isActive: [true],
  });

  get selectableCompanies(): Company[] {
    return this.companies().filter(
      (company) => company.isActive || (this.isEditMode && company.id === this.assignedCompanyId()),
    );
  }

  ngOnInit(): void {
    this.loadCompanies();

    const key = this.route.snapshot.paramMap.get('id');

    if (key) {
      this.careHomeRouteKey = key;

      this.isEditMode = true;

      this.breadcrumbs.set([
        { label: 'Care Homes', routerLink: '/care-homes' },
        { label: 'Edit care home' },
      ]);

      this.loadCareHome();
      return;
    }

    this.breadcrumbs.set([
      { label: 'Care Homes', routerLink: '/care-homes' },
      { label: 'Add care home' },
    ]);

    const companyKey =
      this.route.snapshot.queryParamMap.get('company') ||
      this.route.snapshot.queryParamMap.get('companyId');
    if (companyKey) {
      this.companyService.getCompany(companyKey).subscribe({
        next: (company) => {
          if (company.isActive) {
            this.form.patchValue({ companyId: company.id });
          }
        },
      });
    }
  }

  cancelLink(): string[] {
    if (this.isEditMode && this.careHomeRouteKey) {
      return ['/care-homes', this.careHomeRouteKey, 'dashboard'];
    }
    return ['/care-homes'];
  }

  private loadCompanies(): void {
    this.companyService.getCompanies().subscribe({
      next: (companies) => {
        this.companies.set(companies);
      },

      error: (error) => {
        logApiFailure(error);

        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load companies.'));
      },
    });
  }

  private loadCareHome(): void {
    if (this.careHomeRouteKey === null) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.careHomeService
      .getCareHome(this.careHomeRouteKey)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: (careHome) => {
          this.assignedCompanyId.set(careHome.companyId);

          this.breadcrumbs.set([
            { label: 'Care Homes', routerLink: '/care-homes' },
            { label: careHome.name, routerLink: ['/care-homes', entityRouteKey(careHome), 'dashboard'] },
            { label: 'Edit care home' },
          ]);

          if (careHome.logoPath && this.careHomeRouteKey) {
            this.careHomeService.getLogo(this.careHomeRouteKey).subscribe({
              next: (blob) => this.setLogoPreview(URL.createObjectURL(blob)),
            });
          }

          this.form.patchValue({
            companyId: careHome.companyId ?? 0,

            code: careHome.code,

            name: careHome.name,

            bedCapacity: careHome.bedCapacity,

            address: careHome.address ?? '',

            phone: careHome.phone ?? '',

            email: careHome.email ?? '',

            managerName: careHome.managerName ?? '',

            managerPhone: careHome.managerPhone ?? '',

            managerEmail: careHome.managerEmail ?? '',

            bankDetails: careHome.bankDetails ?? '',

            isActive: careHome.isActive,
          });
        },

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care home.'));
        },
      });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    this.isSaving.set(true);

    this.errorMessage.set(null);

    const value = this.form.getRawValue();

    const request = {
      companyId: value.companyId > 0 ? value.companyId : null,

      code: value.code,

      name: value.name,

      bedCapacity: value.bedCapacity,

      address: value.address,

      phone: value.phone,

      email: optionalEmail(value.email),

      managerName: value.managerName,

      managerPhone: value.managerPhone,

      managerEmail: optionalEmail(value.managerEmail),

      bankDetails: value.bankDetails.trim() || null,
    };

    if (this.isEditMode && this.careHomeRouteKey !== null) {
      this.careHomeService
        .updateCareHome(this.careHomeRouteKey, {
          ...request,
          isActive: value.isActive,
        })
        .pipe(
          switchMap((careHome) => this.uploadLogo(entityRouteKey(careHome))),
          finalize(() => {
            this.isSaving.set(false);
          }),
        )
        .subscribe({
          next: () => {
            this.toast.success('Care home updated successfully.');
            void this.router.navigate(['/care-homes', this.careHomeRouteKey, 'dashboard']);
          },

          error: (error) => {
            logApiFailure(error);

            this.errorMessage.set(getApiErrorMessage(error, 'Unable to update care home.'));
          },
        });

      return;
    }

    this.careHomeService
      .createCareHome(request)
      .pipe(
        switchMap((careHome) =>
          this.uploadLogo(entityRouteKey(careHome)).pipe(map(() => careHome)),
        ),
        finalize(() => {
          this.isSaving.set(false);
        }),
      )
      .subscribe({
        next: (careHome) => {
          this.form.reset({
            companyId: 0,
            code: '',
            name: '',
            bedCapacity: 0,
            address: '',
            phone: '',
            email: '',
            managerName: '',
            managerPhone: '',
            managerEmail: '',
            bankDetails: '',
            isActive: true,
          });
          this.toast.success('Care home created successfully.');
          void this.router.navigate(['/care-homes', entityRouteKey(careHome), 'dashboard']);
        },

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to create care home.'));
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

  private uploadLogo(key: string): Observable<CareHomeLocation | void> {
    if (!this.logoFile) {
      return of(undefined);
    }
    return this.careHomeService.uploadLogo(key, this.logoFile);
  }

  private setLogoPreview(url: string): void {
    const current = this.logoPreviewUrl();
    if (current?.startsWith('blob:')) {
      URL.revokeObjectURL(current);
    }
    this.logoPreviewUrl.set(url);
  }
}
