import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';

import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { CareHomeService } from '../../../care-homes/services/care-home.service';
import { CareHomeLocation } from '../../../care-homes/models/care-home.model';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { ToastService } from '../../../../shared/ui/toast.service';

@Component({
  selector: 'app-user-form',
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
  ],
  templateUrl: './user-form.html',
})
export class UserFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly homesApi = inject(CareHomeService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly homes = signal<CareHomeLocation[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly isSaving = signal(false);
  selectedHomes: number[] = [];

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    displayName: ['', Validators.required],
    password: ['', Validators.required],
    role: ['ReadOnly', Validators.required],
  });

  ngOnInit(): void {
    this.homesApi.getCareHomes().subscribe((x) => this.homes.set(x));
  }

  toggleHome(id: number, checked: boolean): void {
    if (checked) {
      this.selectedHomes.push(id);
    } else {
      this.selectedHomes = this.selectedHomes.filter((x) => x !== id);
    }
  }

  create(event?: Event): void {
    event?.preventDefault();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errorMessage.set(null);
    this.isSaving.set(true);
    const { email, displayName, password, role } = this.form.getRawValue();
    this.auth
      .encryptSecret(password)
      .pipe(
        switchMap((passwordCipher) =>
          this.http.post('/api/users', {
            email,
            displayName,
            role,
            careHomeIds: this.selectedHomes,
            passwordCipher,
          }),
        ),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          this.toast.success('User created successfully.');
          void this.router.navigate(['/users']);
        },
        error: (error) =>
          this.errorMessage.set(getApiErrorMessage(error, 'Unable to create user.')),
      });
  }
}
