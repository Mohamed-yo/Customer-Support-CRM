import { httpClient } from './httpClient';

export interface Department {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface DepartmentUpsert {
  name: string;
}

export async function listDepartments(): Promise<Department[]> {
  const { data } = await httpClient.get<Department[]>('/api/departments');
  return data;
}

export async function createDepartment(body: DepartmentUpsert): Promise<Department> {
  const { data } = await httpClient.post<Department>('/api/departments', body);
  return data;
}

export async function updateDepartment(id: string, body: DepartmentUpsert): Promise<void> {
  await httpClient.put<void>(`/api/departments/${id}`, body);
}

export async function deactivateDepartment(id: string): Promise<void> {
  await httpClient.post<void>(`/api/departments/${id}/deactivate`);
}

export async function reactivateDepartment(id: string): Promise<void> {
  await httpClient.post<void>(`/api/departments/${id}/reactivate`);
}
