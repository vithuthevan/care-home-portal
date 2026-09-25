import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AuthService } from '../../core/auth.service';
import { getApiErrorMessage } from '../../core/api-error';
import { ApiErrorComponent } from '../../shared/ui/api-error';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    ApiErrorComponent,
  ],
  templateUrl: './login.html',
})
export class LoginPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  readonly isSaving = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit(event?: Event): void {
    event?.preventDefault();

    if (this.isSaving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.beginAttempt();
    const { email, password } = this.form.getRawValue();

    this.auth
      .login(email, password)
      .pipe(finalize(() => this.endAttempt()))
      .subscribe({
        next: () => {
          void this.router.navigate(this.auth.homePath());
        },
        error: (error) => {
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to sign in.'));
        },
      });
  }

  private beginAttempt(): void {
    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.form.disable({ emitEvent: false });
  }

  private endAttempt(): void {
    this.isSaving.set(false);
    this.form.enable({ emitEvent: false });
  }
}
