import { httpClient } from './httpClient';

export interface Branch {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface BranchUpsert {
  name: string;
}

export async function listBranches(): Promise<Branch[]> {
  const { data } = await httpClient.get<Branch[]>('/api/branches');
  return data;
}

export async function createBranch(body: BranchUpsert): Promise<Branch> {
  const { data } = await httpClient.post<Branch>('/api/branches', body);
  return data;
}

export async function updateBranch(id: string, body: BranchUpsert): Promise<void> {
  await httpClient.put<void>(`/api/branches/${id}`, body);
}

export async function deactivateBranch(id: string): Promise<void> {
  await httpClient.post<void>(`/api/branches/${id}/deactivate`);
}

export async function reactivateBranch(id: string): Promise<void> {
  await httpClient.post<void>(`/api/branches/${id}/reactivate`);
}
