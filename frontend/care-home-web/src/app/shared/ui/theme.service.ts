import { Injectable, signal } from '@angular/core';

export type AppThemeId = 'green' | 'blue' | 'teal' | 'purple' | 'slate';

export interface AppThemeOption {
  id: AppThemeId;
  label: string;
}

const STORAGE_KEY = 'care-home-ui-theme';

export const APP_THEME_OPTIONS: AppThemeOption[] = [
  { id: 'green', label: 'Green (default)' },
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
    if (typeof document === 'undefined') {
      return;
    }
    const stored = localStorage.getItem(STORAGE_KEY) as AppThemeId | null;
    const theme =
      stored && APP_THEME_OPTIONS.some((option) => option.id === stored) ? stored : 'green';
    this.apply(theme);
  }

  apply(themeId: AppThemeId): void {
    document.documentElement.setAttribute('data-app-theme', themeId);
    localStorage.setItem(STORAGE_KEY, themeId);
    this.activeTheme.set(themeId);
  }
}
