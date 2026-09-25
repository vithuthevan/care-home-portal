import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { AuthService } from '../../core/auth.service';
import { getApiErrorMessage } from '../../core/api-error';
import { ApiErrorComponent } from '../../shared/ui/api-error';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, ApiErrorComponent],
  templateUrl: './forgot-password.html',
})
export class ForgotPasswordPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly errorMessage = signal<string | null>(null);
  readonly sentMessage = signal<string | null>(null);
  readonly isSaving = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.auth
      .forgotPassword(this.form.controls.email.value.trim())
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (result) => this.sentMessage.set(result.message),
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to send a reset link.')),
      });
  }
}
