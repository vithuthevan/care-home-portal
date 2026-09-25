export interface MasterDataUsage {
  fundingContractCount?: number;
  invoiceCount?: number;
  invoiceLineSnapshotCount?: number;
  miscChargeCount?: number;
  invoiceTemplateCount?: number;
  pinnedContractCount?: number;
  totalFinancialReferences?: number;
  hasFinancialReferences?: boolean;
}

export function masterDataUsageLabel(usage: MasterDataUsage | null | undefined): string | null {
  if (!usage) {
    return null;
  }
  const total =
    usage.totalFinancialReferences ??
    (usage.fundingContractCount ?? 0) +
      (usage.invoiceCount ?? 0) +
      (usage.invoiceLineSnapshotCount ?? 0) +
      (usage.miscChargeCount ?? 0) +
      (usage.invoiceTemplateCount ?? 0) +
      (usage.pinnedContractCount ?? 0);
  if (total <= 0) {
    return 'Not used on financial records yet';
  }
  if (total === 1) {
    return 'Used by 1 financial record';
  }
  return `Used by ${total} financial records`;
}

export function fundingAuthorityUsageLabel(usage: MasterDataUsage | null | undefined): string {
  if (!usage) {
    return 'Not used on financial records yet';
  }
  const contracts = usage.fundingContractCount ?? 0;
  const invoices = usage.invoiceCount ?? 0;
  if (contracts === 0 && invoices === 0) {
    return 'Not used on financial records yet';
  }
  const parts: string[] = [];
  if (contracts > 0) {
    parts.push(contracts === 1 ? '1 contract' : `${contracts} contracts`);
  }
  if (invoices > 0) {
    parts.push(invoices === 1 ? '1 invoice' : `${invoices} invoices`);
  }
  return `Used by ${parts.join(' and ')}`;
}

export function invoiceCategoryUsageLabel(usage: MasterDataUsage | null | undefined): string {
  if (!usage) {
    return 'Not used on financial records yet';
  }
  const invoices = usage.invoiceCount ?? 0;
  if (invoices > 0) {
    return invoices === 1 ? 'Used by 1 invoice' : `Used by ${invoices} invoices`;
  }
  const contracts = usage.fundingContractCount ?? 0;
  if (contracts > 0) {
    return contracts === 1 ? 'Used by 1 contract' : `Used by ${contracts} contracts`;
  }
  const templates = usage.invoiceTemplateCount ?? 0;
  if (templates > 0) {
    return templates === 1 ? 'Used by 1 template' : `Used by ${templates} templates`;
  }
  return 'Not used on financial records yet';
}

export function invoiceTemplateUsageLabel(usage: MasterDataUsage | null | undefined): string {
  if (!usage) {
    return 'Not used on financial records yet';
  }
  const invoices = usage.invoiceCount ?? 0;
  const pinned = usage.pinnedContractCount ?? 0;
  if (invoices === 0 && pinned === 0) {
    return 'Not used on financial records yet';
  }
  const parts: string[] = [];
  if (pinned > 0) {
    parts.push(pinned === 1 ? '1 contract' : `${pinned} contracts`);
  }
  if (invoices > 0) {
    parts.push(invoices === 1 ? '1 invoice' : `${invoices} invoices`);
  }
  return `Used by ${parts.join(' and ')}`;
}

export function configurationSourceLabel(source: string | null | undefined): string {
  if (source === 'SystemDefault') {
    return 'System default';
  }
  return 'Organisation';
}

export function deactivateMasterDataMessage(
  name: string,
  usage: MasterDataUsage | null | undefined,
): string {
  const usageLabel = masterDataUsageLabel(usage);
  if (usageLabel && usageLabel !== 'Not used on financial records yet') {
    return `${name} will be marked inactive. Historical invoices and exports keep their existing accounting snapshots. ${usageLabel}.`;
  }
  return `${name} will no longer be available for new billing setup.`;
}

export function deactivateFundingAuthorityMessage(
  name: string,
  usage: MasterDataUsage | null | undefined,
): string {
  const usageLabel = fundingAuthorityUsageLabel(usage);
  if (usageLabel !== 'Not used on financial records yet') {
    return `${name} will be marked inactive. Historical invoices and contracts keep their existing records. ${usageLabel}.`;
  }
  return `${name} will no longer be available for new funding contracts.`;
}

export function deactivateInvoiceTemplateMessage(
  name: string,
  usage: MasterDataUsage | null | undefined,
): string {
  const usageLabel = invoiceTemplateUsageLabel(usage);
  if (usageLabel !== 'Not used on financial records yet') {
    return `${name} will be marked inactive. Invoices already generated keep their template snapshot. ${usageLabel}.`;
  }
  return `${name} will no longer be selected for new billing.`;
}
