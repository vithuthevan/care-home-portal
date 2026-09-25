export interface CareHomeLocation {
  id: number;
  publicId?: string;
  companyId: number;
  companyName: string;

  code: string;
  name: string;

  bedCapacity: number;

  address: string | null;
  phone: string | null;
  email: string | null;

  managerName: string | null;
  managerPhone: string | null;
  managerEmail: string | null;

  isActive: boolean;
  portalAccentTheme?: string | null;
}

export interface CreateCareHomeRequest {
  companyId: number;

  code: string;
  name: string;

  bedCapacity: number;

  address?: string;
  phone?: string;
  email?: string | null;

  managerName?: string;
  managerPhone?: string;
  managerEmail?: string | null;
}

export interface UpdateCareHomeRequest extends CreateCareHomeRequest {
  isActive: boolean;
}
