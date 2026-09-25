import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { AuthService } from '../../core/auth.service';
import { getApiErrorMessage } from '../../core/api-error';
import { ApiErrorComponent } from '../../shared/ui/api-error';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, ApiErrorComponent],
  templateUrl: './reset-password.html',
})
export class ResetPasswordPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  readonly isSaving = signal(false);
  email = '';
  token = '';

  readonly form = this.formBuilder.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.email || !this.token) {
      this.errorMessage.set('This reset link is invalid or has expired.');
    }
  }

  submit(): void {
    if (this.form.invalid || !this.email || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.auth
      .resetPassword(this.email, this.token, this.form.controls.password.value)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => void this.router.navigate(['/login']),
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'This reset link is invalid or has expired.')),
      });
  }
}
