import { Injectable, signal } from '@angular/core';

export interface BreadcrumbItem {
  label: string;
  routerLink?: string;
}

@Injectable({
  providedIn: 'root',
})
export class BreadcrumbService {
  readonly items = signal<BreadcrumbItem[]>([]);

  set(items: BreadcrumbItem[]): void {
    this.items.set(items);
  }

  setFromUrl(url: string): void {
    this.items.set(this.crumbsForUrl(url));
  }

  private crumbsForUrl(url: string): BreadcrumbItem[] {
    const path = url.split('?')[0];
    if (path.startsWith('/clients/new')) {
      return [{ label: 'Clients', routerLink: '/clients' }, { label: 'New resident' }];
    }
    if (/^\/clients\/\d+\/edit/.test(path)) {
      return [{ label: 'Clients', routerLink: '/clients' }, { label: 'Edit resident' }];
    }
    if (/^\/clients\/\d+/.test(path)) {
      return [{ label: 'Clients', routerLink: '/clients' }, { label: 'Resident' }];
    }
    if (path.startsWith('/clients')) {
      return [{ label: 'Clients' }];
    }
    if (/^\/care-homes\/\d+\/dashboard/.test(path)) {
      return [{ label: 'Care Homes', routerLink: '/care-homes' }, { label: 'Care home' }];
    }
    if (path.startsWith('/care-homes/new')) {
      return [{ label: 'Care Homes', routerLink: '/care-homes' }, { label: 'New care home' }];
    }
    if (/^\/care-homes\/\d+\/edit/.test(path)) {
      return [{ label: 'Care Homes', routerLink: '/care-homes' }, { label: 'Edit care home' }];
    }
    if (path.startsWith('/care-homes')) {
      return [{ label: 'Care Homes' }];
    }
    if (path.startsWith('/companies/new')) {
      return [{ label: 'Companies', routerLink: '/companies' }, { label: 'New company' }];
    }
    if (/^\/companies\/\d+/.test(path)) {
      return [{ label: 'Companies', routerLink: '/companies' }, { label: 'Edit company' }];
    }
    if (path.startsWith('/companies')) {
      return [{ label: 'Companies' }];
    }
    if (path.startsWith('/billing')) {
      return [{ label: 'Billing', routerLink: '/billing' }];
    }
    if (/^\/invoices\/\d+/.test(path)) {
      return [
        { label: 'Billing', routerLink: '/billing' },
        { label: 'Invoices', routerLink: '/invoices' },
        { label: 'Invoice' },
      ];
    }
    if (path.startsWith('/invoices')) {
      return [{ label: 'Billing', routerLink: '/billing' }, { label: 'Invoices' }];
    }
    if (path.startsWith('/credit-notes')) {
      return [
        { label: 'Billing', routerLink: '/billing' },
        { label: 'Credit notes', routerLink: '/credit-notes' },
      ];
    }
    if (path.startsWith('/funding-authorities')) {
      return [{ label: 'Funding Authorities' }];
    }
    if (path.startsWith('/invoice-categories')) {
      return [{ label: 'Invoice Categories' }];
    }
    if (path.startsWith('/nominal-codes')) {
      return [{ label: 'Nominal Codes' }];
    }
    if (path.startsWith('/invoice-templates')) {
      return [{ label: 'Invoice Templates' }];
    }
    if (path.startsWith('/misc-charges')) {
      return [{ label: 'Miscellaneous Charges' }];
    }
    if (path.startsWith('/reports')) {
      return [{ label: 'Reports' }];
    }
    if (path.startsWith('/sage-exports')) {
      return [{ label: 'Sage Export' }];
    }
    if (path.startsWith('/users')) {
      return [{ label: 'Users' }];
    }
    if (path.startsWith('/audit')) {
      return [{ label: 'Audit' }];
    }
    if (path.startsWith('/settings')) {
      return [{ label: 'Organisation Settings' }];
    }
    if (path.startsWith('/platform')) {
      return [{ label: 'Organisations' }];
    }
    if (path.startsWith('/dashboard') || path === '/') {
      return [{ label: 'Dashboard' }];
    }
    if (path.startsWith('/forbidden')) {
      return [{ label: 'Access denied' }];
    }
    return [{ label: 'Page not found' }];
  }
}
