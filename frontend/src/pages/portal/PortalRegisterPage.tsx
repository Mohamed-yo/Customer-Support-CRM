import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { registerCustomer } from '../../api/portal';
import PageContainer from '../../components/layout/PageContainer';
import LanguageSwitcher from '../../components/LanguageSwitcher';

// Same practical email check used by the staff CustomersPage form.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormValues {
  fullName: string;
  email: string;
  phone: string;
}

const EMPTY_FORM: FormValues = { fullName: '', email: '', phone: '' };

interface FormErrors {
  fullName?: string;
  email?: string;
}

function validateForm(values: FormValues): FormErrors {
  const errors: FormErrors = {};

  if (!values.fullName.trim()) {
    errors.fullName = 'portal.register.errors.nameRequired';
  }

  const email = values.email.trim();
  if (!email) {
    errors.email = 'portal.register.errors.emailRequired';
  } else if (!EMAIL_PATTERN.test(email)) {
    errors.email = 'portal.register.errors.emailInvalid';
  }

  return errors;
}

export default function PortalRegisterPage() {
  const { t } = useTranslation();

  const [values, setValues] = useState<FormValues>(EMPTY_FORM);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  // The email the verification link was sent to; the form is replaced by a "check your
  // inbox" message once set. The password is chosen on PortalVerifyEmailPage.
  const [sentTo, setSentTo] = useState<string | null>(null);

  const errors = validateForm(values);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    if (Object.keys(errors).length > 0) {
      setAttemptedSubmit(true);
      return;
    }

    setSubmitting(true);
    try {
      const email = values.email.trim();
      await registerCustomer({
        fullName: values.fullName.trim(),
        email,
        phone: values.phone.trim() || null,
      });
      setSentTo(email);
    } catch (err) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      setSubmitError(status === 409 ? t('portal.register.errors.emailTaken') : t('portal.register.errors.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <PageContainer>
      <div className="w-full flex justify-end">
        <LanguageSwitcher />
      </div>
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800 text-center">
        {t('portal.register.title')}
      </h1>
      {sentTo ? (
        <div className="w-full max-w-sm flex flex-col gap-4">
          <p className="text-sm text-slate-700 text-center">{t('portal.register.checkEmail', { email: sentTo })}</p>
          <Link to="/portal/login" className="text-center text-sm text-slate-600 underline">
            {t('portal.register.loginLink')}
          </Link>
        </div>
      ) : (
        <form onSubmit={handleSubmit} noValidate className="w-full max-w-sm flex flex-col gap-4">
          <label className="flex flex-col gap-1 text-sm text-slate-700">
            <span>{t('portal.register.fullName')}</span>
            <input
              type="text"
              value={values.fullName}
              onChange={(e) => setValues((v) => ({ ...v, fullName: e.target.value }))}
              className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
            />
            {attemptedSubmit && errors.fullName && <span className="text-sm text-red-600">{t(errors.fullName)}</span>}
          </label>

          <label className="flex flex-col gap-1 text-sm text-slate-700">
            <span>{t('portal.register.email')}</span>
            <input
              type="email"
              autoComplete="username"
              value={values.email}
              onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))}
              className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
            />
            {attemptedSubmit && errors.email && <span className="text-sm text-red-600">{t(errors.email)}</span>}
          </label>

          <label className="flex flex-col gap-1 text-sm text-slate-700">
            <span>{t('portal.register.phone')}</span>
            <input
              type="text"
              value={values.phone}
              onChange={(e) => setValues((v) => ({ ...v, phone: e.target.value }))}
              className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
            />
          </label>

          {submitError && <p className="text-sm text-red-600 text-center">{submitError}</p>}

          <button
            type="submit"
            disabled={submitting}
            className="rounded bg-slate-800 text-white px-4 py-2 font-medium disabled:opacity-60"
          >
            {submitting ? t('portal.register.submitting') : t('portal.register.submit')}
          </button>

          <Link to="/portal/login" className="text-center text-sm text-slate-600 underline">
            {t('portal.register.loginLink')}
          </Link>
        </form>
      )}
    </PageContainer>
  );
}
