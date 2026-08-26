import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store/useAppStore';

export default function LanguageToggle() {
  const { t } = useTranslation();
  const language = useAppStore((s) => s.language);
  const setLanguage = useAppStore((s) => s.setLanguage);
  const nextLanguage = language === 'en' ? 'ar' : 'en';

  return (
    <button
      type="button"
      onClick={() => setLanguage(nextLanguage)}
      aria-label={
        nextLanguage === 'ar' ? t('shell.languageToggle.switchToArabic') : t('shell.languageToggle.switchToEnglish')
      }
      title={
        nextLanguage === 'ar' ? t('shell.languageToggle.switchToArabic') : t('shell.languageToggle.switchToEnglish')
      }
      className="flex items-center gap-1.5 rounded px-2 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
    >
      <GlobeIcon />
      <span>{language.toUpperCase()}</span>
    </button>
  );
}

function GlobeIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path d="M3 12h18M12 3c2.5 2.5 3.75 5.5 3.75 9S14.5 18.5 12 21c-2.5-2.5-3.75-5.5-3.75-9S9.5 5.5 12 3Z" />
    </svg>
  );
}
