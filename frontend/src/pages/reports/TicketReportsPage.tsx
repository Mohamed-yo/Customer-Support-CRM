import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type TicketCountsReport, getTicketCounts } from '../../api/reports';
import DateRangeFilter from '../../components/reports/DateRangeFilter';

function CountTable({ title, data }: { title: string; data: Record<string, number> }) {
  return (
    <div className="rounded border border-slate-200 bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{title}</h2>
      <table className="w-full text-start text-sm">
        <tbody>
          {Object.entries(data).map(([key, count]) => (
            <tr key={key} className="border-b border-slate-100 last:border-0">
              <td className="py-1.5 text-slate-700">{key}</td>
              <td className="py-1.5 text-end font-medium text-slate-800">{count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default function TicketReportsPage() {
  const { t } = useTranslation();

  const [report, setReport] = useState<TicketCountsReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const load = (fromUtc?: string, toUtc?: string) => {
    setLoading(true);
    setError(null);
    getTicketCounts({ fromUtc: fromUtc || null, toUtc: toUtc || null })
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
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('reports.tickets.title')}</h1>

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
        report && (
          <>
            <div className="rounded border border-slate-200 bg-white p-4">
              <p className="text-sm text-slate-500">{t('reports.tickets.total')}</p>
              <p className="text-3xl font-semibold text-slate-800">{report.total}</p>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <CountTable title={t('reports.tickets.byStatus')} data={report.byStatus} />
              <CountTable title={t('reports.tickets.byCategory')} data={report.byCategory} />
              <CountTable title={t('reports.tickets.byPriority')} data={report.byPriority} />
              <CountTable title={t('reports.tickets.bySource')} data={report.bySource} />
            </div>
          </>
        )
      )}
    </div>
  );
}
