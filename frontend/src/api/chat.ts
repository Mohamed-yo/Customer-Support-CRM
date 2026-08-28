import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
import { httpClient, portalHttpClient } from './httpClient';
import { useAuthStore } from '../store/useAuthStore';
import { useCustomerAuthStore } from '../store/useCustomerAuthStore';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

export type ChatSide = 'staff' | 'customer';

export interface ChatMessage {
  id: string;
  ticketId: string;
  senderType: 'Staff' | 'Customer';
  body: string;
  sentAtUtc: string;
}

export async function getChatHistory(ticketId: string, side: ChatSide): Promise<ChatMessage[]> {
  const client = side === 'staff' ? httpClient : portalHttpClient;
  const { data } = await client.get<ChatMessage[]>(`/api/chat/${ticketId}/history`);
  return data;
}

// Story 12 (post-review amendment): customer-only. Creates a new Ticket(Source="Chat")
// on first contact, or returns the customer's existing open chat ticket - never a
// duplicate. The caller then navigates to that ticket's detail page, which already
// mounts <ChatWidget side="customer" />.
export async function startChat(): Promise<{ ticketId: string }> {
  const { data } = await portalHttpClient.post<{ ticketId: string }>('/api/portal/chat/start');
  return data;
}

export function createChatConnection(side: ChatSide): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${baseURL}/hubs/chat`, {
      accessTokenFactory: () =>
        (side === 'staff' ? useAuthStore.getState().token : useCustomerAuthStore.getState().token) ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}
