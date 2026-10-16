import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { verifyCustomerEmail } from '../../api/portal';
import { useCustomerAuthStore } from '../../store/useCustomerAuthStore';
import PageContainer from '../../components/layout/PageContainer';
import LanguageSwitcher from '../../components/LanguageSwitcher';

interface FormErrors {
  password?: string;
  confirmPassword?: string;
}

function validateForm(password: string, confirmPassword: string): FormErrors {
  const errors: FormErrors = {};
  if (password.length < 8) {
    errors.password = 'portal.verifyEmail.errors.passwordTooShort';
  }
  if (confirmPassword !== password) {
    errors.confirmPassword = 'portal.verifyEmail.errors.passwordMismatch';
  }
  return errors;
}

// Landing page for the link emailed by portal registration: the customer proves they own
// the mailbox by opening it, and only then chooses a password.
export default function PortalVerifyEmailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const setSession = useCustomerAuthStore((s) => s.setSession);

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [tokenInvalid, setTokenInvalid] = useState(!token);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const errors = validateForm(password, confirmPassword);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    if (Object.keys(errors).length > 0) {
      setAttemptedSubmit(true);
      return;
    }

    setSubmitting(true);
    try {
      const response = await verifyCustomerEmail(token, password);
      setSession({
        token: response.token,
        customer: { id: response.customerId, email: response.email, fullName: response.fullName },
        expiresAtUtc: response.expiresAtUtc,
      });
      navigate('/portal/my-requests', { replace: true });
    } catch (err) {
      const error = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      if (error === 'token_invalid') {
        setTokenInvalid(true);
      } else {
        setSubmitError(t('portal.verifyEmail.errors.saveFailed'));
      }
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
        {t('portal.verifyEmail.title')}
      </h1>
      {tokenInvalid ? (
        <div className="w-full max-w-sm flex flex-col gap-4">
          <p className="text-sm text-red-600 text-center">{t('portal.verifyEmail.errors.tokenInvalid')}</p>
          <Link to="/portal/register" className="text-center text-sm text-slate-600 underline">
            {t('portal.verifyEmail.registerLink')}
          </Link>
        </div>
      ) : (
        <form onSubmit={handleSubmit} noValidate className="w-full max-w-sm flex flex-col gap-4">
          <label className="flex flex-col gap-1 text-sm text-slate-700">
            <span>{t('portal.verifyEmail.password')}</span>
            <input
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
            />
            {attemptedSubmit && errors.password && <span className="text-sm text-red-600">{t(errors.password)}</span>}
          </label>

          <label className="flex flex-col gap-1 text-sm text-slate-700">
            <span>{t('portal.verifyEmail.confirmPassword')}</span>
            <input
              type="password"
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
            />
            {attemptedSubmit && errors.confirmPassword && (
              <span className="text-sm text-red-600">{t(errors.confirmPassword)}</span>
            )}
          </label>

          {submitError && <p className="text-sm text-red-600 text-center">{submitError}</p>}

          <button
            type="submit"
            disabled={submitting}
            className="rounded bg-slate-800 text-white px-4 py-2 font-medium disabled:opacity-60"
          >
            {submitting ? t('portal.verifyEmail.submitting') : t('portal.verifyEmail.submit')}
          </button>
        </form>
      )}
    </PageContainer>
  );
}
