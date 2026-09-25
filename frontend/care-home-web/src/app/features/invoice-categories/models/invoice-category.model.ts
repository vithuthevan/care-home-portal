import { MasterDataUsage } from '../../../shared/format/master-data-usage';

export interface InvoiceCategory {
  id: number;
  code: string;
  name: string;
  description: string | null;
  groupingMode?: string;
  isActive: boolean;
  configurationSource?: string;
  usage?: MasterDataUsage | null;
}

export interface CreateInvoiceCategoryRequest {
  code: string;
  name: string;
  description?: string;
  groupingMode?: string;
}

export interface UpdateInvoiceCategoryRequest extends CreateInvoiceCategoryRequest {
  isActive: boolean;
}
