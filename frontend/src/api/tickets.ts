import { httpClient } from './httpClient';

export type TicketStatus = 'Open' | 'InProgress' | 'Closed';
export const TICKET_STATUSES: TicketStatus[] = ['Open', 'InProgress', 'Closed'];

export interface Ticket {
  id: string;
  customerId: string;
  customerFullName: string;
  subject: string;
  description: string | null;
  status: TicketStatus;
  createdAtUtc: string;
}

export interface TicketUpsert {
  customerId: string;
  subject: string;
  description?: string | null;
  status: TicketStatus;
}

export async function listTickets(): Promise<Ticket[]> {
  const { data } = await httpClient.get<Ticket[]>('/api/tickets');
  return data;
}

export async function getTicket(id: string): Promise<Ticket> {
  const { data } = await httpClient.get<Ticket>(`/api/tickets/${id}`);
  return data;
}

export async function createTicket(body: TicketUpsert): Promise<Ticket> {
  const { data } = await httpClient.post<Ticket>('/api/tickets', body);
  return data;
}

export async function updateTicket(id: string, body: TicketUpsert): Promise<void> {
  await httpClient.put<void>(`/api/tickets/${id}`, body);
}

export async function deleteTicket(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/tickets/${id}`);
}
