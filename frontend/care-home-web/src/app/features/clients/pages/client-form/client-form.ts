import { Component, inject, OnInit, signal } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { finalize } from 'rxjs';

import { CareHomeLocation } from '../../../care-homes/models/care-home.model';

import { CareHomeService } from '../../../care-homes/services/care-home.service';

import { ClientService } from '../../services/client.service';

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
import { ToastService } from '../../../../shared/ui/toast.service';
import { AppDateFieldComponent } from '../../../../shared/ui/app-date-field';

@Component({
  selector: 'app-client-form',

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
    AppDateFieldComponent,
  ],

  templateUrl: './client-form.html',
})
export class ClientForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  private readonly clientService = inject(ClientService);

  private readonly careHomeService = inject(CareHomeService);

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  clientRouteKey: string | null = null;

  readonly careHomes = signal<CareHomeLocation[]>([]);

  readonly assignedCareHomeId = signal<number | null>(null);

  isEditMode = false;

  readonly isLoading = signal(false);

  readonly isSaving = signal(false);

  readonly errorMessage = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    careHomeId: [0, [Validators.required, Validators.min(1)]],

    sageId: ['', [Validators.required, Validators.maxLength(20)]],

    referenceNumber: ['', [Validators.required, Validators.maxLength(20)]],

    title: ['', Validators.maxLength(10)],

    firstName: ['', [Validators.required, Validators.maxLength(100)]],

    lastName: ['', [Validators.required, Validators.maxLength(100)]],

    dateOfBirth: [''],

    careType: ['', Validators.required],

    status: ['Current'],

    admissionDate: ['', Validators.required],

    dischargeDate: [''],

    dischargeReason: ['', Validators.maxLength(100)],

    email: ['', [Validators.email, Validators.maxLength(150)]],

    phone: ['', Validators.maxLength(30)],

    notes: ['', Validators.maxLength(1000)],

    isArchived: [false],
  });

  get selectableCareHomes(): CareHomeLocation[] {
    return this.careHomes().filter(
      (careHome) =>
        careHome.isActive || (this.isEditMode && careHome.id === this.assignedCareHomeId()),
    );
  }

  get isCurrentStatus(): boolean {
    return this.form.controls.status.value === 'Current';
  }

  ngOnInit(): void {
    this.loadCareHomes();

    this.form.controls.status.valueChanges.subscribe((status) => {
      if (status === 'Current') {
        this.form.patchValue(
          {
            dischargeDate: '',
            dischargeReason: '',
            isArchived: false,
          },
          { emitEvent: false },
        );

        this.form.controls.dischargeDate.clearValidators();
      } else {
        this.form.controls.dischargeDate.setValidators(Validators.required);
      }

      this.form.controls.dischargeDate.updateValueAndValidity({
        emitEvent: false,
      });
    });

    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.clientRouteKey = id;

      this.isEditMode = true;

      this.loadClient();
    } else {
      this.form.controls.sageId.clearValidators();
      this.form.controls.sageId.setValidators([Validators.maxLength(20)]);
      this.form.controls.referenceNumber.clearValidators();
      this.form.controls.referenceNumber.setValidators([Validators.maxLength(20)]);
      this.form.controls.sageId.updateValueAndValidity();
      this.form.controls.referenceNumber.updateValueAndValidity();
    }
  }

  private loadCareHomes(): void {
    this.careHomeService.getCareHomes().subscribe({
      next: (careHomes) => {
        this.careHomes.set(careHomes);
      },

      error: (error) => {
        logApiFailure(error);

        this.errorMessage.set(getApiErrorMessage(error, 'Unable to load care homes.'));
      },
    });
  }

  private loadClient(): void {
    if (this.clientRouteKey === null) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.clientService
      .getClient(this.clientRouteKey)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: (client) => {
          this.assignedCareHomeId.set(client.careHomeId);

          this.form.patchValue({
            careHomeId: client.careHomeId,

            sageId: client.sageId,

            referenceNumber: client.referenceNumber,

            title: client.title ?? '',

            firstName: client.firstName,

            lastName: client.lastName,

            dateOfBirth: client.dateOfBirth ?? '',

            careType: client.careType,

            status: client.status,

            admissionDate: client.admissionDate,

            dischargeDate: client.dischargeDate ?? '',

            dischargeReason: client.dischargeReason ?? '',

            email: client.email ?? '',

            phone: client.phone ?? '',

            notes: client.notes ?? '',

            isArchived: client.isArchived,
          });
        },

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load client.'));
        },
      });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    this.errorMessage.set(null);

    const value = this.form.getRawValue();

    if (this.isEditMode && value.status !== 'Current' && !value.dischargeDate) {
      this.form.markAllAsTouched();

      this.errorMessage.set('Discharge date is required when client is no longer current.');

      return;
    }

    this.isSaving.set(true);

    const sageId = value.sageId.trim();
    const referenceNumber = value.referenceNumber.trim();

    const baseRequest = {
      careHomeId: value.careHomeId,

      sageId: sageId || undefined,

      referenceNumber: referenceNumber || undefined,

      title: value.title,

      firstName: value.firstName,

      lastName: value.lastName,

      dateOfBirth: value.dateOfBirth || null,

      careType: value.careType,

      admissionDate: value.admissionDate,

      email: value.email,

      phone: value.phone,

      notes: value.notes,
    };

    if (this.isEditMode && this.clientRouteKey !== null) {
      this.clientService
        .updateClient(this.clientRouteKey, {
          ...baseRequest,

          status: value.status,

          dischargeDate: value.status === 'Current' ? null : value.dischargeDate || null,

          dischargeReason: value.status === 'Current' ? null : value.dischargeReason,

          isArchived: value.status === 'Current' ? false : value.isArchived,
        })
        .pipe(
          finalize(() => {
            this.isSaving.set(false);
          }),
        )
        .subscribe({
          next: () => {
            this.toast.success('Client updated successfully.');
            this.router.navigate(['/clients']);
          },

          error: (error) => {
            logApiFailure(error);

            this.errorMessage.set(getApiErrorMessage(error, 'Unable to update client.'));
          },
        });

      return;
    }

    this.clientService
      .createClient(baseRequest)
      .pipe(
        finalize(() => {
          this.isSaving.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.toast.success('Resident created successfully.');
          void this.router.navigate(['/clients']);
        },

        error: (error) => {
          logApiFailure(error);

          this.errorMessage.set(getApiErrorMessage(error, 'Unable to create client.'));
        },
      });
  }
}
