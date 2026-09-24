/** Maps blank optional email fields to null for API requests. */
export function optionalEmail(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed === '' ? null : trimmed;
}
