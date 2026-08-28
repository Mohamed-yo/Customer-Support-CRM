import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { portalHttpClient } from '../../api/httpClient';
import {
  getKnowledgeArticle,
  listKnowledgeArticles,
  type KnowledgeArticle,
  type KnowledgeArticleListItem,
} from '../../api/knowledgeArticles';

export default function PortalKnowledgeBasePage() {
  const { t } = useTranslation();

  const [query, setQuery] = useState('');
  const [articles, setArticles] = useState<KnowledgeArticleListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<KnowledgeArticle | null>(null);

  const load = (q?: string) => {
    setLoading(true);
    setError(null);
    listKnowledgeArticles(q, portalHttpClient)
      .then(setArticles)
      .catch(() => setError(t('kb.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    load(query.trim() || undefined);
  };

  const openArticle = async (id: string) => {
    try {
      const article = await getKnowledgeArticle(id, portalHttpClient);
      setSelected(article);
    } catch {
      setError(t('kb.loadFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('kb.title')}</h1>

      <form onSubmit={handleSearch} className="flex gap-2">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={t('kb.searchPlaceholder') ?? ''}
          className="w-full max-w-sm rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
        />
        <button
          type="submit"
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white"
        >
          {t('kb.search')}
        </button>
      </form>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {selected ? (
        <section className="rounded border border-slate-200 bg-white p-4">
          <button
            type="button"
            onClick={() => setSelected(null)}
            className="mb-3 text-sm font-medium text-slate-600 hover:text-slate-800"
          >
            {t('kb.backToList')}
          </button>
          <h2 className="mb-2 text-lg font-semibold text-slate-800">{selected.title}</h2>
          <p className="whitespace-pre-wrap text-sm text-slate-700">{selected.body}</p>
        </section>
      ) : (
        !loading && (
          <div className="rounded border border-slate-200 bg-white">
            {articles.length === 0 ? (
              <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
                <p className="text-sm text-slate-500">{t('kb.empty')}</p>
              </div>
            ) : (
              <ul className="flex flex-col">
                {articles.map((a) => (
                  <li key={a.id} className="border-b border-slate-100 last:border-0">
                    <button
                      type="button"
                      onClick={() => openArticle(a.id)}
                      className="w-full px-4 py-3 text-start text-sm font-medium text-slate-800 hover:bg-slate-50"
                    >
                      {a.title}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )
      )}
    </div>
  );
}
