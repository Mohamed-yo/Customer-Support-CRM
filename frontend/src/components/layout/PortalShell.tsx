import { useEffect, useRef, useState, type CSSProperties } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useCustomerAuthStore } from '../../store/useCustomerAuthStore';
import { useBranding } from '../../hooks/useBranding';
import LanguageToggle from '../LanguageToggle';
import AiChatWidget from '../AiChatWidget';

const NAV_ITEMS: Array<{ to: string; labelKey: string }> = [
  { to: '/portal/submit-ticket', labelKey: 'portal.nav.submitTicket' },
  { to: '/portal/my-requests', labelKey: 'portal.nav.myRequests' },
  { to: '/portal/knowledge-base', labelKey: 'portal.nav.knowledgeBase' },
];


const navLinkClassName = ({ isActive }: { isActive: boolean }) =>
  `rounded px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800 ${
    isActive ? 'bg-[var(--brand-primary,#1e293b)] text-white' : 'text-slate-700 hover:bg-slate-100 active:bg-slate-200'
  }`;

export default function PortalShell() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { appName, logoDataUrl, primaryColorHex } = useBranding();
  const customer = useCustomerAuthStore((s) => s.customer);
  const clearSession = useCustomerAuthStore((s) => s.clearSession);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);

  const [lastPathname, setLastPathname] = useState(location.pathname);
  if (location.pathname !== lastPathname) {
    setLastPathname(location.pathname);
    setDrawerOpen(false);
  }

  useEffect(() => {
    if (!drawerOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setDrawerOpen(false);
        menuButtonRef.current?.focus();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [drawerOpen]);

  const handleLogout = () => {
    clearSession();
    navigate('/portal/login', { replace: true });
  };

  return (
    <div
      className="flex min-h-screen bg-slate-50"
      style={primaryColorHex ? ({ '--brand-primary': primaryColorHex } as CSSProperties) : undefined}
    >
      {drawerOpen && (
        <div
          className="fixed inset-0 z-30 bg-slate-900/50 lg:hidden"
          onClick={() => setDrawerOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        className={`${drawerOpen ? 'flex' : 'hidden'} lg:flex fixed inset-y-0 start-0 z-40 w-64 flex-col gap-6 bg-white px-4 py-6 shadow-sm lg:static`}
        aria-label={t('shell.sidebar')}
      >
        <div className="flex items-center gap-2">
          {logoDataUrl && <img src={logoDataUrl} alt="" className="h-7 w-7 rounded object-contain" />}
          <span className="text-lg font-semibold text-slate-800">{appName}</span>
        </div>
        <span className="-mt-4 text-xs font-medium uppercase tracking-wide text-slate-400">
          {t('portal.title')}
        </span>

        <nav aria-label="Primary" className="flex flex-col gap-1">
          {NAV_ITEMS.map((item) => (
            <NavLink key={item.to} to={item.to} className={navLinkClassName}>
              {t(item.labelKey)}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto flex flex-col gap-4 border-t border-slate-200 pt-4">
          <div className="flex flex-col gap-0.5 text-sm">
            <span className="font-medium text-slate-800">{customer?.fullName}</span>
            <span className="text-slate-500">{customer?.email}</span>
          </div>

          <button
            type="button"
            onClick={handleLogout}
            className="rounded border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
          >
            {t('auth.logout')}
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-3">
          <div className="flex items-center gap-3">
            <button
              ref={menuButtonRef}
              type="button"
              onClick={() => setDrawerOpen((open) => !open)}
              aria-label={drawerOpen ? t('shell.closeMenu') : t('shell.openMenu')}
              aria-expanded={drawerOpen}
              className="rounded p-2 text-slate-700 hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800 lg:hidden"
            >
              <MenuIcon open={drawerOpen} />
            </button>
            <span className="text-base font-semibold text-slate-800 lg:text-lg">{appName}</span>
          </div>

          <div className="flex items-center gap-2">
            <LanguageToggle />
            <button
              type="button"
              onClick={handleLogout}
              aria-label={t('auth.logout')}
              title={t('auth.logout')}
              className="rounded p-2 text-slate-700 transition-colors hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
            >
              <LogoutIcon />
            </button>
          </div>
        </header>

        <main className="flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
          <div className="mx-auto w-full max-w-5xl">
            <Outlet />
          </div>
        </main>
      </div>

      <AiChatWidget />
    </div>
  );
}

function MenuIcon({ open }: { open: boolean }) {
  if (open) {
    return (
      <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
        <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
      </svg>
    );
  }
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M4 7h16M4 12h16M4 17h16" strokeLinecap="round" />
    </svg>
  );
}

function LogoutIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M16 17l5-5-5-5M21 12H9" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
