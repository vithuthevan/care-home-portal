export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuthUser {
  token: string;
  displayName: string;
  email: string;
  roles: string[];
  careHomeIds: number[];
  tenantName?: string | null;
  tenantPublicId?: string | null;
  mustChangePassword?: boolean;
  showGuardian?: boolean;
  financeModuleEnabled?: boolean;
}
