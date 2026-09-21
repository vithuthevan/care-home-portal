/** Prefer stable public UUID in URLs when the API provides it. */
export function entityRouteKey(entity: { publicId?: string | null; id: number }): string {
  if (entity.publicId) {
    return entity.publicId;
  }
  return String(entity.id);
}

export function isUuidRouteKey(key: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(key);
}
