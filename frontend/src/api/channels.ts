import axios from 'axios';
import { httpClient } from './httpClient';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

// Public, unauthenticated endpoint - deliberately does NOT use httpClient (which would
// attach a staff Authorization header if one happens to be present in this browser).
const publicClient = axios.create({ baseURL, headers: { 'Content-Type': 'application/json' } });

export interface WebFormSubmission {
  fullName: string;
  email: string;
  phone?: string | null;
  subject: string;
  description?: string | null;
  priority?: string | null;
}

export interface WebFormSubmissionResult {
  ticketId: string;
  referenceNumber: string;
}

export async function submitWebForm(payload: WebFormSubmission): Promise<WebFormSubmissionResult> {
  const { data } = await publicClient.post<WebFormSubmissionResult>('/api/channels/webform', payload);
  return data;
}

export interface ChannelMessage {
  id: string;
  channel: string;
  direction: 'Inbound' | 'Outbound';
  fromAddress: string;
  toAddress: string | null;
  subject: string | null;
  body: string;
  sendResult: string;
  sendResultDetail: string | null;
  createdAtUtc: string;
}

export async function listChannelMessages(ticketId: string): Promise<ChannelMessage[]> {
  const { data } = await httpClient.get<ChannelMessage[]>(`/api/channels/tickets/${ticketId}/messages`);
  return data;
}

export async function sendChannelReply(channel: string, ticketId: string, body: string, subject?: string): Promise<ChannelMessage> {
  const { data } = await httpClient.post<ChannelMessage>(`/api/channels/${channel}/outbound`, { ticketId, body, subject });
  return data;
}
