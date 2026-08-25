'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { ToastContainer } from '@/components/Toast';
import { auth } from '@/lib/auth';
import { ticketsApi } from '@/features/tickets/api';
import { PaginatedList, TicketListItem } from '@/types/api';
import { StatusBadge } from '@/components/StatusBadge';
import { PriorityBadge } from '@/components/PriorityBadge';
import { SlaBadge } from '@/components/SlaBadge';
import { PageSpinner } from '@/components/Spinner';
import { ErrorState } from '@/components/ErrorState';
import { AdminDashboard } from '@/features/reports/components/AdminDashboard';

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

function CustomerDashboard() {
  const [recentTickets, setRecentTickets] = useState<TicketListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = () => {
    const token = auth.getToken();
    if (!token) return;
    setLoading(true);
    setError(null);
    ticketsApi
      .getTickets(token, { pageSize: 5, sortBy: 'CreatedOn', descending: true })
      .then((res: PaginatedList<TicketListItem>) => setRecentTickets(res.data))
      .catch(() => setError('Failed to load recent tickets.'))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  return (
    <div className="max-w-5xl mx-auto space-y-7">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-gradient-to-r from-blue-900 to-indigo-800 p-6 sm:p-8 rounded-3xl text-white shadow-sm relative overflow-hidden">
        <div className="relative z-10 space-y-1.5 max-w-xl">
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-white/15 text-blue-100 backdrop-blur-xs">
            Customer Portal
          </span>
          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight">Need help with something?</h1>
          <p className="text-blue-100 text-sm leading-relaxed">
            Create a support ticket and track its progress from start to resolution.
          </p>
        </div>
        <div className="relative z-10 flex-shrink-0">
          <Link
            href="/tickets/new"
            className="inline-flex items-center gap-2 px-5 py-3 bg-white hover:bg-blue-50 text-blue-900 text-sm font-bold rounded-2xl shadow-sm transition-all transform active:scale-95"
          >
            <svg className="w-4 h-4 text-blue-700" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
            </svg>
            Create New Ticket
          </Link>
        </div>
        {/* Background decorative circles */}
        <div className="absolute -right-8 -bottom-10 w-48 h-48 bg-white/5 rounded-full blur-2xl pointer-events-none" />
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Link
          href="/tickets/new"
          className="flex items-center gap-4 p-5 bg-white hover:bg-blue-50/40 border border-slate-200/80 hover:border-blue-300 rounded-2xl shadow-xs hover:shadow-sm transition-all group"
        >
          <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center flex-shrink-0 group-hover:scale-105 group-hover:bg-blue-600 group-hover:text-white transition-all shadow-2xs">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-bold text-base text-slate-900 group-hover:text-blue-600 transition-colors">Submit a Ticket</p>
            <p className="text-slate-500 text-xs mt-0.5">Report an issue or request assistance</p>
          </div>
          <span className="text-slate-300 group-hover:text-blue-600 group-hover:translate-x-0.5 transition-all text-sm">→</span>
        </Link>

        <Link
          href="/tickets"
          className="flex items-center gap-4 p-5 bg-white hover:bg-slate-50/80 border border-slate-200/80 hover:border-slate-300 rounded-2xl shadow-xs hover:shadow-sm transition-all group"
        >
          <div className="w-12 h-12 rounded-xl bg-slate-100 text-slate-700 flex items-center justify-center flex-shrink-0 group-hover:scale-105 group-hover:bg-slate-800 group-hover:text-white transition-all shadow-2xs">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
                d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-bold text-base text-slate-900 group-hover:text-slate-900 transition-colors">All My Tickets</p>
            <p className="text-slate-500 text-xs mt-0.5">View your open and closed requests</p>
          </div>
          <span className="text-slate-300 group-hover:text-slate-600 group-hover:translate-x-0.5 transition-all text-sm">→</span>
        </Link>
      </div>

      {/* Recent Tickets Section */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4.5 border-b border-slate-100">
          <div>
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider">Recent Activity</h2>
            <p className="text-xs text-slate-400 mt-0.5">Your latest support requests</p>
          </div>
          <Link href="/tickets" className="text-xs font-bold text-blue-600 hover:text-blue-700 hover:underline flex items-center gap-1">
            View all tickets <span>→</span>
          </Link>
        </div>

        {loading && <div className="py-12"><PageSpinner /></div>}
        {error && !loading && <ErrorState message={error} onRetry={load} />}

        {!loading && !error && recentTickets.length === 0 && (
          <div className="py-14 text-center px-4">
            <div className="w-12 h-12 rounded-2xl bg-slate-100 flex items-center justify-center mx-auto mb-3 text-slate-400">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
              </svg>
            </div>
            <p className="text-sm font-semibold text-slate-700">No tickets found</p>
            <p className="text-xs text-slate-400 mt-1 max-w-sm mx-auto">You haven't submitted any tickets yet.</p>
            <Link href="/tickets/new" className="mt-4 inline-flex items-center gap-1.5 px-4 py-2 bg-blue-600 text-white text-xs font-bold rounded-xl hover:bg-blue-700 transition-colors shadow-2xs">
              + Submit Your First Ticket
            </Link>
          </div>
        )}

        {!loading && !error && recentTickets.length > 0 && (
          <ul className="divide-y divide-slate-100">
            {recentTickets.map((ticket) => (
              <li key={ticket.id}>
                <Link
                  href={`/tickets/${ticket.id}`}
                  className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4 px-6 py-4 hover:bg-slate-50/80 transition-colors group"
                >
                  <div className="flex-1 min-w-0 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs font-bold text-blue-600 group-hover:underline">
                        #{ticket.ticketNumber}
                      </span>
                      <StatusBadge status={ticket.status} />
                    </div>
                    <p className="text-sm font-semibold text-slate-900 truncate group-hover:text-blue-600 transition-colors">
                      {ticket.title}
                    </p>
                    <p className="text-xs text-slate-400">Created {formatDate(ticket.createdOnUtc)}</p>
                  </div>
                  <div className="flex items-center gap-3 sm:gap-4 flex-shrink-0 self-start sm:self-center">
                    <PriorityBadge priority={ticket.priority} />
                    <SlaBadge slaStatus={ticket.slaStatus} />
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function AgentDashboard() {
  const [recentTickets, setRecentTickets] = useState<TicketListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = () => {
    const token = auth.getToken();
    if (!token) return;
    setLoading(true);
    setError(null);
    ticketsApi
      .getTickets(token, { pageSize: 5, sortBy: 'CreatedOn', descending: true })
      .then((res: PaginatedList<TicketListItem>) => setRecentTickets(res.data))
      .catch(() => setError('Failed to load recent tickets.'))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  return (
    <div className="max-w-5xl mx-auto space-y-7">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">Agent Workspace</h1>
          <p className="text-slate-500 mt-1 text-sm">Manage incoming support requests and active queues.</p>
        </div>
        <Link
          href="/tickets"
          className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors"
        >
          View Full Queue →
        </Link>
      </div>

      {/* Queue Quick Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Link
          href="/tickets?status=New"
          className="p-5 bg-white border border-slate-200/80 hover:border-blue-300 rounded-2xl shadow-xs hover:shadow-md transition-all group relative overflow-hidden"
        >
          <div className="flex items-center justify-between mb-3">
            <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center group-hover:scale-105 transition-transform shadow-2xs">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-blue-50 text-blue-700 border border-blue-200">
              Needs Triage
            </span>
          </div>
          <p className="font-bold text-slate-900 text-lg group-hover:text-blue-600 transition-colors">New Queue</p>
          <p className="text-xs text-slate-500 mt-1">Unassigned and newly opened tickets</p>
        </Link>
        
        <Link
          href="/tickets?status=InProgress"
          className="p-5 bg-white border border-slate-200/80 hover:border-amber-300 rounded-2xl shadow-xs hover:shadow-md transition-all group relative overflow-hidden"
        >
          <div className="flex items-center justify-between mb-3">
            <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center group-hover:scale-105 transition-transform shadow-2xs">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-amber-50 text-amber-700 border border-amber-200">
              Active
            </span>
          </div>
          <p className="font-bold text-slate-900 text-lg group-hover:text-amber-600 transition-colors">In Progress</p>
          <p className="text-xs text-slate-500 mt-1">Tickets currently being investigated</p>
        </Link>

        <Link
          href="/tickets"
          className="p-5 bg-white border border-slate-200/80 hover:border-slate-300 rounded-2xl shadow-xs hover:shadow-md transition-all group relative overflow-hidden"
        >
          <div className="flex items-center justify-between mb-3">
            <div className="w-10 h-10 rounded-xl bg-slate-100 text-slate-700 flex items-center justify-center group-hover:scale-105 transition-transform shadow-2xs">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
              </svg>
            </div>
            <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-slate-100 text-slate-700 border border-slate-200">
              Complete Queue
            </span>
          </div>
          <p className="font-bold text-slate-900 text-lg group-hover:text-slate-700 transition-colors">All Tickets</p>
          <p className="text-xs text-slate-500 mt-1">Search and filter across all tickets</p>
        </Link>
      </div>

      {/* Recent Queue Activity */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4.5 border-b border-slate-100">
          <div>
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider">Queue Activity</h2>
            <p className="text-xs text-slate-400 mt-0.5">Most recently created or updated tickets</p>
          </div>
          <Link href="/tickets" className="text-xs font-bold text-blue-600 hover:text-blue-700 hover:underline">
            View full queue →
          </Link>
        </div>

        {loading && <div className="py-12"><PageSpinner /></div>}
        {error && !loading && <ErrorState message={error} onRetry={load} />}

        {!loading && !error && recentTickets.length === 0 && (
          <div className="py-12 text-center text-sm text-slate-500">No tickets found.</div>
        )}

        {!loading && !error && recentTickets.length > 0 && (
          <ul className="divide-y divide-slate-100">
            {recentTickets.map((ticket) => (
              <li key={ticket.id}>
                <Link
                  href={`/tickets/${ticket.id}`}
                  className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4 px-6 py-4 hover:bg-slate-50/80 transition-colors group"
                >
                  <div className="flex-1 min-w-0 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs font-bold text-blue-600 group-hover:underline">
                        #{ticket.ticketNumber}
                      </span>
                      <StatusBadge status={ticket.status} />
                    </div>
                    <p className="text-sm font-semibold text-slate-900 truncate group-hover:text-blue-600 transition-colors">
                      {ticket.title}
                    </p>
                    <p className="text-xs text-slate-400">{formatDate(ticket.createdOnUtc)}</p>
                  </div>
                  <div className="flex items-center gap-3 sm:gap-4 flex-shrink-0">
                    <PriorityBadge priority={ticket.priority} />
                    <SlaBadge slaStatus={ticket.slaStatus} deadline={ticket.responseDeadlineUtc} />
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const router = useRouter();
  const [role, setRole] = useState<string | null>(null);

  useEffect(() => {
    setRole(auth.getRole() ?? 'Unknown');
  }, []);

  return (
    <RoleGuard>
      <AppShell title="Dashboard">
        {role === 'Customer' && <CustomerDashboard />}
        {role === 'Agent' && <AgentDashboard />}
        {role === 'Admin' && <AdminDashboard />}
      </AppShell>
      <ToastContainer />
    </RoleGuard>
  );
}
