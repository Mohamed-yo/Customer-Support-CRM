import { useLayoutEffect, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store/useAppStore';
import { isRtl } from './index';

type Props = { children: ReactNode };

export default function LanguageProvider({ children }: Props) {
  const language = useAppStore((s) => s.language);
  const { i18n } = useTranslation();

  useLayoutEffect(() => {
    if (i18n.language !== language) {
      void i18n.changeLanguage(language);
    }
    const root = document.documentElement;
    root.lang = language;
    root.dir = isRtl(language) ? 'rtl' : 'ltr';
  }, [language, i18n]);

  return <>{children}</>;
}
