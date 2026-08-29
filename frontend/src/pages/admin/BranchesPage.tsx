import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type Branch,
  createBranch,
  deactivateBranch,
  listBranches,
  reactivateBranch,
  updateBranch,
} from '../../api/branches';

export default function BranchesPage() {
  const { t } = useTranslation();

  const [branches, setBranches] = useState<Branch[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [name, setName] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const loadData = () => {
    setLoading(true);
    setError(null);
    listBranches()
      .then(setBranches)
      .catch(() => setError(t('branches.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const openCreateForm = () => {
    setEditingId(null);
    setName('');
    setFormError(null);
    setFormOpen(true);
  };

  const openEditForm = (branch: Branch) => {
    setEditingId(branch.id);
    setName(branch.name);
    setFormError(null);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditingId(null);
    setFormError(null);
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) {
      setFormError(t('branches.nameRequired'));
      return;
    }

    setSubmitting(true);
    try {
      if (editingId) {
        await updateBranch(editingId, { name: name.trim() });
      } else {
        await createBranch({ name: name.trim() });
      }
      closeForm();
      loadData();
    } catch (err: any) {
      const code = err?.response?.data?.error;
      setFormError(code === 'name_in_use' ? t('branches.nameInUse') : t('branches.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleActive = async (branch: Branch) => {
    try {
      if (branch.isActive) {
        await deactivateBranch(branch.id);
      } else {
        await reactivateBranch(branch.id);
      }
      loadData();
    } catch {
      setError(t('branches.actionFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('branches.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          {t('branches.new')}
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {branches.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('branches.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('branches.columns.name')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('branches.columns.status')}
                  </th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {branches.map((b) => (
                  <tr key={b.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 text-slate-800">{b.name}</td>
                    <td className="px-4 py-3 text-slate-600">{b.isActive ? t('branches.active') : t('branches.inactive')}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEditForm(b)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                        >
                          {t('customers.edit')}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleToggleActive(b)}
                          className={`rounded px-2.5 py-1.5 text-sm font-medium hover:bg-slate-100 ${b.isActive ? 'text-red-600 hover:bg-red-50' : 'text-slate-700'}`}
                        >
                          {b.isActive ? t('branches.deactivate') : t('branches.reactivate')}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {formOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-900/50 p-4"
          onClick={closeForm}
        >
          <form
            onSubmit={handleSubmit}
            onClick={(e) => e.stopPropagation()}
            noValidate
            className="flex max-h-full w-full max-w-lg flex-col gap-4 overflow-y-auto rounded bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-slate-800">{editingId ? t('customers.edit') : t('branches.new')}</h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('branches.form.name')}</span>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={closeForm}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              >
                {t('branches.form.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('branches.form.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
