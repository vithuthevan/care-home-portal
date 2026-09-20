const LABELS: Record<string, string> = {
  MISSING_CONTRACT: 'No active funding contract for the selected period.',
  MISSING_RATE: 'No applicable funding rate for the selected period.',
  MISSING_NOMINAL: 'A nominal code must be configured before generating this invoice.',
  MISSING_TEMPLATE: 'No invoice template is configured for this billing stream.',
  ALREADY_FULLY_BILLED: 'Already fully billed for this period.',
  OVERLAPPING_FUNDING_CONTRACTS: 'Funding contracts overlap for the same authority and category.',
  PARTIAL_PERIOD_BILLING: 'Only unbilled dates in this period will be invoiced.',
};

export function billingExceptionLabel(code: string, fallback: string): string {
  const message = fallback?.trim();
  if (message) {
    return message;
  }
  return LABELS[code] || code;
}

export function billingExceptionHeadline(code: string): string {
  const headlines: Record<string, string> = {
    MISSING_CONTRACT: 'Cannot bill',
    MISSING_RATE: 'Cannot bill',
    MISSING_NOMINAL: 'Setup required',
    MISSING_TEMPLATE: 'Setup required',
    ALREADY_FULLY_BILLED: 'Already billed',
    OVERLAPPING_FUNDING_CONTRACTS: 'Contract overlap',
    PARTIAL_PERIOD_BILLING: 'Partial period',
  };
  return headlines[code] || 'Requires attention';
}
