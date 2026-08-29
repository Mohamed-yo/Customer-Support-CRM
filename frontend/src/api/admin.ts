import { httpClient } from './httpClient';

export interface AdminUserListItem {
  id: string;
  email: string;
  displayName: string;
  isActive: boolean;
  createdAtUtc: string;
  departmentId: string | null;
  departmentName: string | null;
  branchId: string | null;
  branchName: string | null;
  roles: string[];
}

export interface RoleListItem {
  id: string;
  name: string;
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
  departmentId?: string | null;
  branchId?: string | null;
}

export interface PatchUserRequest {
  displayName?: string;
  departmentId?: string | null;
  branchId?: string | null;
}

export async function listUsers(search?: string): Promise<AdminUserListItem[]> {
  const { data } = await httpClient.get<AdminUserListItem[]>('/api/admin/users', {
    params: search ? { search } : undefined,
  });
  return data;
}

export async function getUser(id: string): Promise<AdminUserListItem> {
  const { data } = await httpClient.get<AdminUserListItem>(`/api/admin/users/${id}`);
  return data;
}

export async function createUser(body: CreateUserRequest): Promise<AdminUserListItem> {
  const { data } = await httpClient.post<AdminUserListItem>('/api/admin/users', body);
  return data;
}

export async function patchUser(id: string, body: PatchUserRequest): Promise<AdminUserListItem> {
  const { data } = await httpClient.patch<AdminUserListItem>(`/api/admin/users/${id}`, body);
  return data;
}

export async function deactivateUser(id: string): Promise<void> {
  await httpClient.post<void>(`/api/admin/users/${id}/deactivate`);
}

export async function reactivateUser(id: string): Promise<void> {
  await httpClient.post<void>(`/api/admin/users/${id}/reactivate`);
}

export async function assignRole(userId: string, roleId: string): Promise<void> {
  await httpClient.post<void>(`/api/admin/users/${userId}/roles`, { roleId });
}

export async function removeRole(userId: string, roleId: string): Promise<void> {
  await httpClient.delete<void>(`/api/admin/users/${userId}/roles/${roleId}`);
}

export async function listRoles(): Promise<RoleListItem[]> {
  const { data } = await httpClient.get<RoleListItem[]>('/api/admin/roles');
  return data;
}

export interface AuditLogListItem {
  id: string;
  timestampUtc: string;
  action: string;
  outcome: string;
  actorUserId: string | null;
  actorEmail: string | null;
  targetUserId: string | null;
  details: string | null;
}

export interface AuditLogFilters {
  action?: string;
  actorUserId?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}

export async function listAuditLogs(filters?: AuditLogFilters): Promise<AuditLogListItem[]> {
  const { data } = await httpClient.get<AuditLogListItem[]>('/api/admin/audit-logs', { params: filters });
  return data;
}
