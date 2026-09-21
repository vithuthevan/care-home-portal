import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  template: `<span [class]="classes()">{{ display() }}</span>`,
})
export class StatusBadgeComponent {
  readonly value = input.required<string>();
  readonly label = input<string>();

  readonly display = computed(() => this.label() || this.humanize(this.value()));
  readonly classes = computed(() => `badge ${this.tone(this.value())}`);

  private humanize(value: string): string {
    if (!value) {
      return 'Unknown';
    }
    if (value === 'NotPaid') {
      return 'Not Paid';
    }
    return value.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private tone(value: string): string {
    const key = (value || '').toLowerCase();
    if (
      [
        'active',
        'current',
        'generated',
        'paid',
        'received',
        'success',
        'successful',
        'valid',
      ].includes(key)
    ) {
      return 'badge-success';
    }
    if (['sent', 'simulated', 'draft', 'info'].includes(key)) {
      return 'badge-info';
    }
    if (['left', 'due', 'warning', 'pending', 'not paid', 'notpaid'].includes(key)) {
      return 'badge-warning';
    }
    if (
      ['void', 'deceased', 'failed', 'inactive', 'invalid', 'cancelled', 'canceled'].includes(key)
    ) {
      return 'badge-danger';
    }
    return 'badge-neutral';
  }
}
