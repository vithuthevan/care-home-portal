export interface Company {
  id: number;
  name: string;
  isActive: boolean;
  careHomeCount?: number;
  activeCareHomeCount?: number;
  residentCount?: number;
}

export interface CreateCompanyRequest {
  name: string;
}

export interface UpdateCompanyRequest {
  name: string;
  isActive: boolean;
}
