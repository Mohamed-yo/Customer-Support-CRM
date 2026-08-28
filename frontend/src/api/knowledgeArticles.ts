import type { AxiosInstance } from 'axios';
import { httpClient } from './httpClient';

export interface KnowledgeArticleListItem {
  id: string;
  title: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface KnowledgeArticle {
  id: string;
  title: string;
  body: string;
  createdByUserId: string;
  createdByDisplayName: string;
  createdAtUtc: string;
  updatedByUserId: string | null;
  updatedByDisplayName: string | null;
  updatedAtUtc: string | null;
}

export interface KnowledgeArticleUpsert {
  title: string;
  body: string;
}

// `client` defaults to the staff instance; the portal (customer) pages pass
// `portalHttpClient` explicitly since read access is shared but the caller's session
// determines which Authorization header is actually valid.
export async function listKnowledgeArticles(q?: string, client: AxiosInstance = httpClient): Promise<KnowledgeArticleListItem[]> {
  const { data } = await client.get<KnowledgeArticleListItem[]>('/api/knowledge-articles', {
    params: q ? { q } : undefined,
  });
  return data;
}

export async function getKnowledgeArticle(id: string, client: AxiosInstance = httpClient): Promise<KnowledgeArticle> {
  const { data } = await client.get<KnowledgeArticle>(`/api/knowledge-articles/${id}`);
  return data;
}

export async function createKnowledgeArticle(body: KnowledgeArticleUpsert): Promise<KnowledgeArticle> {
  const { data } = await httpClient.post<KnowledgeArticle>('/api/knowledge-articles', body);
  return data;
}

export async function updateKnowledgeArticle(id: string, body: KnowledgeArticleUpsert): Promise<void> {
  await httpClient.put<void>(`/api/knowledge-articles/${id}`, body);
}

export async function deleteKnowledgeArticle(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/knowledge-articles/${id}`);
}
