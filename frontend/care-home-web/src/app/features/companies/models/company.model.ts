export interface Company {
  id: number;
  publicId?: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  logoPath?: string | null;
  isActive: boolean;
  careHomeCount?: number;
  activeCareHomeCount?: number;
  residentCount?: number;
}

export interface CreateCompanyRequest {
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
}

export interface UpdateCompanyRequest {
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive: boolean;
}
