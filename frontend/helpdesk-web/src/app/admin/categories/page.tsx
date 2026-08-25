'use client';

import { useEffect, useState } from 'react';
import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { auth } from '@/lib/auth';
import { categoriesApi } from '@/features/categories/api';
import { Category } from '@/types/api';
import { PageSpinner, Spinner } from '@/components/Spinner';
import { ErrorState } from '@/components/ErrorState';
import { ToastContainer, showToast } from '@/components/Toast';

export default function CategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);

  // Form State
  const [name, setName] = useState('');
  const [responseTimeTargetHours, setResponseTimeTargetHours] = useState<number | ''>('');
  const [submitting, setSubmitting] = useState(false);

  const load = async () => {
    const token = auth.getToken();
    if (!token) return;
    setLoading(true);
    setError(null);
    try {
      const data = await categoriesApi.getCategories(token);
      setCategories(data);
    } catch (err) {
      setError('Failed to load categories.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleOpenAdd = () => {
    setEditingCategory(null);
    setName('');
    setResponseTimeTargetHours('');
    setIsModalOpen(true);
  };

  const handleOpenEdit = (category: Category) => {
    setEditingCategory(category);
    setName(category.name);
    setResponseTimeTargetHours(category.responseTimeTargetHours);
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const token = auth.getToken();
    if (!token) return;

    if (responseTimeTargetHours === '' || responseTimeTargetHours <= 0) {
      showToast('error', 'Response time target must be greater than 0 hours.');
      return;
    }

    setSubmitting(true);
    try {
      if (editingCategory) {
        await categoriesApi.updateCategory(token, editingCategory.id, { name, responseTimeTargetHours });
        showToast('success', 'Category updated successfully.');
      } else {
        await categoriesApi.createCategory(token, { name, responseTimeTargetHours });
        showToast('success', 'Category created successfully.');
      }
      setIsModalOpen(false);
      load();
    } catch (err: any) {
      showToast('error', err.message || 'Failed to save category.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (category: Category) => {
    const token = auth.getToken();
    if (!token) return;
    if (!confirm(`Are you sure you want to delete the "${category.name}" category?`)) return;
    try {
      await categoriesApi.deleteCategory(token, category.id);
      showToast('success', 'Category deleted.');
      load();
    } catch (err: any) {
      showToast('error', err.message || 'Failed to delete category. It may have existing tickets assigned.');
    }
  };

  const inputClass =
    'w-full px-4 py-2.5 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent text-xs sm:text-sm shadow-2xs transition-all';
  const labelClass = 'block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5';

  return (
    <RoleGuard allowedRoles={['Admin']}>
      <AppShell title="Category Management">
        <div className="max-w-6xl mx-auto space-y-6">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div>
              <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">Ticket Categories</h1>
              <p className="text-slate-500 text-xs sm:text-sm mt-1">Configure issue categories, triage buckets, and SLA response targets.</p>
            </div>
            <button
              onClick={handleOpenAdd}
              className="inline-flex items-center gap-1.5 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-xs font-bold rounded-xl shadow-xs transition-all"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              Add Category
            </button>
          </div>

          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
            {loading ? (
              <div className="py-12"><PageSpinner /></div>
            ) : error ? (
              <ErrorState message={error} onRetry={load} />
            ) : categories.length === 0 ? (
              <div className="p-12 text-center text-sm text-slate-500">No categories found.</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse" aria-label="Categories list">
                  <thead>
                    <tr className="bg-slate-50/80 border-b border-slate-100 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                      <th className="px-6 py-4">Category Name</th>
                      <th className="px-6 py-4">SLA Target</th>
                      <th className="px-6 py-4 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {categories.map((category) => (
                      <tr key={category.id} className="hover:bg-slate-50/60 transition-colors">
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-xl bg-purple-50 text-purple-600 font-bold text-xs flex items-center justify-center flex-shrink-0 border border-purple-200/60">
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                              </svg>
                            </div>
                            <span className="font-bold text-slate-900">{category.name}</span>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-slate-100 text-slate-700 border border-slate-200/80">
                            <svg className="w-3 h-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            {category.responseTimeTargetHours} hours target
                          </span>
                        </td>
                        <td className="px-6 py-4 text-right">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              onClick={() => handleOpenEdit(category)}
                              className="px-3 py-1.5 text-xs font-bold text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-xl transition-colors"
                            >
                              Edit
                            </button>
                            <button
                              onClick={() => handleDelete(category)}
                              className="px-3 py-1.5 text-xs font-bold text-red-700 bg-red-50 hover:bg-red-100 rounded-xl transition-colors"
                            >
                              Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>

        {/* Modal dialog */}
        {isModalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-xs animate-in fade-in duration-150">
            <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-100">
              <div className="px-6 py-5 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
                <h2 className="text-base font-extrabold text-slate-900">
                  {editingCategory ? 'Edit Category' : 'Create Category'}
                </h2>
                <button
                  onClick={() => setIsModalOpen(false)}
                  className="w-8 h-8 rounded-full flex items-center justify-center text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors text-sm"
                >
                  ✕
                </button>
              </div>
              <form onSubmit={handleSave} className="p-6 space-y-4">
                <div>
                  <label className={labelClass}>Category Name</label>
                  <input
                    required
                    type="text"
                    placeholder="e.g. Hardware Issues"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>SLA Target (Hours)</label>
                  <input
                    required
                    type="number"
                    min="1"
                    placeholder="e.g. 24"
                    value={responseTimeTargetHours}
                    onChange={(e) => setResponseTimeTargetHours(e.target.value ? parseInt(e.target.value, 10) : '')}
                    className={inputClass}
                  />
                </div>
                <div className="pt-4 flex justify-end gap-2.5 border-t border-slate-100">
                  <button
                    type="button"
                    onClick={() => setIsModalOpen(false)}
                    className="px-4 py-2.5 text-xs font-bold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-xl transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={submitting}
                    className="inline-flex items-center gap-2 px-5 py-2.5 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-xs transition-colors disabled:opacity-50"
                  >
                    {submitting && <Spinner size="sm" className="border-white" />}
                    {submitting ? 'Saving…' : 'Save Category'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </AppShell>
      <ToastContainer />
    </RoleGuard>
  );
}
