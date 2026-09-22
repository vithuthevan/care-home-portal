import { Component, input, Optional, Self } from '@angular/core';
import { ControlValueAccessor, FormsModule, NgControl } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

/** Parse yyyy-MM-dd to local Date (no timezone shift). */
export function parseIsoDateString(value: string | null | undefined): Date | null {
  if (!value?.trim()) {
    return null;
  }
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim());
  if (!match) {
    return null;
  }
  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  const date = new Date(year, month, day);
  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month ||
    date.getDate() !== day
  ) {
    return null;
  }
  return date;
}

/** Format local Date to yyyy-MM-dd. */
export function formatIsoDateString(date: Date | null): string {
  if (!date) {
    return '';
  }
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

@Component({
  selector: 'app-date-field',
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
  ],
  template: `
    <mat-form-field appearance="outline" class="w-full">
      <mat-label>{{ label() }}</mat-label>
      <input
        matInput
        [matDatepicker]="picker"
        [required]="required()"
        [disabled]="disabled"
        [(ngModel)]="pickerDate"
        [ngModelOptions]="{ standalone: true }"
        (ngModelChange)="onDateChange($event)"
        (blur)="onTouched()"
      />
      <mat-datepicker-toggle matIconSuffix [for]="picker" />
      <mat-datepicker #picker />
      @if (hint()) {
        <mat-hint>{{ hint() }}</mat-hint>
      }
      @if (errorText()) {
        <mat-error>{{ errorText() }}</mat-error>
      } @else if (showRequiredError()) {
        <mat-error>{{ requiredErrorMessage() }}</mat-error>
      }
    </mat-form-field>
  `,
})
export class AppDateFieldComponent implements ControlValueAccessor {
  readonly label = input.required<string>();
  readonly required = input(false);
  readonly requiredErrorMessage = input('This date is required.');
  readonly errorText = input<string | null>(null);
  readonly hint = input<string | null>(null);

  disabled = false;
  pickerDate: Date | null = null;
  private isoValue = '';

  private onChange: (value: string) => void = () => {};
  private onTouchedCallback: () => void = () => {};

  constructor(@Self() @Optional() private readonly ngControl: NgControl | null) {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  writeValue(value: string | null): void {
    this.isoValue = value ?? '';
    this.pickerDate = parseIsoDateString(this.isoValue);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedCallback = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onDateChange(date: Date | null | undefined): void {
    const normalized = date ?? null;
    this.pickerDate = normalized;
    this.isoValue = formatIsoDateString(normalized);
    this.onChange(this.isoValue);
    this.onTouchedCallback();
  }

  onTouched(): void {
    this.onTouchedCallback();
  }

  showRequiredError(): boolean {
    const control = this.ngControl?.control;
    return !!(
      control &&
      control.touched &&
      control.hasError('required')
    );
  }
}
