import { MasterDataUsage } from '../../../shared/format/master-data-usage';

export interface InvoiceCategory {
  id: number;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  configurationSource?: string;
  usage?: MasterDataUsage | null;
}

export interface CreateInvoiceCategoryRequest {
  code: string;
  name: string;
  description?: string;
}

export interface UpdateInvoiceCategoryRequest extends CreateInvoiceCategoryRequest {
  isActive: boolean;
}
