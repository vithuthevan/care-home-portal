import { Component, computed, input } from '@angular/core';

import { configurationSourceLabel } from '../format/master-data-usage';

@Component({
  selector: 'app-configuration-source-badge',
  template: `<span class="config-source-badge">{{ text() }}</span>`,
})
export class ConfigurationSourceBadgeComponent {
  readonly source = input<string | null | undefined>('Organisation');

  readonly text = computed(() => configurationSourceLabel(this.source()));
}
