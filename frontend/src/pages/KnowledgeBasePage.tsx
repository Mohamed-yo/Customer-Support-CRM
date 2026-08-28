import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type KnowledgeArticle,
  type KnowledgeArticleListItem,
  type KnowledgeArticleUpsert,
  createKnowledgeArticle,
  deleteKnowledgeArticle,
  getKnowledgeArticle,
  listKnowledgeArticles,
  updateKnowledgeArticle,
} from '../api/knowledgeArticles';

const EMPTY_FORM: KnowledgeArticleUpsert = { title: '', body: '' };

interface FormErrors {
  title?: string;
  body?: string;
}

function validateForm(values: KnowledgeArticleUpsert): FormErrors {
  const errors: FormErrors = {};
  if (!values.title.trim()) errors.title = 'kb.validation.titleRequired';
  if (!values.body.trim()) errors.body = 'kb.validation.bodyRequired';
  return errors;
}

export default function KnowledgeBasePage() {
  const { t } = useTranslation();

  const [query, setQuery] = useState('');
  const [articles, setArticles] = useState<KnowledgeArticleListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formValues, setFormValues] = useState<KnowledgeArticleUpsert>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);

  const fieldErrors = validateForm(formValues);

  const loadData = (q?: string) => {
    setLoading(true);
    setError(null);
    listKnowledgeArticles(q)
      .then(setArticles)
      .catch(() => setError(t('kb.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Lock background scroll while the modal is open; restore whatever was there before.
  useEffect(() => {
    if (!formOpen) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [formOpen]);

  const handleSearch = (e: FormEvent) => {
    e.preventDefault();
    loadData(query.trim() || undefined);
  };

  const openCreateForm = () => {
    setEditingId(null);
    setFormValues(EMPTY_FORM);
    setFormError(null);
    setAttemptedSubmit(false);
    setFormOpen(true);
  };

  const openEditForm = async (item: KnowledgeArticleListItem) => {
    try {
      const article: KnowledgeArticle = await getKnowledgeArticle(item.id);
      setEditingId(article.id);
      setFormValues({ title: article.title, body: article.body });
      setFormError(null);
      setAttemptedSubmit(false);
      setFormOpen(true);
    } catch {
      setError(t('kb.loadFailed'));
    }
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditingId(null);
    setFormError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    const errors = validateForm(formValues);
    if (Object.keys(errors).length > 0) {
      setAttemptedSubmit(true);
      return;
    }

    setSubmitting(true);
    try {
      if (editingId) {
        await updateKnowledgeArticle(editingId, formValues);
      } else {
        await createKnowledgeArticle(formValues);
      }
      closeForm();
      loadData(query.trim() || undefined);
    } catch {
      setFormError(t('kb.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (item: KnowledgeArticleListItem) => {
    if (!window.confirm(t('kb.deleteConfirm'))) return;
    try {
      await deleteKnowledgeArticle(item.id);
      loadData(query.trim() || undefined);
    } catch {
      setError(t('kb.deleteFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('kb.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          {t('kb.new')}
        </button>
      </div>

      <form onSubmit={handleSearch} className="flex gap-2">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={t('kb.searchPlaceholder') ?? ''}
          className="w-full max-w-sm rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
        />
        <button type="submit" className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white">
          {t('kb.search')}
        </button>
      </form>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {articles.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('kb.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('kb.form.title')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.createdAt')}
                  </th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {articles.map((article) => (
                  <tr key={article.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 text-slate-800">{article.title}</td>
                    <td className="px-4 py-3 text-slate-600">{new Date(article.createdAtUtc).toLocaleDateString()}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEditForm(article)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                        >
                          {t('customers.edit')}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(article)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50"
                        >
                          {t('customers.delete')}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {formOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-900/50 p-4"
          onClick={closeForm}
        >
          <form
            onSubmit={handleSubmit}
            onClick={(e) => e.stopPropagation()}
            noValidate
            className="flex max-h-full w-full max-w-lg flex-col gap-4 overflow-y-auto rounded bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-slate-800">
              {editingId ? t('kb.edit') : t('kb.new')}
            </h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('kb.form.title')}</span>
              <input
                type="text"
                value={formValues.title}
                onChange={(e) => setFormValues((v) => ({ ...v, title: e.target.value }))}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {attemptedSubmit && fieldErrors.title && (
                <span className="text-sm text-red-600">{t(fieldErrors.title)}</span>
              )}
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('kb.form.body')}</span>
              <textarea
                value={formValues.body}
                onChange={(e) => setFormValues((v) => ({ ...v, body: e.target.value }))}
                rows={8}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {attemptedSubmit && fieldErrors.body && (
                <span className="text-sm text-red-600">{t(fieldErrors.body)}</span>
              )}
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={closeForm}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              >
                {t('kb.form.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('kb.form.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
