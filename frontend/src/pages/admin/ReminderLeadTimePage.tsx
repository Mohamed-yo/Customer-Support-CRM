import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { getReminderLeadTime, updateReminderLeadTime } from '../../api/runtimeSettings';

export default function ReminderLeadTimePage() {
  const { t } = useTranslation();

  const [hours, setHours] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  useEffect(() => {
    getReminderLeadTime()
      .then((data) => setHours(String(data.hours)))
      .catch(() => setError(t('admin.reminderLeadTime.loadFailed')))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSaveMessage(null);

    const parsed = Number(hours);
    if (!(parsed > 0)) {
      setError(t('admin.reminderLeadTime.invalid'));
      return;
    }

    setSaving(true);
    try {
      await updateReminderLeadTime({ hours: parsed });
      setSaveMessage(t('admin.runtime.saved'));
    } catch {
      setError(t('admin.reminderLeadTime.saveFailed'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return null;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('admin.reminderLeadTime.title')}</h1>
        <p className="mt-1 text-sm text-slate-600">{t('admin.reminderLeadTime.description')}</p>
      </div>

      <form onSubmit={handleSubmit} className="flex max-w-xs flex-col gap-4 rounded border border-slate-200 bg-white p-6">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.reminderLeadTime.hours')}</span>
          <input
            type="number"
            min="0"
            step="0.5"
            value={hours}
            onChange={(e) => setHours(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {saveMessage && <p className="text-sm text-green-600">{saveMessage}</p>}

        <button
          type="submit"
          disabled={saving}
          className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {t('admin.reminderLeadTime.save')}
        </button>
      </form>
    </div>
  );
}
