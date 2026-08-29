import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { type AuditLogListItem, listAuditLogs } from '../../api/admin';

export default function AuditLogPage() {
  const { t } = useTranslation();

  const [items, setItems] = useState<AuditLogListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [action, setAction] = useState('');
  const [fromUtc, setFromUtc] = useState('');
  const [toUtc, setToUtc] = useState('');

  const load = (filters?: { action?: string; fromUtc?: string; toUtc?: string }) => {
    setLoading(true);
    setError(null);
    listAuditLogs({
      action: filters?.action || undefined,
      fromUtc: filters?.fromUtc || undefined,
      toUtc: filters?.toUtc || undefined,
      pageSize: 200,
      page: 1,
    })
      .then(setItems)
      .catch(() => setError(t('admin.audit.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleFilterSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    load({ action: action.trim(), fromUtc, toUtc });
  };

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('admin.audit.title')}</h1>

      <form onSubmit={handleFilterSubmit} className="flex flex-wrap items-end gap-3">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.audit.filters.action')}</span>
          <input
            type="text"
            value={action}
            onChange={(e) => setAction(e.target.value)}
            placeholder={t('admin.audit.filters.actionPlaceholder') ?? ''}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
          />
        </label>
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.audit.filters.from')}</span>
          <input
            type="datetime-local"
            value={fromUtc}
            onChange={(e) => setFromUtc(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
          />
        </label>
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.audit.filters.to')}</span>
          <input
            type="datetime-local"
            value={toUtc}
            onChange={(e) => setToUtc(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
          />
        </label>
        <button type="submit" className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">
          {t('admin.audit.filters.apply')}
        </button>
      </form>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {items.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('admin.audit.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.audit.columns.timestamp')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.audit.columns.action')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.audit.columns.outcome')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.audit.columns.actor')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.audit.columns.details')}</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 text-slate-600">{new Date(item.timestampUtc).toLocaleString()}</td>
                    <td className="px-4 py-3 font-mono text-slate-800">{item.action}</td>
                    <td className="px-4 py-3 text-slate-600">{item.outcome}</td>
                    <td className="px-4 py-3 text-slate-600">{item.actorEmail ?? '—'}</td>
                    <td className="max-w-xs truncate px-4 py-3 text-slate-600">{item.details ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
}
