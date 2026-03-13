export interface LookupOption {
  id: string;
  code: string;
  name: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuthenticatedUser {
  id: string;
  userName: string;
  email?: string | null;
  roles: string[];
  permissions: string[];
  branchIds: string[];
  defaultBranchId?: string | null;
}

export interface TokenEnvelope {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

export interface ListQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}
