import { httpClient } from './httpClient';

export interface LoginResponse {
  token: string;
  email: string;
  displayName: string;
  expiresAtUtc: string;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const { data } = await httpClient.post<LoginResponse>('/api/auth/login', { email, password });
  return data;
}

export async function fetchMe() {
  const { data } = await httpClient.get('/api/auth/me');
  return data;
}
