export interface Client {
  id: number;
  publicId?: string;

  careHomeId: number;
  careHomePublicId?: string;
  careHomeName: string;
  companyId: number;
  companyPublicId?: string;
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

  email?: string;
  phone?: string;

  notes?: string;
}

export interface UpdateClientRequest extends CreateClientRequest {
  status: string;

  dischargeDate?: string | null;

  dischargeReason?: string | null;

  isArchived: boolean;
}
