import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { login } from '../api/auth';
import { useAuthStore } from '../store/useAuthStore';
import PageContainer from '../components/layout/PageContainer';
import LanguageSwitcher from '../components/LanguageSwitcher';

type LoginError = 'invalidCredentials' | 'network' | null;

export default function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<LoginError>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const response = await login(email, password);
      setSession({
        token: response.token,
        user: { id: response.id, email: response.email, displayName: response.displayName, roles: response.roles },
        expiresAtUtc: response.expiresAtUtc,
      });
      navigate('/', { replace: true });
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
        {t('auth.login.title')}
      </h1>
      <form onSubmit={handleSubmit} className="w-full max-w-sm flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('auth.login.email')}</span>
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
          <span>{t('auth.login.password')}</span>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        {error && <p className="text-sm text-red-600 text-center">{t(`auth.errors.${error}`)}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="rounded bg-slate-800 text-white px-4 py-2 font-medium disabled:opacity-60"
        >
          {submitting ? t('auth.login.submitting') : t('auth.login.submit')}
        </button>
      </form>
    </PageContainer>
  );
}
