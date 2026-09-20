import { Pipe, PipeTransform } from '@angular/core';

/** Human-friendly UK-style dates for display (keeps form controls on raw ISO). */
@Pipe({ name: 'displayDate', standalone: true })
export class DisplayDatePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '—';
    }
    const iso = value.length >= 10 ? value.slice(0, 10) : value;
    const parts = iso.split('-').map((p) => Number(p));
    if (parts.length !== 3 || parts.some((n) => !Number.isFinite(n))) {
      return value;
    }
    const [year, month, day] = parts;
    const date = new Date(year, month - 1, day);
    if (Number.isNaN(date.getTime())) {
      return value;
    }
    return date.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
