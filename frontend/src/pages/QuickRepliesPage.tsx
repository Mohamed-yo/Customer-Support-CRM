import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type QuickReplyTemplate,
  type QuickReplyTemplateUpsert,
  createQuickReply,
  deleteQuickReply,
  listQuickReplies,
  updateQuickReply,
} from '../api/quickReplies';

const EMPTY_FORM: QuickReplyTemplateUpsert = { title: '', body: '' };

interface FormErrors {
  title?: string;
  body?: string;
}

function validateForm(values: QuickReplyTemplateUpsert): FormErrors {
  const errors: FormErrors = {};
  if (!values.title.trim()) errors.title = 'quickReplies.validation.titleRequired';
  if (!values.body.trim()) errors.body = 'quickReplies.validation.bodyRequired';
  return errors;
}

export default function QuickRepliesPage() {
  const { t } = useTranslation();

  const [templates, setTemplates] = useState<QuickReplyTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editing, setEditing] = useState<QuickReplyTemplate | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formValues, setFormValues] = useState<QuickReplyTemplateUpsert>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);

  const fieldErrors = validateForm(formValues);

  const loadData = () => {
    setLoading(true);
    setError(null);
    listQuickReplies()
      .then(setTemplates)
      .catch(() => setError(t('quickReplies.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const openCreateForm = () => {
    setEditing(null);
    setFormValues(EMPTY_FORM);
    setFormError(null);
    setAttemptedSubmit(false);
    setFormOpen(true);
  };

  const openEditForm = (template: QuickReplyTemplate) => {
    setEditing(template);
    setFormValues({ title: template.title, body: template.body });
    setFormError(null);
    setAttemptedSubmit(false);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
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
      if (editing) {
        await updateQuickReply(editing.id, formValues);
      } else {
        await createQuickReply(formValues);
      }
      closeForm();
      loadData();
    } catch {
      setFormError(t('quickReplies.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (template: QuickReplyTemplate) => {
    if (!window.confirm(t('quickReplies.deleteConfirm'))) return;
    try {
      await deleteQuickReply(template.id);
      loadData();
    } catch {
      setError(t('quickReplies.deleteFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('quickReplies.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          {t('quickReplies.new')}
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {templates.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('quickReplies.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('quickReplies.form.title')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('quickReplies.form.body')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500" />
                </tr>
              </thead>
              <tbody>
                {templates.map((template) => (
                  <tr
                    key={template.id}
                    className="border-b border-slate-100 transition-colors last:border-0 hover:bg-slate-50"
                  >
                    <td className="px-4 py-3 text-slate-800">{template.title}</td>
                    <td className="max-w-md truncate px-4 py-3 text-slate-600">{template.body}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEditForm(template)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
                        >
                          {t('customers.edit')}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(template)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-red-600 transition-colors hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-600"
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
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 px-4"
          onClick={closeForm}
        >
          <form
            onSubmit={handleSubmit}
            onClick={(e) => e.stopPropagation()}
            noValidate
            className="flex w-full max-w-sm flex-col gap-4 rounded bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-slate-800">
              {editing ? t('quickReplies.title') : t('quickReplies.new')}
            </h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('quickReplies.form.title')}</span>
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
              <span>{t('quickReplies.form.body')}</span>
              <textarea
                value={formValues.body}
                onChange={(e) => setFormValues((v) => ({ ...v, body: e.target.value }))}
                rows={4}
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
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
              >
                {t('quickReplies.form.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
              >
                {t('quickReplies.form.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
