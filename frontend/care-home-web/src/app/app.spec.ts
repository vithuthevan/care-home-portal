import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { AuthService } from './core/auth.service';
import { App } from './app';

const authMock = {
  isLoggedIn: () => true,
  mustChangePassword: () => false,
  currentUser: signal({
    displayName: 'Test User',
    tenantName: 'Demo Tenant',
    tenantPublicId: 'tenant-1',
    token: 'test',
    roles: [] as string[],
    mustChangePassword: false,
  }),
  isPlatformAdmin: () => false,
  canManageUsers: () => false,
  canManageOrganisation: () => false,
  financeModuleEnabled: () => false,
  logout: () => undefined,
  refreshProfile: () => of(void 0),
} as unknown as AuthService;

describe('App', () => {
  beforeEach(async () => {
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches: false,
        media: query,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      }),
    });
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application shell', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand')?.textContent).toContain('Care Home');
  });
});
