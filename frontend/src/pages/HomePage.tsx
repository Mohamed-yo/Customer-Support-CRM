import { useEffect, useState } from 'react';
import { httpClient } from '../api/httpClient';
import { useAppStore } from '../store/useAppStore';

export default function HomePage() {
  const appName = useAppStore((s) => s.appName);
  const [health, setHealth] = useState<string>('unknown');

  useEffect(() => {
    httpClient
      .get('/api/health')
      .then((res) => setHealth(res.data?.status ?? 'unknown'))
      .catch(() => setHealth('unreachable'));
  }, []);

  return (
    <main className="min-h-screen flex flex-col items-center justify-center gap-4 bg-slate-50">
      <h1 className="text-3xl font-semibold text-slate-800">{appName}</h1>
      <p className="text-slate-600">Backend health: {health}</p>
    </main>
  );
}
