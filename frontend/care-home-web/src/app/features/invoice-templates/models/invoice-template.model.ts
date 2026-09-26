import { MasterDataUsage } from '../../../shared/format/master-data-usage';

export interface InvoiceTemplate {
  id: number;
  name: string;
  invoiceCategoryId: number;
  invoiceCategoryName: string;
  fundingAuthorityId?: number | null;
  fundingAuthorityName?: string | null;
  careHomeId?: number | null;
  careHomeName?: string | null;
  companyId?: number | null;
  companyName?: string | null;
  headerText1?: string | null;
  headerText2?: string | null;
  footerText?: string | null;
  bankAccountName?: string | null;
  sortCode?: string | null;
  accountNumber?: string | null;
  contactName?: string | null;
  contactJobTitle?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  emailSubjectTemplate?: string | null;
  emailBodyTemplate?: string | null;
  companyLogoPath?: string | null;
  authorityLogoPath?: string | null;
  isActive: boolean;
  usage?: MasterDataUsage | null;
}

export interface UpsertInvoiceTemplateRequest {
  name: string;
  invoiceCategoryId: number;
  fundingAuthorityId?: number | null;
  careHomeId?: number | null;
  companyId?: number | null;
  headerText1?: string | null;
  headerText2?: string | null;
  footerText?: string | null;
  bankAccountName?: string | null;
  sortCode?: string | null;
  accountNumber?: string | null;
  contactName?: string | null;
  contactJobTitle?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  emailSubjectTemplate?: string | null;
  emailBodyTemplate?: string | null;
  isActive?: boolean;
}
