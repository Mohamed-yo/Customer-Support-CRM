import { httpClient, portalHttpClient } from './httpClient';

export type AiStatus = 'Ok' | 'NotConfigured' | 'ProviderError';

export interface AiTextResult {
  status: AiStatus;
  value: string | null;
  error: string | null;
}

export interface AiKbArticleSuggestion {
  id: string;
  title: string;
}

export interface AiKbArticlesResult {
  status: AiStatus;
  value: AiKbArticleSuggestion[] | null;
  error: string | null;
}

export async function summarizeTicket(ticketId: string): Promise<AiTextResult> {
  const { data } = await httpClient.post<AiTextResult>(`/api/ai/tickets/${ticketId}/summarize`);
  return data;
}

export async function suggestReply(ticketId: string): Promise<AiTextResult> {
  const { data } = await httpClient.post<AiTextResult>(`/api/ai/tickets/${ticketId}/suggest-reply`);
  return data;
}

export async function suggestCategory(ticketId: string): Promise<AiTextResult> {
  const { data } = await httpClient.post<AiTextResult>(`/api/ai/tickets/${ticketId}/suggest-category`);
  return data;
}

export async function suggestKbArticles(ticketId: string): Promise<AiKbArticlesResult> {
  const { data } = await httpClient.post<AiKbArticlesResult>(`/api/ai/tickets/${ticketId}/suggest-kb-articles`);
  return data;
}

// Anonymous, portal-facing self-service AI chat - uses portalHttpClient (not the staff
// httpClient) since it belongs alongside the other customer-facing portal pages.
export async function sendAiChatMessage(sessionId: string, message: string): Promise<AiTextResult> {
  const { data } = await portalHttpClient.post<AiTextResult>('/api/ai/chat', { sessionId, message });
  return data;
}
