export interface Client {
  id: number;
  publicId?: string;

  careHomeId: number;
  careHomePublicId?: string;
  careHomeName: string;
  companyId: number | null;
  companyPublicId?: string | null;
  companyName: string;

  sageId: string;
  referenceNumber: string;

  title: string | null;

  firstName: string;
  lastName: string;

  dateOfBirth: string | null;

  careType: string;
  status: string;

  admissionDate: string;

  dischargeDate: string | null;
  dischargeReason: string | null;

  email: string | null;
  phone: string | null;

  notes: string | null;

  isArchived: boolean;

  primaryFundingAuthorityName?: string | null;
  guardianName?: string | null;
  guardianRelationship?: string | null;
  guardianEmail?: string | null;
  guardianPhone?: string | null;
  guardianAddress?: string | null;
}

export interface CreateClientRequest {
  careHomeId: number;

  sageId?: string;
  referenceNumber?: string;

  title?: string;

  firstName: string;
  lastName: string;

  dateOfBirth?: string | null;

  careType: string;

  admissionDate: string;

  email?: string | null;
  phone?: string;

  notes?: string;
}

export interface UpdateClientRequest extends CreateClientRequest {
  status: string;

  dischargeDate?: string | null;

  dischargeReason?: string | null;

  isArchived: boolean;
}
