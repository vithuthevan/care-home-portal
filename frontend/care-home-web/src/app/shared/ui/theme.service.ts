import { Injectable, signal } from '@angular/core';

export type AppThemeId = 'green' | 'blue' | 'teal' | 'purple' | 'slate';

export type AppColorMode = 'light' | 'dark';

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

const COLOR_MODE_STORAGE_KEY = 'carehome.colorMode';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  readonly activeTheme = signal<AppThemeId>('green');
  readonly activeColorMode = signal<AppColorMode>('light');

  init(): void {
    this.applyDefaultAccent();
    this.applyStoredColorMode();
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

  setColorMode(mode: AppColorMode): void {
    if (typeof document === 'undefined') {
      return;
    }
    document.documentElement.setAttribute('data-app-color-mode', mode);
    this.activeColorMode.set(mode);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(COLOR_MODE_STORAGE_KEY, mode);
    }
  }

  toggleColorMode(): void {
    this.setColorMode(this.activeColorMode() === 'dark' ? 'light' : 'dark');
  }

  private applyStoredColorMode(): void {
    let mode: AppColorMode = 'light';
    if (typeof localStorage !== 'undefined') {
      const stored = localStorage.getItem(COLOR_MODE_STORAGE_KEY);
      if (stored === 'dark' || stored === 'light') {
        mode = stored;
      }
    }
    this.setColorMode(mode);
  }
}
