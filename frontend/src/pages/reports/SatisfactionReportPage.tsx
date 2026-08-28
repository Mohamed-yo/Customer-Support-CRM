import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type SatisfactionReport, getSatisfaction } from '../../api/reports';
import DateRangeFilter from '../../components/reports/DateRangeFilter';

function BreakdownTable({ title, data }: { title: string; data: Record<string, number> }) {
  return (
    <div className="rounded border border-slate-200 bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{title}</h2>
      <table className="w-full text-start text-sm">
        <tbody>
          {Object.entries(data).map(([key, average]) => (
            <tr key={key} className="border-b border-slate-100 last:border-0">
              <td className="py-1.5 text-slate-700">{key}</td>
              <td className="py-1.5 text-end font-medium text-slate-800">{average.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default function SatisfactionReportPage() {
  const { t } = useTranslation();

  const [report, setReport] = useState<SatisfactionReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const load = (fromUtc?: string, toUtc?: string) => {
    setLoading(true);
    setError(null);
    getSatisfaction({ fromUtc: fromUtc || null, toUtc: toUtc || null })
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
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('reports.satisfaction.title')}</h1>

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
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div className="rounded border border-slate-200 bg-white p-4">
                <p className="text-sm text-slate-500">{t('reports.satisfaction.averageRating')}</p>
                <p className="text-3xl font-semibold text-slate-800">{report.averageRating.toFixed(2)}</p>
              </div>
              <div className="rounded border border-slate-200 bg-white p-4">
                <p className="text-sm text-slate-500">{t('reports.satisfaction.feedbackCount')}</p>
                <p className="text-3xl font-semibold text-slate-800">{report.feedbackCount}</p>
              </div>
              <div className="rounded border border-slate-200 bg-white p-4">
                <p className="text-sm text-slate-500">{t('reports.satisfaction.responseRate')}</p>
                <p className="text-3xl font-semibold text-slate-800">{report.responseRatePercent.toFixed(1)}%</p>
              </div>
            </div>

            <div className="rounded border border-slate-200 bg-white p-4">
              <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
                {t('reports.satisfaction.distribution')}
              </h2>
              <table className="w-full text-start text-sm">
                <tbody>
                  {report.distribution.map((entry) => (
                    <tr key={entry.rating} className="border-b border-slate-100 last:border-0">
                      <td className="py-1.5 text-slate-700">{entry.rating}</td>
                      <td className="py-1.5 text-end font-medium text-slate-800">{entry.count}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <BreakdownTable
                title={t('reports.satisfaction.byCategory')}
                data={report.averageRatingByCategory}
              />
              <BreakdownTable title={t('reports.satisfaction.byAgent')} data={report.averageRatingByAgent} />
            </div>
          </>
        )
      )}
    </div>
  );
}
