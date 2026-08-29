import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type ApiKeyListItem,
  type CreateApiKeyResponse,
  createApiKey,
  listApiKeys,
  revokeApiKey,
} from '../api/apiKeys';

export default function ApiKeysPage() {
  const { t } = useTranslation();

  const [apiKeys, setApiKeys] = useState<ApiKeyListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [label, setLabel] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);

  // Held only in memory, only until the user navigates away or dismisses it - never
  // persisted, never fetched again (the backend never returns a plaintext key twice).
  const [newlyCreated, setNewlyCreated] = useState<CreateApiKeyResponse | null>(null);
  const [copied, setCopied] = useState(false);

  const loadData = () => {
    setLoading(true);
    setError(null);
    listApiKeys()
      .then(setApiKeys)
      .catch(() => setError(t('apiKeys.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!formOpen) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [formOpen]);

  const openCreateForm = () => {
    setLabel('');
    setFormError(null);
    setAttemptedSubmit(false);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setFormError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    if (!label.trim()) {
      setAttemptedSubmit(true);
      return;
    }

    setSubmitting(true);
    try {
      const response = await createApiKey({ label: label.trim() });
      setNewlyCreated(response);
      setCopied(false);
      closeForm();
      loadData();
    } catch {
      setFormError(t('apiKeys.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleRevoke = async (item: ApiKeyListItem) => {
    if (!window.confirm(t('apiKeys.revokeConfirm'))) return;
    try {
      await revokeApiKey(item.id);
      loadData();
    } catch {
      setError(t('apiKeys.revokeFailed'));
    }
  };

  const handleCopy = async () => {
    if (!newlyCreated) return;
    try {
      await navigator.clipboard.writeText(newlyCreated.plaintextKey);
      setCopied(true);
    } catch {
      // Clipboard API unavailable/denied - the key remains visible for manual copy.
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('apiKeys.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          {t('apiKeys.new')}
        </button>
      </div>

      <p className="text-sm text-slate-600">{t('apiKeys.description')}</p>

      {newlyCreated && (
        <div className="rounded border border-amber-300 bg-amber-50 p-4">
          <p className="text-sm font-semibold text-amber-900">{t('apiKeys.secretWarning')}</p>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <code className="break-all rounded border border-amber-200 bg-white px-3 py-2 text-sm text-slate-800">
              {newlyCreated.plaintextKey}
            </code>
            <button
              type="button"
              onClick={handleCopy}
              className="rounded border border-amber-300 px-3 py-1.5 text-sm font-medium text-amber-900 hover:bg-amber-100"
            >
              {copied ? t('apiKeys.copied') : t('apiKeys.copy')}
            </button>
            <button
              type="button"
              onClick={() => setNewlyCreated(null)}
              className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
            >
              {t('apiKeys.dismiss')}
            </button>
          </div>
        </div>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {apiKeys.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('apiKeys.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('apiKeys.columns.label')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('apiKeys.columns.prefix')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('apiKeys.columns.created')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('apiKeys.columns.lastUsed')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('apiKeys.columns.status')}
                  </th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {apiKeys.map((item) => (
                  <tr key={item.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="max-w-xs truncate px-4 py-3 text-slate-800">{item.label}</td>
                    <td className="px-4 py-3 font-mono text-slate-600">{item.prefix}</td>
                    <td className="px-4 py-3 text-slate-600">{new Date(item.createdAtUtc).toLocaleString()}</td>
                    <td className="px-4 py-3 text-slate-600">
                      {item.lastUsedAtUtc ? new Date(item.lastUsedAtUtc).toLocaleString() : t('apiKeys.neverUsed')}
                    </td>
                    <td className="px-4 py-3 text-slate-600">
                      {item.isActive ? t('apiKeys.active') : t('apiKeys.revoked')}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        {item.isActive && (
                          <button
                            type="button"
                            onClick={() => handleRevoke(item)}
                            className="rounded px-2.5 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50"
                          >
                            {t('apiKeys.revoke')}
                          </button>
                        )}
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
            <h2 className="text-lg font-semibold text-slate-800">{t('apiKeys.new')}</h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('apiKeys.form.label')}</span>
              <input
                type="text"
                value={label}
                onChange={(e) => setLabel(e.target.value)}
                placeholder={t('apiKeys.form.labelPlaceholder') ?? ''}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {attemptedSubmit && !label.trim() && (
                <span className="text-sm text-red-600">{t('apiKeys.form.labelRequired')}</span>
              )}
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={closeForm}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              >
                {t('apiKeys.form.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('apiKeys.form.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
