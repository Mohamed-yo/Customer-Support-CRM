import { portalHttpClient } from './httpClient';

export interface CustomerLoginResponse {
  customerId: string;
  token: string;
  email: string;
  fullName: string;
  expiresAtUtc: string;
}

export interface PortalTicketListItem {
  id: string;
  subject: string;
  status: string;
  priority: string;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
  hasFeedback: boolean;
}

export interface PortalTicketHistoryEntry {
  timestampUtc: string;
  action: string;
}

export interface PortalFeedbackItem {
  rating: number;
  comment: string | null;
  createdAtUtc: string;
}

export interface PortalTicketDetail {
  id: string;
  subject: string;
  description: string | null;
  status: string;
  priority: string;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
  history: PortalTicketHistoryEntry[];
  feedback: PortalFeedbackItem | null;
}

export interface PortalSubmitTicketRequest {
  subject: string;
  description?: string | null;
  priority?: string | null;
}

export async function registerCustomer(payload: {
  fullName: string;
  email: string;
  phone?: string | null;
  password: string;
}): Promise<CustomerLoginResponse> {
  const { data } = await portalHttpClient.post<CustomerLoginResponse>('/api/portal/auth/register', payload);
  return data;
}

export async function loginCustomer(email: string, password: string): Promise<CustomerLoginResponse> {
  const { data } = await portalHttpClient.post<CustomerLoginResponse>('/api/portal/auth/login', { email, password });
  return data;
}

export async function submitTicket(payload: PortalSubmitTicketRequest): Promise<PortalTicketListItem> {
  const { data } = await portalHttpClient.post<PortalTicketListItem>('/api/portal/tickets', payload);
  return data;
}

export async function listMyRequests(): Promise<PortalTicketListItem[]> {
  const { data } = await portalHttpClient.get<PortalTicketListItem[]>('/api/portal/my-requests');
  return data;
}

export async function getMyRequest(id: string): Promise<PortalTicketDetail> {
  const { data } = await portalHttpClient.get<PortalTicketDetail>(`/api/portal/my-requests/${id}`);
  return data;
}

export async function submitFeedback(id: string, rating: number, comment?: string | null): Promise<PortalFeedbackItem> {
  const { data } = await portalHttpClient.post<PortalFeedbackItem>(`/api/portal/my-requests/${id}/feedback`, {
    rating,
    comment: comment || null,
  });
  return data;
}
