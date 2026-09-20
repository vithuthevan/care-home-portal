import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../models/company.model';
import { PagedResult } from '../../../core/models';

@Injectable({
  providedIn: 'root',
})
export class CompanyService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = '/api/companies';

  getCompanies(): Observable<Company[]> {
    return this.http.get<Company[]>(this.apiUrl);
  }

  getCompaniesPaged(
    page: number,
    pageSize: number,
    search?: string,
  ): Observable<PagedResult<Company>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResult<Company>>(this.apiUrl, { params });
  }

  getCompany(id: number): Observable<Company> {
    return this.http.get<Company>(`${this.apiUrl}/${id}`);
  }

  createCompany(request: CreateCompanyRequest): Observable<Company> {
    return this.http.post<Company>(this.apiUrl, request);
  }

  updateCompany(id: number, request: UpdateCompanyRequest): Observable<Company> {
    return this.http.put<Company>(`${this.apiUrl}/${id}`, request);
  }

  deactivateCompany(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
