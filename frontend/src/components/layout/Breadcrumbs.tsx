import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { BREADCRUMB_ROUTES, matchBreadcrumbRoute } from '../../routes/breadcrumbs';

interface Props {
  currentDynamicLabel: string | null;
}

interface Crumb {
  path: string;
  labelKey: string;
}

// Rendered once by AppShell.tsx, above <Outlet />. Reads route hierarchy from
// BREADCRUMB_ROUTES via the current pathname (matchBreadcrumbRoute) - this app uses
// <BrowserRouter><Routes> in declarative mode, not a data router, so React Router's
// useMatches() is not available (it throws outside a data router). Routes are flat
// siblings in AppRouter.tsx, so ancestors are resolved by walking parentPath through
// BREADCRUMB_ROUTES, not by any route nesting. No page under frontend/src/pages/
// implements any breadcrumb rendering of its own.
export default function Breadcrumbs({ currentDynamicLabel }: Props) {
  const { t } = useTranslation();
  const location = useLocation();

  const currentMatch = matchBreadcrumbRoute(location.pathname);
  if (!currentMatch) return null;
  const currentHandle = currentMatch.handle;

  // Walk parentPath upward (ancestors are NOT nested routes here, just a declared chain),
  // stopping at a top-level page (parentPath === null) or an orphaned/typo'd parentPath.
  const chain: Crumb[] = [{ path: location.pathname, labelKey: currentHandle.labelKey }];
  let parentPath = currentHandle.parentPath;
  while (parentPath !== null) {
    const parentHandle = BREADCRUMB_ROUTES[parentPath];
    if (!parentHandle) break;
    chain.push({ path: parentPath, labelKey: parentHandle.labelKey });
    parentPath = parentHandle.parentPath;
  }
  chain.reverse();

  const lastIndex = chain.length - 1;
  const parentLabelKey = lastIndex > 0 ? chain[lastIndex - 1].labelKey : currentHandle.labelKey;
  // Never the raw :id: while a dynamic label hasn't been published yet, fall back to the
  // parent's translated label rather than any route parameter.
  const currentLabel = currentHandle.dynamic ? (currentDynamicLabel ?? t(parentLabelKey)) : t(currentHandle.labelKey);

  return (
    <nav aria-label={t('shell.breadcrumb.ariaLabel')} className="flex flex-wrap items-center gap-2 text-sm text-slate-500">
      {chain.map((crumb, index) => (
        <span key={crumb.path} className="flex items-center gap-2">
          {index > 0 && <span aria-hidden="true">{t('shell.breadcrumb.separator')}</span>}
          {index === lastIndex ? (
            <span aria-current="page" className="font-medium text-slate-900">
              {currentLabel}
            </span>
          ) : (
            <Link to={crumb.path} className="transition-colors hover:text-slate-800 hover:underline">
              {t(crumb.labelKey)}
            </Link>
          )}
        </span>
      ))}
    </nav>
  );
}
