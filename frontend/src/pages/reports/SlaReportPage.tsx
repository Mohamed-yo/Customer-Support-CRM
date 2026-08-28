import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type SlaPerformanceReport, getSlaPerformance } from '../../api/reports';
import DateRangeFilter from '../../components/reports/DateRangeFilter';

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-slate-200 bg-white p-4">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="text-2xl font-semibold text-slate-800">{value}</p>
    </div>
  );
}

export default function SlaReportPage() {
  const { t } = useTranslation();

  const [report, setReport] = useState<SlaPerformanceReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const load = (fromUtc?: string, toUtc?: string) => {
    setLoading(true);
    setError(null);
    getSlaPerformance({ fromUtc: fromUtc || null, toUtc: toUtc || null })
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
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('reports.sla.title')}</h1>

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
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <StatCard label={t('reports.sla.responseMetPercent')} value={`${report.responseMetPercent.toFixed(1)}%`} />
            <StatCard label={t('reports.sla.resolutionMetPercent')} value={`${report.resolutionMetPercent.toFixed(1)}%`} />
            <StatCard label={t('reports.sla.escalatedCount')} value={String(report.escalatedCount)} />
            <StatCard label={t('reports.sla.averageResponseMinutes')} value={report.averageResponseMinutes.toFixed(1)} />
            <StatCard label={t('reports.sla.averageResolutionMinutes')} value={report.averageResolutionMinutes.toFixed(1)} />
            <StatCard
              label={t('reports.sla.responseBreakdown')}
              value={`${report.responseMet} / ${report.responseBreached}`}
            />
            <StatCard
              label={t('reports.sla.resolutionBreakdown')}
              value={`${report.resolutionMet} / ${report.resolutionBreached}`}
            />
          </div>
        )
      )}
    </div>
  );
}
