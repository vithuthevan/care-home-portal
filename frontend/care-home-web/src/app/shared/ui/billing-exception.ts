const LABELS: Record<string, string> = {
  MISSING_NOMINAL: 'A nominal code must be configured before generating this invoice.',
  ALREADY_FULLY_BILLED: 'Already fully billed for this period.',
  OVERLAPPING_FUNDING_CONTRACTS: 'This funding contract overlaps an existing funding contract.',
};

export function billingExceptionLabel(code: string, fallback: string): string {
  return LABELS[code] || fallback;
}
