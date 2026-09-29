import { MasterDataUsage } from '../../../shared/format/master-data-usage';

export interface FundingAuthority {
  id: number;
  publicId?: string;
  code: string;
  name: string;
  type: string;

  contactName: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;

  billingFrequency: string;
  billingIntervalDays: number | null;
  cycleAnchorDate?: string | null;

  isActive: boolean;
  configurationSource?: string;
  usage?: MasterDataUsage | null;
}

export interface CreateFundingAuthorityRequest {
  code: string;
  name: string;
  type: string;

  contactName?: string;
  phone?: string;
  email?: string | null;
  address?: string;

  billingFrequency: string;
  billingIntervalDays?: number | null;
  cycleAnchorDate?: string | null;
}

export interface UpdateFundingAuthorityRequest extends CreateFundingAuthorityRequest {
  isActive: boolean;
}
