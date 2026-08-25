import { useTranslation } from 'react-i18next';
import { useAppStore, type Language } from '../store/useAppStore';
import { SUPPORTED_LANGUAGES } from '../i18n';

export default function LanguageSwitcher() {
  const { t } = useTranslation();
  const language = useAppStore((s) => s.language);
  const setLanguage = useAppStore((s) => s.setLanguage);

  return (
    <label className="flex items-center gap-2 text-sm text-slate-700">
      <span>{t('common.language')}</span>
      <select
        className="rounded border border-slate-300 bg-white px-2 py-1"
        value={language}
        onChange={(e) => setLanguage(e.target.value as Language)}
      >
        {SUPPORTED_LANGUAGES.map((code) => (
          <option key={code} value={code}>
            {code === 'en' ? t('common.languageEnglish') : t('common.languageArabic')}
          </option>
        ))}
      </select>
    </label>
  );
}
