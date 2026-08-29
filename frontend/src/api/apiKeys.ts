import { httpClient } from './httpClient';

export interface ApiKeyListItem {
  id: string;
  label: string;
  prefix: string;
  createdAtUtc: string;
  lastUsedAtUtc: string | null;
  revokedAtUtc: string | null;
  isActive: boolean;
}

export interface CreateApiKeyRequest {
  label: string;
}

// Only ever present in the response to createApiKey - never persisted, never returned
// again by listApiKeys.
export interface CreateApiKeyResponse {
  id: string;
  label: string;
  prefix: string;
  plaintextKey: string;
  createdAtUtc: string;
}

export async function listApiKeys(): Promise<ApiKeyListItem[]> {
  const { data } = await httpClient.get<ApiKeyListItem[]>('/api/api-keys');
  return data;
}

export async function createApiKey(body: CreateApiKeyRequest): Promise<CreateApiKeyResponse> {
  const { data } = await httpClient.post<CreateApiKeyResponse>('/api/api-keys', body);
  return data;
}

export async function revokeApiKey(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/api-keys/${id}`);
}
