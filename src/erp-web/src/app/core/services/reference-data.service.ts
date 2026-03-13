import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LookupOption } from '../models/common.models';
import { PermissionDto, RoleDto } from '../models/erp.models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly api = inject(ApiService);

  getBranches(): Observable<LookupOption[]> {
    return this.api.get<LookupOption[]>('branches/lookup');
  }

  getCategories(): Observable<LookupOption[]> {
    return this.api.get<LookupOption[]>('categories/lookup');
  }

  getProducts(): Observable<LookupOption[]> {
    return this.api.get<LookupOption[]>('products/lookup');
  }

  getCustomers(): Observable<LookupOption[]> {
    return this.api.get<LookupOption[]>('customers/lookup');
  }

  getSuppliers(): Observable<LookupOption[]> {
    return this.api.get<LookupOption[]>('suppliers/lookup');
  }

  getRoles(): Observable<RoleDto[]> {
    return this.api.get<RoleDto[]>('roles');
  }

  getPermissions(): Observable<PermissionDto[]> {
    return this.api.get<PermissionDto[]>('permissions');
  }
}
