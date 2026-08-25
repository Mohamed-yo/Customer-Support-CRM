import AppRouter from './routes/AppRouter';
import LanguageProvider from './i18n/LanguageProvider';

export default function App() {
  return (
    <LanguageProvider>
      <AppRouter />
    </LanguageProvider>
  );
}
