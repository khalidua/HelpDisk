'use client';

import { useEffect, useState } from 'react';
import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { auth } from '@/lib/auth';
import { agentsApi } from '@/features/agents/api';
import { Agent } from '@/types/api';
import { PageSpinner, Spinner } from '@/components/Spinner';
import { ErrorState } from '@/components/ErrorState';
import { ToastContainer, showToast } from '@/components/Toast';

export default function AgentsPage() {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null);

  // Form State
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const load = async () => {
    const token = auth.getToken();
    if (!token) return;
    setLoading(true);
    setError(null);
    try {
      const data = await agentsApi.getAgents(token);
      setAgents(data);
    } catch (err) {
      setError('Failed to load agents.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleOpenAdd = () => {
    setEditingAgent(null);
    setEmail('');
    setFirstName('');
    setLastName('');
    setPassword('');
    setIsModalOpen(true);
  };

  const handleOpenEdit = (agent: Agent) => {
    setEditingAgent(agent);
    setEmail(agent.email);
    setFirstName(agent.firstName);
    setLastName(agent.lastName);
    setPassword('');
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const token = auth.getToken();
    if (!token) return;

    setSubmitting(true);
    try {
      if (editingAgent) {
        await agentsApi.updateAgent(token, editingAgent.userId, { email, firstName, lastName });
        showToast('success', 'Agent updated successfully.');
      } else {
        await agentsApi.createAgent(token, { email, password, firstName, lastName });
        showToast('success', 'Agent created successfully.');
      }
      setIsModalOpen(false);
      load();
    } catch (err: any) {
      showToast('error', err.message || 'Failed to save agent.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleActive = async (agent: Agent) => {
    const token = auth.getToken();
    if (!token) return;
    try {
      if (agent.isActive) {
        if (!confirm(`Are you sure you want to deactivate ${agent.firstName} ${agent.lastName}? They will no longer be able to log in.`)) return;
        await agentsApi.deactivateAgent(token, agent.userId);
        showToast('success', 'Agent deactivated.');
      } else {
        await agentsApi.activateAgent(token, agent.userId);
        showToast('success', 'Agent activated.');
      }
      load();
    } catch (err: any) {
      showToast('error', err.message || 'Action failed.');
    }
  };

  const inputClass =
    'w-full px-4 py-2.5 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent text-xs sm:text-sm shadow-2xs transition-all';
  const labelClass = 'block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5';

  return (
    <RoleGuard allowedRoles={['Admin']}>
      <AppShell title="Agent Management">
        <div className="max-w-6xl mx-auto space-y-6">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div>
              <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">Agent Staff</h1>
              <p className="text-slate-500 text-xs sm:text-sm mt-1">Manage support staff accounts, access status, and profile details.</p>
            </div>
            <button
              onClick={handleOpenAdd}
              className="inline-flex items-center gap-1.5 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-xs font-bold rounded-xl shadow-xs transition-all"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              Add New Agent
            </button>
          </div>

          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
            {loading ? (
              <div className="py-12"><PageSpinner /></div>
            ) : error ? (
              <ErrorState message={error} onRetry={load} />
            ) : agents.length === 0 ? (
              <div className="p-12 text-center text-sm text-slate-500">No agents registered in the system.</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse" aria-label="Agents list">
                  <thead>
                    <tr className="bg-slate-50/80 border-b border-slate-100 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                      <th className="px-6 py-4">Agent Name</th>
                      <th className="px-6 py-4">Email</th>
                      <th className="px-6 py-4">Status</th>
                      <th className="px-6 py-4 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-sm">
                    {agents.map((agent) => (
                      <tr key={agent.userId} className="hover:bg-slate-50/60 transition-colors">
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-slate-800 text-white font-bold text-xs flex items-center justify-center flex-shrink-0">
                              {agent.firstName ? agent.firstName[0].toUpperCase() : 'A'}
                            </div>
                            <div>
                              <div className="font-bold text-slate-900">
                                {agent.firstName} {agent.lastName}
                              </div>
                              <div className="font-mono text-[11px] text-slate-400">
                                {agent.userId}
                              </div>
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4 text-xs font-medium text-slate-600">{agent.email}</td>
                        <td className="px-6 py-4">
                          <span
                            className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border ${
                              agent.isActive
                                ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                : 'bg-slate-100 text-slate-600 border-slate-200'
                            }`}
                          >
                            <span className={`w-1.5 h-1.5 rounded-full ${agent.isActive ? 'bg-emerald-600' : 'bg-slate-400'}`} />
                            {agent.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-right">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              onClick={() => handleOpenEdit(agent)}
                              className="px-3 py-1.5 text-xs font-bold text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-xl transition-colors"
                            >
                              Edit
                            </button>
                            <button
                              onClick={() => handleToggleActive(agent)}
                              className={`px-3 py-1.5 text-xs font-bold rounded-xl transition-colors ${
                                agent.isActive
                                  ? 'text-red-700 bg-red-50 hover:bg-red-100'
                                  : 'text-emerald-700 bg-emerald-50 hover:bg-emerald-100'
                              }`}
                            >
                              {agent.isActive ? 'Deactivate' : 'Activate'}
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
                  {editingAgent ? 'Edit Agent Profile' : 'Add New Agent'}
                </h2>
                <button
                  onClick={() => setIsModalOpen(false)}
                  className="w-8 h-8 rounded-full flex items-center justify-center text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors text-sm"
                >
                  ✕
                </button>
              </div>
              <form onSubmit={handleSave} className="p-6 space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className={labelClass}>First Name</label>
                    <input
                      required
                      type="text"
                      value={firstName}
                      onChange={(e) => setFirstName(e.target.value)}
                      className={inputClass}
                    />
                  </div>
                  <div>
                    <label className={labelClass}>Last Name</label>
                    <input
                      required
                      type="text"
                      value={lastName}
                      onChange={(e) => setLastName(e.target.value)}
                      className={inputClass}
                    />
                  </div>
                </div>
                <div>
                  <label className={labelClass}>Email Address</label>
                  <input
                    required
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className={inputClass}
                  />
                </div>
                {!editingAgent && (
                  <div>
                    <label className={labelClass}>Password</label>
                    <input
                      required
                      type="password"
                      placeholder="••••••••"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className={inputClass}
                    />
                  </div>
                )}
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
                    {submitting ? 'Saving…' : 'Save Agent'}
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
