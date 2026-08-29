import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type WebhookSubscription,
  type WebhookSubscriptionUpsert,
  createWebhookSubscription,
  deleteWebhookSubscription,
  listWebhookSubscriptions,
  rotateWebhookSigningSecret,
  updateWebhookSubscription,
} from '../api/webhooks';

const EVENT_TYPES = ['ticket.created', 'ticket.closed'] as const;

const EMPTY_FORM: WebhookSubscriptionUpsert = { targetUrl: '', eventType: 'ticket.created', isActive: true };

interface FormErrors {
  targetUrl?: string;
}

function validateForm(values: WebhookSubscriptionUpsert): FormErrors {
  const errors: FormErrors = {};
  try {
    const url = new URL(values.targetUrl);
    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      errors.targetUrl = 'webhooks.validation.targetUrlInvalid';
    }
  } catch {
    errors.targetUrl = 'webhooks.validation.targetUrlInvalid';
  }
  return errors;
}

export default function WebhooksPage() {
  const { t } = useTranslation();

  const [subscriptions, setSubscriptions] = useState<WebhookSubscription[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formValues, setFormValues] = useState<WebhookSubscriptionUpsert>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);

  // Held only in memory, only until dismissed - never persisted, never fetched again (the
  // backend never returns a signing secret in plaintext twice). Mirrors ApiKeysPage.
  const [revealedSecret, setRevealedSecret] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const fieldErrors = validateForm(formValues);

  const loadData = () => {
    setLoading(true);
    setError(null);
    listWebhookSubscriptions()
      .then(setSubscriptions)
      .catch(() => setError(t('webhooks.loadFailed')))
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

  const openCreateForm = () => {
    setEditingId(null);
    setFormValues(EMPTY_FORM);
    setFormError(null);
    setAttemptedSubmit(false);
    setFormOpen(true);
  };

  const openEditForm = (item: WebhookSubscription) => {
    setEditingId(item.id);
    setFormValues({ targetUrl: item.targetUrl, eventType: item.eventType, isActive: item.isActive });
    setFormError(null);
    setAttemptedSubmit(false);
    setFormOpen(true);
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
        await updateWebhookSubscription(editingId, formValues);
      } else {
        const created = await createWebhookSubscription(formValues);
        setRevealedSecret(created.signingSecret);
        setCopied(false);
      }
      closeForm();
      loadData();
    } catch {
      setFormError(t('webhooks.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (item: WebhookSubscription) => {
    if (!window.confirm(t('webhooks.deleteConfirm'))) return;
    try {
      await deleteWebhookSubscription(item.id);
      loadData();
    } catch {
      setError(t('webhooks.deleteFailed'));
    }
  };

  const handleRotateSecret = async (item: WebhookSubscription) => {
    if (!window.confirm(t('webhooks.rotateSecretConfirm'))) return;
    try {
      const secret = await rotateWebhookSigningSecret(item.id);
      setRevealedSecret(secret);
      setCopied(false);
    } catch {
      setError(t('webhooks.rotateSecretFailed'));
    }
  };

  const handleCopySecret = async () => {
    if (!revealedSecret) return;
    try {
      await navigator.clipboard.writeText(revealedSecret);
      setCopied(true);
    } catch {
      // Clipboard API unavailable/denied - the secret remains visible for manual copy.
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('webhooks.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          {t('webhooks.new')}
        </button>
      </div>

      {revealedSecret && (
        <div className="rounded border border-amber-300 bg-amber-50 p-4">
          <p className="text-sm font-semibold text-amber-900">{t('webhooks.secretWarning')}</p>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <code className="break-all rounded border border-amber-200 bg-white px-3 py-2 text-sm text-slate-800">
              {revealedSecret}
            </code>
            <button
              type="button"
              onClick={handleCopySecret}
              className="rounded border border-amber-300 px-3 py-1.5 text-sm font-medium text-amber-900 hover:bg-amber-100"
            >
              {copied ? t('webhooks.copied') : t('webhooks.copy')}
            </button>
            <button
              type="button"
              onClick={() => setRevealedSecret(null)}
              className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
            >
              {t('webhooks.dismiss')}
            </button>
          </div>
        </div>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {subscriptions.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('webhooks.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('webhooks.form.targetUrl')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('webhooks.form.eventType')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('webhooks.form.isActive')}
                  </th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {subscriptions.map((s) => (
                  <tr key={s.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="max-w-xs truncate px-4 py-3 text-slate-800">{s.targetUrl}</td>
                    <td className="px-4 py-3 text-slate-600">{s.eventType}</td>
                    <td className="px-4 py-3 text-slate-600">
                      {s.isActive ? t('webhooks.active') : t('webhooks.inactive')}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => handleRotateSecret(s)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                        >
                          {t('webhooks.rotateSecret')}
                        </button>
                        <button
                          type="button"
                          onClick={() => openEditForm(s)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                        >
                          {t('customers.edit')}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(s)}
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
              {editingId ? t('webhooks.edit') : t('webhooks.new')}
            </h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('webhooks.form.targetUrl')}</span>
              <input
                type="text"
                value={formValues.targetUrl}
                onChange={(e) => setFormValues((v) => ({ ...v, targetUrl: e.target.value }))}
                placeholder="https://example.com/webhook"
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {attemptedSubmit && fieldErrors.targetUrl && (
                <span className="text-sm text-red-600">{t(fieldErrors.targetUrl)}</span>
              )}
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('webhooks.form.eventType')}</span>
              <select
                value={formValues.eventType}
                onChange={(e) => setFormValues((v) => ({ ...v, eventType: e.target.value }))}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              >
                {EVENT_TYPES.map((et) => (
                  <option key={et} value={et}>
                    {et}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={formValues.isActive}
                onChange={(e) => setFormValues((v) => ({ ...v, isActive: e.target.checked }))}
              />
              <span>{t('webhooks.form.isActive')}</span>
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={closeForm}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              >
                {t('webhooks.form.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('webhooks.form.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
