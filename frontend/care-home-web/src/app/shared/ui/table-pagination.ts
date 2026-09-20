import { Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-table-pagination',
  imports: [MatButtonModule, MatFormFieldModule, MatSelectModule],
  template: `
    @if (totalCount() > 0) {
      <nav class="table-pagination" aria-label="Table pagination">
        <p class="table-pagination__summary">
          Showing {{ rangeStart() }}–{{ rangeEnd() }} of {{ totalCount() }}
        </p>
        <div class="table-pagination__controls">
          <mat-form-field appearance="outline" class="table-pagination__size">
            <mat-label>Page size</mat-label>
            <mat-select
              [value]="pageSize()"
              (selectionChange)="pageSizeChange.emit($event.value)"
              aria-label="Rows per page"
            >
              @for (size of pageSizeOptions(); track size) {
                <mat-option [value]="size">{{ size }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <button
            mat-stroked-button
            type="button"
            (click)="pageChange.emit(page() - 1)"
            [disabled]="page() <= 1 || disabled()"
            aria-label="Previous page"
          >
            Previous
          </button>
          <span class="table-pagination__page" aria-live="polite">Page {{ page() }} of {{ totalPages() }}</span>
          <button
            mat-stroked-button
            type="button"
            (click)="pageChange.emit(page() + 1)"
            [disabled]="page() >= totalPages() || disabled()"
            aria-label="Next page"
          >
            Next
          </button>
        </div>
      </nav>
    }
  `,
  styles: `
    .table-pagination {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      margin-top: var(--space-4);
      padding: var(--space-3) var(--space-4);
      background: var(--app-surface);
      border: 1px solid var(--app-border);
      border-radius: var(--app-radius-lg);
    }

    .table-pagination__summary {
      margin: 0;
      font-size: 0.875rem;
      color: var(--app-text-muted);
    }

    .table-pagination__controls {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--space-2);
    }

    .table-pagination__page {
      font-size: 0.875rem;
      color: var(--app-text);
      min-width: 6.5rem;
      text-align: center;
    }

    .table-pagination__size {
      width: 7.5rem;
      margin: 0;
    }

    .table-pagination__size ::ng-deep .mat-mdc-form-field-subscript-wrapper {
      display: none;
    }
  `,
})
export class TablePaginationComponent {
  readonly page = input(1);
  readonly pageSize = input(50);
  readonly totalCount = input(0);
  readonly disabled = input(false);
  readonly pageSizeOptions = input([25, 50, 100]);

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / Math.max(1, this.pageSize()))),
  );

  readonly rangeStart = computed(() => {
    if (this.totalCount() === 0) {
      return 0;
    }
    return (this.page() - 1) * this.pageSize() + 1;
  });

  readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount()),
  );
}
