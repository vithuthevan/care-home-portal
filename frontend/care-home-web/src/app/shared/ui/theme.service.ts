import { Injectable, signal } from '@angular/core';

export type AppThemeId = 'green' | 'blue' | 'teal' | 'purple' | 'slate';

export interface AppThemeOption {
  id: AppThemeId;
  label: string;
}

export const APP_THEME_OPTIONS: AppThemeOption[] = [
  { id: 'green', label: 'Green' },
  { id: 'blue', label: 'Blue' },
  { id: 'teal', label: 'Teal' },
  { id: 'purple', label: 'Purple' },
  { id: 'slate', label: 'Slate' },
];

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  readonly activeTheme = signal<AppThemeId>('green');

  init(): void {
    this.applyDefaultAccent();
  }

  applyDefaultAccent(): void {
    this.apply('green');
  }

  apply(themeId: AppThemeId): void {
    if (typeof document === 'undefined') {
      return;
    }
    if (!APP_THEME_OPTIONS.some((option) => option.id === themeId)) {
      themeId = 'green';
    }
    document.documentElement.setAttribute('data-app-theme', themeId);
    this.activeTheme.set(themeId);
  }
}
