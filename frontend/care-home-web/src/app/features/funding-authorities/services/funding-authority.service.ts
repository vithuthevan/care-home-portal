import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CreateFundingAuthorityRequest,
  FundingAuthority,
  UpdateFundingAuthorityRequest,
} from '../models/funding-authority.model';
import { PagedResult } from '../../../core/models';

@Injectable({
  providedIn: 'root',
})
export class FundingAuthorityService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = '/api/funding-authorities';

  getFundingAuthorities(): Observable<FundingAuthority[]> {
    return this.http.get<FundingAuthority[]>(this.apiUrl);
  }

  getFundingAuthoritiesPaged(page: number, pageSize: number): Observable<PagedResult<FundingAuthority>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<FundingAuthority>>(this.apiUrl, { params });
  }

  getFundingAuthority(id: number): Observable<FundingAuthority> {
    return this.http.get<FundingAuthority>(`${this.apiUrl}/${id}`);
  }

  createFundingAuthority(request: CreateFundingAuthorityRequest): Observable<FundingAuthority> {
    return this.http.post<FundingAuthority>(this.apiUrl, request);
  }

  updateFundingAuthority(
    id: number,
    request: UpdateFundingAuthorityRequest,
  ): Observable<FundingAuthority> {
    return this.http.put<FundingAuthority>(`${this.apiUrl}/${id}`, request);
  }

  deactivateFundingAuthority(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
