import { httpClient } from './httpClient';

export interface Customer {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  createdAtUtc: string;
}

export interface CustomerUpsert {
  fullName: string;
  email: string;
  phone?: string | null;
}

export async function listCustomers(): Promise<Customer[]> {
  const { data } = await httpClient.get<Customer[]>('/api/customers');
  return data;
}

export async function getCustomer(id: string): Promise<Customer> {
  const { data } = await httpClient.get<Customer>(`/api/customers/${id}`);
  return data;
}

export async function createCustomer(body: CustomerUpsert): Promise<Customer> {
  const { data } = await httpClient.post<Customer>('/api/customers', body);
  return data;
}

export async function updateCustomer(id: string, body: CustomerUpsert): Promise<void> {
  await httpClient.put<void>(`/api/customers/${id}`, body);
}

export async function deleteCustomer(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/customers/${id}`);
}
