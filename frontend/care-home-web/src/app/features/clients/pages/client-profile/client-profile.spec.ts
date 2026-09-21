import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { Client } from '../../models/client.model';
import { ClientProfilePage } from './client-profile';

describe('ClientProfilePage', () => {
  let fixture: ComponentFixture<ClientProfilePage>;
  let http: HttpTestingController;

  const client: Client = {
    id: 1,
    careHomeId: 1,
    careHomeName: 'Green Valley',
    companyId: 1,
    companyName: 'Green Valley Ltd',
    sageId: 'SAGE001',
    referenceNumber: 'CLIENT001',
    title: 'Ms',
    firstName: 'Alice',
    lastName: 'Brown',
    dateOfBirth: '1940-01-01',
    careType: 'Residential',
    status: 'Current',
    admissionDate: '2020-01-01',
    dischargeDate: null,
    dischargeReason: null,
    email: 'alice@example.com',
    phone: '01234',
    notes: null,
    isArchived: false,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientProfilePage],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: '1' })) },
        },
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ClientProfilePage);
    fixture.detectChanges();

    http.expectOne('/api/funding-authorities?activeOnly=true').flush([]);
    http.expectOne('/api/invoice-categories?activeOnly=true').flush([]);
    http.expectOne('/api/nominal-codes?activeOnly=true').flush([]);
    http.expectOne('/api/clients/1').flush(client);
    http.expectOne('/api/clients/1/funding-contracts').flush([]);
    http.expectOne((req) => req.url === '/api/invoices').flush({ items: [] });
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should render profile hero, funding summary, and workflow tabs', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('mat-tab-group')).toBeTruthy();
    expect(el.textContent).toContain('Alice Brown');
    expect(el.textContent).toContain('Resident details');
    expect(el.textContent).toContain('Funding');
    expect(el.textContent).toContain('Billing');
    expect(el.textContent).toContain('Invoices');
    expect(el.textContent).toContain('Funding summary');
    expect(el.textContent).toContain('Identity & placement');
    expect(el.textContent).toContain('Green Valley');
  });
});
