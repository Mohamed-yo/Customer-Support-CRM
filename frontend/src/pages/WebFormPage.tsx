import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { submitWebForm } from '../api/channels';
import PageContainer from '../components/layout/PageContainer';
import LanguageSwitcher from '../components/LanguageSwitcher';

const PRIORITIES = ['Low', 'Normal', 'High', 'Urgent'] as const;

interface FormValues {
  fullName: string;
  email: string;
  phone: string;
  subject: string;
  description: string;
  priority: string;
}

const EMPTY_FORM: FormValues = { fullName: '', email: '', phone: '', subject: '', description: '', priority: 'Normal' };

// Same practical email check used elsewhere in this app (CustomersPage, PortalRegisterPage).
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export default function WebFormPage() {
  const { t } = useTranslation();

  const [values, setValues] = useState<FormValues>(EMPTY_FORM);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [reference, setReference] = useState<string | null>(null);

  const errors = {
    fullName: !values.fullName.trim() ? 'webform.errors.nameRequired' : null,
    email: !values.email.trim()
      ? 'webform.errors.emailRequired'
      : !EMAIL_PATTERN.test(values.email.trim())
        ? 'webform.errors.emailInvalid'
        : null,
    subject: !values.subject.trim() ? 'webform.errors.subjectRequired' : null,
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    if (Object.values(errors).some(Boolean)) {
      setAttemptedSubmit(true);
      return;
    }

    setSubmitting(true);
    try {
      const result = await submitWebForm({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        phone: values.phone.trim() || null,
        subject: values.subject.trim(),
        description: values.description.trim() || null,
        priority: values.priority,
      });
      setReference(result.referenceNumber);
    } catch {
      setSubmitError(t('webform.errors.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  if (reference) {
    return (
      <PageContainer>
        <div className="w-full flex justify-end">
          <LanguageSwitcher />
        </div>
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800 text-center">{t('webform.successTitle')}</h1>
        <p className="max-w-sm text-center text-sm text-slate-600">
          {t('webform.successBody')} <span className="font-mono font-semibold text-slate-800">#{reference}</span>
        </p>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <div className="w-full flex justify-end">
        <LanguageSwitcher />
      </div>
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800 text-center">{t('webform.title')}</h1>
      <form onSubmit={handleSubmit} noValidate className="w-full max-w-sm flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('webform.fullName')}</span>
          <input
            type="text"
            value={values.fullName}
            onChange={(e) => setValues((v) => ({ ...v, fullName: e.target.value }))}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
          {attemptedSubmit && errors.fullName && <span className="text-sm text-red-600">{t(errors.fullName)}</span>}
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('webform.email')}</span>
          <input
            type="email"
            value={values.email}
            onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
          {attemptedSubmit && errors.email && <span className="text-sm text-red-600">{t(errors.email)}</span>}
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('webform.phone')}</span>
          <input
            type="text"
            value={values.phone}
            onChange={(e) => setValues((v) => ({ ...v, phone: e.target.value }))}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('webform.subject')}</span>
          <input
            type="text"
            value={values.subject}
            onChange={(e) => setValues((v) => ({ ...v, subject: e.target.value }))}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
          {attemptedSubmit && errors.subject && <span className="text-sm text-red-600">{t(errors.subject)}</span>}
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('webform.description')}</span>
          <textarea
            value={values.description}
            onChange={(e) => setValues((v) => ({ ...v, description: e.target.value }))}
            rows={4}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('webform.priority')}</span>
          <select
            value={values.priority}
            onChange={(e) => setValues((v) => ({ ...v, priority: e.target.value }))}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          >
            {PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {t(`tickets.priority.${p}`)}
              </option>
            ))}
          </select>
        </label>

        {submitError && <p className="text-sm text-red-600 text-center">{submitError}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="rounded bg-slate-800 text-white px-4 py-2 font-medium disabled:opacity-60"
        >
          {t('webform.submit')}
        </button>
      </form>
    </PageContainer>
  );
}
