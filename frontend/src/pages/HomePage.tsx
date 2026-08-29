import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { httpClient } from '../api/httpClient';
import { useBranding } from '../hooks/useBranding';

type HealthStatus = 'unknown' | 'unreachable' | string;

export default function HomePage() {
  const { t } = useTranslation();
  const { appName } = useBranding();
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
    <div className="flex flex-col items-center justify-center gap-4 py-12 text-center">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{appName}</h1>
      <p className="text-slate-600">
        {t('home.backendHealth')}: {healthLabel}
      </p>
    </div>
  );
}
