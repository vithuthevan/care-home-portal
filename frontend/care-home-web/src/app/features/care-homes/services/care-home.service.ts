import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CareHomeLocation,
  CreateCareHomeRequest,
  UpdateCareHomeRequest,
} from '../models/care-home.model';
import { PagedResult } from '../../../core/models';

@Injectable({
  providedIn: 'root',
})
export class CareHomeService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = '/api/care-homes';

  getCareHomes(): Observable<CareHomeLocation[]> {
    return this.http.get<CareHomeLocation[]>(this.apiUrl);
  }

  getCareHomesPaged(
    page: number,
    pageSize: number,
    companyId?: number,
  ): Observable<PagedResult<CareHomeLocation>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (companyId) {
      params = params.set('companyId', companyId);
    }
    return this.http.get<PagedResult<CareHomeLocation>>(this.apiUrl, { params });
  }

  getCareHome(id: number | string): Observable<CareHomeLocation> {
    return this.http.get<CareHomeLocation>(`${this.apiUrl}/${id}`);
  }

  updatePortalAppearance(
    id: number | string,
    portalAccentTheme: string,
  ): Observable<CareHomeLocation> {
    return this.http.put<CareHomeLocation>(`${this.apiUrl}/${id}/portal-appearance`, {
      portalAccentTheme,
    });
  }

  createCareHome(request: CreateCareHomeRequest): Observable<CareHomeLocation> {
    return this.http.post<CareHomeLocation>(this.apiUrl, request);
  }

  updateCareHome(id: number | string, request: UpdateCareHomeRequest): Observable<CareHomeLocation> {
    return this.http.put<CareHomeLocation>(`${this.apiUrl}/${id}`, request);
  }

  deactivateCareHome(id: number | string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getLogo(id: number | string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/logo`, { responseType: 'blob' });
  }

  uploadLogo(id: number | string, file: File): Observable<CareHomeLocation> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<CareHomeLocation>(`${this.apiUrl}/${id}/logo`, body);
  }
}
