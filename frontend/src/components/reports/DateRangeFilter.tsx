import { useTranslation } from 'react-i18next';

interface Props {
  from: string;
  to: string;
  onFromChange: (value: string) => void;
  onToChange: (value: string) => void;
  onApply: () => void;
  onClear: () => void;
}

export default function DateRangeFilter({ from, to, onFromChange, onToChange, onApply, onClear }: Props) {
  const { t } = useTranslation();

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onApply();
      }}
      className="flex flex-wrap items-end gap-3"
    >
      <label className="flex flex-col gap-1 text-sm text-slate-700">
        <span>{t('reports.dateRange.from')}</span>
        <input
          type="date"
          value={from}
          onChange={(e) => onFromChange(e.target.value)}
          className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm text-slate-700">
        <span>{t('reports.dateRange.to')}</span>
        <input
          type="date"
          value={to}
          onChange={(e) => onToChange(e.target.value)}
          className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
        />
      </label>
      <button
        type="submit"
        className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700"
      >
        {t('reports.dateRange.apply')}
      </button>
      <button
        type="button"
        onClick={onClear}
        className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
      >
        {t('reports.dateRange.allTime')}
      </button>
    </form>
  );
}
