import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { TICKET_PRIORITIES } from '../../api/tickets';
import { type SlaTargets, getSlaTargets, updateSlaTargets } from '../../api/runtimeSettings';

export default function SlaTargetsPage() {
  const { t } = useTranslation();

  const [targets, setTargets] = useState<SlaTargets>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  useEffect(() => {
    getSlaTargets()
      .then(setTargets)
      .catch(() => setError(t('admin.slaTargets.loadFailed')))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleChange = (priority: string, field: 'responseHours' | 'resolutionHours', value: string) => {
    const parsed = Number(value);
    setTargets((prev) => ({
      ...prev,
      [priority]: { ...prev[priority], [field]: parsed },
    }));
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSaveMessage(null);

    const invalid = TICKET_PRIORITIES.some((p) => {
      const target = targets[p];
      return !target || !(target.responseHours > 0) || !(target.resolutionHours > 0);
    });
    if (invalid) {
      setError(t('admin.slaTargets.invalid'));
      return;
    }

    setSaving(true);
    try {
      await updateSlaTargets(targets);
      setSaveMessage(t('admin.runtime.saved'));
    } catch {
      setError(t('admin.slaTargets.saveFailed'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return null;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('admin.slaTargets.title')}</h1>
        <p className="mt-1 text-sm text-slate-600">{t('admin.slaTargets.description')}</p>
      </div>

      <form onSubmit={handleSubmit} className="flex max-w-xl flex-col gap-4">
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          <table className="w-full text-start text-sm">
            <thead className="bg-slate-50">
              <tr className="border-b border-slate-200">
                <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                  {t('admin.slaTargets.columns.priority')}
                </th>
                <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                  {t('admin.slaTargets.columns.response')}
                </th>
                <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                  {t('admin.slaTargets.columns.resolution')}
                </th>
              </tr>
            </thead>
            <tbody>
              {TICKET_PRIORITIES.map((priority) => (
                <tr key={priority} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-3 text-slate-800">{t(`tickets.priority.${priority}`)}</td>
                  <td className="px-4 py-3">
                    <input
                      type="number"
                      min="0"
                      step="0.5"
                      value={targets[priority]?.responseHours ?? ''}
                      onChange={(e) => handleChange(priority, 'responseHours', e.target.value)}
                      className="w-24 rounded border border-slate-300 bg-white px-2 py-1 text-slate-800"
                    />
                  </td>
                  <td className="px-4 py-3">
                    <input
                      type="number"
                      min="0"
                      step="0.5"
                      value={targets[priority]?.resolutionHours ?? ''}
                      onChange={(e) => handleChange(priority, 'resolutionHours', e.target.value)}
                      className="w-24 rounded border border-slate-300 bg-white px-2 py-1 text-slate-800"
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {saveMessage && <p className="text-sm text-green-600">{saveMessage}</p>}

        <button
          type="submit"
          disabled={saving}
          className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {t('admin.slaTargets.save')}
        </button>
      </form>
    </div>
  );
}
