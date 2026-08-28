import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { loginCustomer } from '../../api/portal';
import { useCustomerAuthStore } from '../../store/useCustomerAuthStore';
import PageContainer from '../../components/layout/PageContainer';
import LanguageSwitcher from '../../components/LanguageSwitcher';

type LoginError = 'invalidCredentials' | 'network' | null;

export default function PortalLoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const setSession = useCustomerAuthStore((s) => s.setSession);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<LoginError>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const response = await loginCustomer(email, password);
      setSession({
        token: response.token,
        customer: { id: response.customerId, email: response.email, fullName: response.fullName },
        expiresAtUtc: response.expiresAtUtc,
      });
      navigate('/portal/my-requests', { replace: true });
    } catch (err) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      setError(status === 401 ? 'invalidCredentials' : 'network');
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
        {t('portal.login.title')}
      </h1>
      <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('portal.login.email')}</span>
          <input
            type="email"
            required
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('portal.login.password')}</span>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        {error && <p className="text-sm text-red-600 text-center">{t(`portal.login.errors.${error}`)}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="rounded bg-slate-800 text-white px-4 py-2 font-medium disabled:opacity-60"
        >
          {submitting ? t('portal.login.submitting') : t('portal.login.submit')}
        </button>

        <Link to="/portal/register" className="text-center text-sm text-slate-600 underline">
          {t('portal.login.registerLink')}
        </Link>
      </form>
    </PageContainer>
  );
}
