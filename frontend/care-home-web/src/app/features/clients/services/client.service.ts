import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { PagedResult } from '../../../core/models';
import { Client, CreateClientRequest, UpdateClientRequest } from '../models/client.model';

@Injectable({
  providedIn: 'root',
})
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/clients';

  getClients(
    search?: string,
    careHomeId?: number,
    includeArchived = false,
    page = 1,
    pageSize = 50,
    extra?: {
      companyId?: number;
      status?: string;
      fundingAuthorityId?: number;
      contractStatus?: string;
    },
  ): Observable<PagedResult<Client>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('includeArchived', includeArchived ? 'true' : 'false');

    if (search) {
      params = params.set('search', search);
    }
    if (careHomeId) {
      params = params.set('careHomeId', careHomeId);
    }
    if (extra?.companyId) {
      params = params.set('companyId', extra.companyId);
    }
    if (extra?.status) {
      params = params.set('status', extra.status);
    }
    if (extra?.fundingAuthorityId) {
      params = params.set('fundingAuthorityId', extra.fundingAuthorityId);
    }
    if (extra?.contractStatus) {
      params = params.set('contractStatus', extra.contractStatus);
    }

    return this.http.get<PagedResult<Client>>(this.apiUrl, { params });
  }

  getClient(id: number | string): Observable<Client> {
    return this.http.get<Client>(`${this.apiUrl}/${id}`);
  }

  createClient(request: CreateClientRequest): Observable<Client> {
    return this.http.post<Client>(this.apiUrl, request);
  }

  updateClient(id: number | string, request: UpdateClientRequest): Observable<Client> {
    return this.http.put<Client>(`${this.apiUrl}/${id}`, request);
  }

  archiveClient(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
