import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { httpClient } from '../api/httpClient';
import { useAppStore } from '../store/useAppStore';
import { useAuthStore } from '../store/useAuthStore';
import PageContainer from '../components/layout/PageContainer';
import LanguageSwitcher from '../components/LanguageSwitcher';

type HealthStatus = 'unknown' | 'unreachable' | string;

export default function HomePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const appName = useAppStore((s) => s.appName);
  const clearSession = useAuthStore((s) => s.clearSession);
  const [health, setHealth] = useState<HealthStatus>('unknown');

  useEffect(() => {
    httpClient
      .get('/api/health')
      .then((res) => setHealth(res.data?.status ?? 'unknown'))
      .catch(() => setHealth('unreachable'));
  }, []);

  const healthLabel =
    health === 'unknown'
      ? t('home.statusUnknown')
      : health === 'unreachable'
        ? t('home.statusUnreachable')
        : health;

  return (
    <PageContainer>
      <div className="w-full flex items-center justify-end gap-4">
        <LanguageSwitcher />
        <button
          type="button"
          onClick={() => {
            clearSession();
            navigate('/login', { replace: true });
          }}
          className="text-sm text-slate-700 underline"
        >
          {t('auth.logout')}
        </button>
      </div>
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800 text-center">
        {appName}
      </h1>
      <p className="text-slate-600 text-center">
        {t('home.backendHealth')}: {healthLabel}
      </p>
    </PageContainer>
  );
}
