import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { LoadingStateComponent } from '../../../../shared/ui/loading-state';
import { KpiCardComponent } from '../../../../shared/ui/kpi-card';
import { ImportExportToolbarComponent } from '../../../../shared/ui/import-export-toolbar';
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

interface CollectionsDashboard {
  dueThisWeek: number;
  overdue: number;
  overdue30: number;
  overdue60: number;
  overdue90: number;
}

interface CollectionReminderRunResult {
  succeeded: number;
  failed: number;
  skipped: number;
  remindersDisabled: boolean;
}

@Component({
  selector: 'app-collections-workspace',
  imports: [
    DecimalPipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    PageHeaderComponent,
    ApiErrorComponent,
    LoadingStateComponent,
    KpiCardComponent,
    ImportExportToolbarComponent,
  ],
  templateUrl: './collections-workspace.html',
})
export class CollectionsWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  dashboard = signal<CollectionsDashboard | null>(null);
  policy: CollectionPolicyForm = this.defaultPolicy();
  loadErrorMessage = signal<string | null>(null);
  isLoading = signal(false);
  isSavingPolicy = signal(false);
  isSendingReminders = signal(false);
  reminderResult = signal<CollectionReminderRunResult | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.loadErrorMessage.set(null);
    this.http
      .get<CollectionsDashboard & { policy: CollectionPolicyForm }>('/api/collections/dashboard')
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (d) => {
          this.dashboard.set({
            dueThisWeek: d.dueThisWeek ?? 0,
            overdue: d.overdue ?? 0,
            overdue30: d.overdue30 ?? 0,
            overdue60: d.overdue60 ?? 0,
            overdue90: d.overdue90 ?? 0,
          });
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
          this.loadErrorMessage.set(
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
          this.toast.error(getApiErrorMessage(err, 'Unable to save collection policy.')),
      });
  }

  sendReminders(): void {
    this.isSendingReminders.set(true);
    this.http
      .post<CollectionReminderRunResult>('/api/collections/send-reminders', {})
      .pipe(finalize(() => this.isSendingReminders.set(false)))
      .subscribe({
        next: (result) => {
          this.reminderResult.set(result);
          if (result.remindersDisabled) {
            this.toast.error(
              'Collection reminders are disabled for this organisation. Enable them in the policy below and save.',
            );
            return;
          }

          this.toast.success(
            `Reminders: ${result.succeeded} sent, ${result.failed} failed, ${result.skipped} skipped.`,
          );
        },
        error: (err) =>
          this.toast.error(getApiErrorMessage(err, 'Unable to send collection reminders.')),
      });
  }

  policyReadOnly(): boolean {
    return !this.auth.canManageOrganisation();
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
