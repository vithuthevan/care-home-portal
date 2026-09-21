import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent } from '../../../../shared/ui/page-header';
import { ApiErrorComponent } from '../../../../shared/ui/api-error';
import { getApiErrorMessage } from '../../../../core/api-error';

interface Finding {
  publicId: string;
  ruleCode: string;
  severity: string;
  status: string;
  estimatedImpact?: number;
  explanation: string;
}

@Component({
  selector: 'app-revenue-assurance-workspace',
  imports: [DecimalPipe, MatButtonModule, PageHeaderComponent, ApiErrorComponent],
  template: `
    <app-page-header title="Revenue assurance" subtitle="Deterministic leakage and billing integrity findings" />
    @if (errorMessage()) {
      <app-api-error [message]="errorMessage()!" />
    }
    <section class="panel panel--flat mt-4">
      <button mat-flat-button color="primary" type="button" (click)="runScan()">Run scan</button>
      <p class="mt-2">Open findings: {{ dashboardOpen() }}</p>
    </section>
    <div class="table-wrap mt-4">
      <table class="data-table">
        <thead>
          <tr>
            <th>Rule</th>
            <th>Severity</th>
            <th>Impact</th>
            <th>Explanation</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (f of findings(); track f.publicId) {
            <tr>
              <td>{{ f.ruleCode }}</td>
              <td>{{ f.severity }}</td>
              <td>{{ f.estimatedImpact != null ? '£' + (f.estimatedImpact | number: '1.2-2') : '—' }}</td>
              <td>{{ f.explanation }}</td>
              <td>
                @if (f.status === 'Open') {
                  <button mat-button type="button" (click)="resolve(f.publicId)">Resolve</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class RevenueAssuranceWorkspacePage implements OnInit {
  private readonly http = inject(HttpClient);
  findings = signal<Finding[]>([]);
  dashboardOpen = signal(0);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.http.get<{ openFindings: number }>('/api/revenue-assurance/dashboard').subscribe({
      next: (d) => this.dashboardOpen.set(d.openFindings),
    });
    this.http.get<Finding[]>('/api/revenue-assurance/findings?status=Open').subscribe({
      next: (rows) => this.findings.set(rows),
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Failed to load findings.')),
    });
  }

  runScan(): void {
    this.http.post('/api/revenue-assurance/scan', {}).subscribe({
      next: () => this.refresh(),
      error: (err) => this.errorMessage.set(getApiErrorMessage(err, 'Scan failed.')),
    });
  }

  resolve(publicId: string): void {
    this.http.post(`/api/revenue-assurance/findings/${publicId}/resolve`, { status: 'Resolved' }).subscribe({
      next: () => this.refresh(),
    });
  }
}
