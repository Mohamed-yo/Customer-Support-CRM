import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type AgentPerformanceReport, getAgentPerformance } from '../../api/reports';
import DateRangeFilter from '../../components/reports/DateRangeFilter';

export default function AgentReportPage() {
  const { t } = useTranslation();

  const [report, setReport] = useState<AgentPerformanceReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const load = (fromUtc?: string, toUtc?: string) => {
    setLoading(true);
    setError(null);
    getAgentPerformance({ fromUtc: fromUtc || null, toUtc: toUtc || null })
      .then(setReport)
      .catch(() => setError(t('reports.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('reports.agents.title')}</h1>

      <DateRangeFilter
        from={from}
        to={to}
        onFromChange={setFrom}
        onToChange={setTo}
        onApply={() => load(from, to)}
        onClear={() => {
          setFrom('');
          setTo('');
          load();
        }}
      />

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {report && report.agents.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('reports.agents.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.agent')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.open')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.inProgress')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.closed')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.averageResolutionMinutes')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {report?.agents.map((agent) => (
                  <tr key={agent.userId} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 text-slate-800">{agent.displayName}</td>
                    <td className="px-4 py-3 text-slate-600">{agent.open}</td>
                    <td className="px-4 py-3 text-slate-600">{agent.inProgress}</td>
                    <td className="px-4 py-3 text-slate-600">{agent.closed}</td>
                    <td className="px-4 py-3 text-slate-600">{agent.averageResolutionMinutes.toFixed(1)}</td>
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
