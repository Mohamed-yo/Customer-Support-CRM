import { httpClient } from './httpClient';

export interface LoginResponse {
  id: string;
  token: string;
  email: string;
  displayName: string;
  expiresAtUtc: string;
  roles: string[];
}

export interface MeResponse {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const { data } = await httpClient.post<LoginResponse>('/api/auth/login', { email, password });
  return data;
}

export async function fetchMe(): Promise<MeResponse> {
  const { data } = await httpClient.get<MeResponse>('/api/auth/me');
  return data;
}
