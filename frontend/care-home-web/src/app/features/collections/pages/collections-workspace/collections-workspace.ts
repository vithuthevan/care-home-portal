import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state';
import { getApiErrorMessage } from '../../../../core/api-error';
import { AuthService } from '../../../../core/auth.service';
import { ToastService } from '../../../../shared/ui/toast.service';

interface CollectionPolicyForm {
  dueReminderDaysBefore: number;
  overdue7Days: number;
  overdue14Days: number;
  overdue30Days: number;
  escalationDays: number;
  remindersEnabled: boolean;
  reminderEmailSubjectTemplate: string;
  reminderEmailBodyTemplate: string;
}

@Component({
  selector: 'app-collections-workspace',
  imports: [
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    EmptyStateComponent,
  ],
  templateUrl: './collections-workspace.html',
})
export class CollectionsWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  dashboard = signal<{ overdue: number; overdue90: number } | null>(null);
  policy: CollectionPolicyForm = this.defaultPolicy();
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  isSavingPolicy = signal(false);
  isSendingReminders = signal(false);
  reminderResult = signal<{ succeeded: number; failed: number; skipped: number } | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.http
      .get<{
        overdue: number;
        overdue90: number;
        policy: CollectionPolicyForm;
      }>('/api/collections/dashboard')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (d) => {
          this.dashboard.set({ overdue: d.overdue, overdue90: d.overdue90 });
          this.policy = {
            dueReminderDaysBefore: d.policy.dueReminderDaysBefore ?? 0,
            overdue7Days: d.policy.overdue7Days ?? 7,
            overdue14Days: d.policy.overdue14Days ?? 14,
            overdue30Days: d.policy.overdue30Days ?? 30,
            escalationDays: d.policy.escalationDays ?? 60,
            remindersEnabled: d.policy.remindersEnabled ?? false,
            reminderEmailSubjectTemplate:
              d.policy.reminderEmailSubjectTemplate ??
              'Payment reminder: invoice {{InvoiceNumber}}',
            reminderEmailBodyTemplate:
              d.policy.reminderEmailBodyTemplate ??
              'Invoice {{InvoiceNumber}} was due on {{DueDate}}. Outstanding: {{OutstandingAmount}}.',
          };
        },
        error: (err) =>
          this.errorMessage.set(
            getApiErrorMessage(err, "We couldn't retrieve this information right now. Please try again."),
          ),
      });
  }

  savePolicy(): void {
    this.isSavingPolicy.set(true);
    this.http
      .put('/api/collections/policy', this.policy)
      .pipe(finalize(() => this.isSavingPolicy.set(false)))
      .subscribe({
        next: () => this.toast.success('Collection policy saved.'),
        error: (err) =>
          this.errorMessage.set(getApiErrorMessage(err, 'Unable to save collection policy.')),
      });
  }

  sendReminders(): void {
    this.isSendingReminders.set(true);
    this.http
      .post<{ succeeded: number; failed: number; skipped: number }>('/api/collections/send-reminders', {})
      .pipe(finalize(() => this.isSendingReminders.set(false)))
      .subscribe({
        next: (result) => {
          this.reminderResult.set(result);
          this.toast.success(
            `Reminders: ${result.succeeded} sent, ${result.failed} failed, ${result.skipped} skipped.`,
          );
        },
        error: (err) =>
          this.errorMessage.set(getApiErrorMessage(err, 'Unable to send collection reminders.')),
      });
  }

  private defaultPolicy(): CollectionPolicyForm {
    return {
      dueReminderDaysBefore: 0,
      overdue7Days: 7,
      overdue14Days: 14,
      overdue30Days: 30,
      escalationDays: 60,
      remindersEnabled: false,
      reminderEmailSubjectTemplate: 'Payment reminder: invoice {{InvoiceNumber}}',
      reminderEmailBodyTemplate:
        'Invoice {{InvoiceNumber}} was due on {{DueDate}}. Outstanding: {{OutstandingAmount}}.',
    };
  }
}
