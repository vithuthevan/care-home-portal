import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';

import { getApiErrorMessage } from '../../../../core/api-error';
import { optionalEmail } from '../../../../shared/format/optional-email';
import { AuthService } from '../../../../core/auth.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { ToastService } from '../../../../shared/ui/toast.service';

@Component({
  selector: 'app-organisation-settings',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
  ],
  templateUrl: './organisation-settings.html',
})
export class OrganisationSettingsPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly errorMessage = signal<string | null>(null);
  readonly savedMessage = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    tradingName: [''],
    registrationNumber: [''],
    address: [''],
    phone: [''],
    email: ['', Validators.email],
    website: [''],
    currencyCode: ['GBP', Validators.required],
    currencySymbol: ['£', Validators.required],
    timeZoneId: ['Europe/London', Validators.required],
    invoicePrefix: ['INV-'],
    creditNotePrefix: ['CN-'],
    numberLength: [4, Validators.required],
    paymentTermsDays: [30, Validators.required],
    emailFromName: [''],
    emailFromAddress: ['', Validators.email],
    primaryColour: [''],
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.http
      .get<typeof this.form.value>('/api/settings/organisation')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (settings) => this.form.patchValue(settings),
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to load organisation settings.')),
      });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.savedMessage.set(null);
    const raw = this.form.getRawValue();
    this.http
      .put('/api/settings/organisation', {
        ...raw,
        email: optionalEmail(raw.email),
        emailFromAddress: optionalEmail(raw.emailFromAddress),
      })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.savedMessage.set('Organisation settings saved.');
          this.toast.success('Organisation updated successfully.');
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to save organisation settings.'));
        },
      });
  }
}
