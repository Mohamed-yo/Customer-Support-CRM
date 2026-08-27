import { httpClient } from './httpClient';

export type TicketStatus = 'Open' | 'InProgress' | 'Closed';
export const TICKET_STATUSES: TicketStatus[] = ['Open', 'InProgress', 'Closed'];

export type TicketCategory = 'General' | 'Billing' | 'Technical' | 'Account';
export const TICKET_CATEGORIES: TicketCategory[] = ['General', 'Billing', 'Technical', 'Account'];

export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Urgent';
export const TICKET_PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent'];

export interface Ticket {
  id: string;
  customerId: string;
  customerFullName: string;
  subject: string;
  description: string | null;
  status: TicketStatus;
  createdAtUtc: string;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  category: TicketCategory;
  priority: TicketPriority;
}

export interface TicketUpsert {
  customerId: string;
  subject: string;
  description?: string | null;
  status: TicketStatus;
  assignedToUserId?: string | null;
  category: TicketCategory;
  priority: TicketPriority;
}

export interface AssignableUser {
  id: string;
  displayName: string;
}

export interface HistoryEntry {
  id: string;
  action: string;
  outcome: string;
  actorUserId: string | null;
  actorDisplayName: string | null;
  timestampUtc: string;
}

export interface TicketNote {
  id: string;
  ticketId: string;
  authorUserId: string;
  authorDisplayName: string;
  body: string;
  createdAtUtc: string;
}

export interface TicketAttachment {
  id: string;
  ticketId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByUserId: string;
  uploadedByDisplayName: string;
  createdAtUtc: string;
}

export interface TicketTask {
  id: string;
  ticketId: string;
  title: string;
  dueAtUtc: string | null;
  isDone: boolean;
  createdAtUtc: string;
}

export interface TicketTaskUpsert {
  title: string;
  dueAtUtc?: string | null;
  isDone: boolean;
}

export async function listTickets(customerId?: string): Promise<Ticket[]> {
  const { data } = await httpClient.get<Ticket[]>('/api/tickets', {
    params: customerId ? { customerId } : undefined,
  });
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

export async function listAssignableUsers(): Promise<AssignableUser[]> {
  const { data } = await httpClient.get<AssignableUser[]>('/api/tickets/assignable-users');
  return data;
}

export async function getTicketHistory(id: string): Promise<HistoryEntry[]> {
  const { data } = await httpClient.get<HistoryEntry[]>(`/api/tickets/${id}/history`);
  return data;
}

export async function listTicketNotes(ticketId: string): Promise<TicketNote[]> {
  const { data } = await httpClient.get<TicketNote[]>(`/api/tickets/${ticketId}/notes`);
  return data;
}

export async function createTicketNote(ticketId: string, body: string): Promise<TicketNote> {
  const { data } = await httpClient.post<TicketNote>(`/api/tickets/${ticketId}/notes`, { body });
  return data;
}

export async function listTicketAttachments(ticketId: string): Promise<TicketAttachment[]> {
  const { data } = await httpClient.get<TicketAttachment[]>(`/api/tickets/${ticketId}/attachments`);
  return data;
}

export async function uploadTicketAttachment(ticketId: string, file: File): Promise<TicketAttachment> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await httpClient.post<TicketAttachment>(`/api/tickets/${ticketId}/attachments`, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

// Fetched through the authenticated httpClient (not a plain URL) because the
// download endpoint requires the same JWT bearer auth as every other ticket
// endpoint — a raw <a href>/window.open would have no Authorization header.
export async function downloadTicketAttachment(ticketId: string, attachmentId: string, fileName: string): Promise<void> {
  const response = await httpClient.get(`/api/tickets/${ticketId}/attachments/${attachmentId}/download`, {
    responseType: 'blob',
  });
  const url = window.URL.createObjectURL(response.data as Blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export async function deleteTicketAttachment(ticketId: string, attachmentId: string): Promise<void> {
  await httpClient.delete(`/api/tickets/${ticketId}/attachments/${attachmentId}`);
}

export async function listTicketTasks(ticketId: string): Promise<TicketTask[]> {
  const { data } = await httpClient.get<TicketTask[]>(`/api/tickets/${ticketId}/tasks`);
  return data;
}

export async function createTicketTask(ticketId: string, body: TicketTaskUpsert): Promise<TicketTask> {
  const { data } = await httpClient.post<TicketTask>(`/api/tickets/${ticketId}/tasks`, body);
  return data;
}

export async function updateTicketTask(ticketId: string, taskId: string, body: TicketTaskUpsert): Promise<void> {
  await httpClient.put(`/api/tickets/${ticketId}/tasks/${taskId}`, body);
}

export async function deleteTicketTask(ticketId: string, taskId: string): Promise<void> {
  await httpClient.delete(`/api/tickets/${ticketId}/tasks/${taskId}`);
}
