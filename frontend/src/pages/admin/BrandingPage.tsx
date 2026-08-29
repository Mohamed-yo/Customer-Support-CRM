import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { getBranding, updateBranding } from '../../api/branding';

const MAX_LOGO_BYTES = 256 * 1024;

function readFileAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

export default function BrandingPage() {
  const { t } = useTranslation();

  const [appName, setAppName] = useState('');
  const [logoDataUrl, setLogoDataUrl] = useState<string | null>(null);
  const [primaryColorHex, setPrimaryColorHex] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  useEffect(() => {
    getBranding()
      .then((data) => {
        setAppName(data.appName);
        setLogoDataUrl(data.logoDataUrl);
        setPrimaryColorHex(data.primaryColorHex);
      })
      .catch(() => setError(t('admin.branding.loadFailed')))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleLogoChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    setError(null);
    if (file.size > MAX_LOGO_BYTES) {
      setError(t('admin.branding.logoTooLarge'));
      return;
    }
    try {
      const dataUrl = await readFileAsDataUrl(file);
      setLogoDataUrl(dataUrl);
    } catch {
      setError(t('admin.branding.saveFailed'));
    }
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setSaveMessage(null);
    setSaving(true);
    try {
      await updateBranding({ appName: appName.trim(), logoDataUrl, primaryColorHex });
      setSaveMessage(t('admin.runtime.saved'));
    } catch (err: any) {
      const code = err?.response?.data?.error;
      setError(
        code === 'logo_too_large'
          ? t('admin.branding.logoTooLarge')
          : code === 'primary_color_invalid'
            ? t('admin.branding.primaryColorInvalid')
            : t('admin.branding.saveFailed'),
      );
    } finally {
      setSaving(false);
    }
  };

  if (loading) return null;

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('admin.branding.title')}</h1>

      <form onSubmit={handleSubmit} className="flex max-w-lg flex-col gap-4 rounded border border-slate-200 bg-white p-6">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.branding.appName')}</span>
          <input
            type="text"
            value={appName}
            onChange={(e) => setAppName(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        <div className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.branding.logo')}</span>
          <div className="flex items-center gap-3">
            {logoDataUrl && <img src={logoDataUrl} alt="" className="h-10 w-10 rounded border border-slate-200 object-contain" />}
            <input type="file" accept="image/png,image/jpeg" onChange={handleLogoChange} className="text-sm text-slate-700" />
            {logoDataUrl && (
              <button
                type="button"
                onClick={() => setLogoDataUrl(null)}
                className="text-sm font-medium text-red-600 hover:underline"
              >
                {t('admin.branding.remove')}
              </button>
            )}
          </div>
          <span className="text-xs text-slate-500">{t('admin.branding.logoHint')}</span>
        </div>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('admin.branding.primaryColor')}</span>
          <div className="flex items-center gap-3">
            <input
              type="color"
              value={primaryColorHex ?? '#1e293b'}
              onChange={(e) => setPrimaryColorHex(e.target.value)}
              className="h-9 w-14 rounded border border-slate-300 bg-white"
            />
            {primaryColorHex && (
              <button
                type="button"
                onClick={() => setPrimaryColorHex(null)}
                className="text-sm font-medium text-red-600 hover:underline"
              >
                {t('admin.branding.resetColor')}
              </button>
            )}
          </div>
        </label>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {saveMessage && <p className="text-sm text-green-600">{saveMessage}</p>}

        <button
          type="submit"
          disabled={saving}
          className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {t('admin.branding.save')}
        </button>
      </form>
    </div>
  );
}
