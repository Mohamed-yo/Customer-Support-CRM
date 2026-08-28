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
      // This app never uses cookie-based auth (JWT only, via accessTokenFactory above -
      // Authorization header for negotiate/long-polling, ?access_token= query string for
      // the WebSocket handshake per Program.cs's OnMessageReceived). The SignalR client
      // defaults withCredentials to true, which makes /negotiate a credentialed
      // cross-origin request; the backend's CORS policy intentionally does not send
      // Access-Control-Allow-Credentials (it doesn't need to - no cookies are ever sent),
      // so the browser blocks it. Disabling it here removes the mismatch without loosening
      // CORS for the rest of the app.
      withCredentials: false,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}
