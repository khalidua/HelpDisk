'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { auth } from '@/lib/auth';
import { reportsApi } from '@/features/reports/api';
import { ticketsApi } from '@/features/tickets/api';
import { categoriesApi } from '@/features/categories/api';
import { agentsApi } from '@/features/agents/api';
import {
  OpenTicketsPerAgentReport,
  AverageResolutionTimeReport,
  SlaBreachesReport,
  TicketListItem,
  Category,
  Agent,
} from '@/types/api';
import { StatusBadge } from '@/components/StatusBadge';
import { PriorityBadge } from '@/components/PriorityBadge';
import { SlaBadge } from '@/components/SlaBadge';
import { PageSpinner } from '@/components/Spinner';
import { ErrorState } from '@/components/ErrorState';

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

export function AdminDashboard() {
  const [recentTickets, setRecentTickets] = useState<TicketListItem[]>([]);
  const [openTickets, setOpenTickets] = useState<OpenTicketsPerAgentReport[]>([]);
  const [avgResTime, setAvgResTime] = useState<AverageResolutionTimeReport[]>([]);
  const [slaBreaches, setSlaBreaches] = useState<SlaBreachesReport | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    const token = auth.getToken();
    if (!token) return;
    setLoading(true);
    setError(null);
    try {
      const [
        ticketsRes,
        openTRes,
        avgResT,
        slaRes,
        catRes,
        agentsRes
      ] = await Promise.all([
        ticketsApi.getTickets(token, { pageSize: 5, sortBy: 'CreatedOn', descending: true }),
        reportsApi.getOpenTicketsPerAgent(token),
        reportsApi.getAverageResolutionTimePerCategory(token),
        reportsApi.getSlaBreachesThisMonth(token),
        categoriesApi.getCategories(token),
        agentsApi.getAgents(token),
      ]);

      setRecentTickets(ticketsRes.data);
      setOpenTickets(openTRes);
      setAvgResTime(avgResT);
      setSlaBreaches(slaRes);
      setCategories(catRes);
      setAgents(agentsRes);
    } catch (err) {
      setError('Failed to load dashboard metrics.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const getCategoryName = (id: string) => categories.find(c => c.id === id)?.name || 'Unknown Category';
  
  const getAgentDetails = (id: string | null) => {
    if (!id) return { name: 'Unassigned', id: null };
    const agent = agents.find(a => a.userId === id);
    if (agent) {
      return { name: `${agent.firstName} ${agent.lastName}`, id: id };
    }
    return { name: 'Unknown Agent', id: id };
  };

  if (loading) return <div className="py-12"><PageSpinner /></div>;
  if (error) return <ErrorState message={error} onRetry={load} />;

  const totalOpen = openTickets.reduce((acc, curr) => acc + curr.openTicketsCount, 0);
  const avgHours = avgResTime.length > 0
    ? (avgResTime.reduce((acc, curr) => acc + curr.averageResolutionTimeInHours, 0) / avgResTime.length).toFixed(1)
    : '0';

  return (
    <div className="max-w-6xl mx-auto space-y-7">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">Admin Overview</h1>
          <p className="text-slate-500 mt-1 text-sm">System metrics, team workload, and SLA performance reports.</p>
        </div>
        <div className="flex items-center gap-2.5">
          <Link
            href="/admin/categories"
            className="px-3.5 py-2 bg-white border border-slate-200 hover:border-slate-300 text-slate-700 text-xs font-bold rounded-xl shadow-2xs transition-colors"
          >
            Manage Categories
          </Link>
          <Link
            href="/admin/agents"
            className="px-3.5 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors"
          >
            Manage Agents
          </Link>
        </div>
      </div>

      {/* KPI Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-5">
        <div className="p-6 bg-white border border-slate-200/80 rounded-2xl shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Total Open Tickets</span>
            <div className="w-8 h-8 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
            </div>
          </div>
          <div className="text-3xl font-extrabold text-slate-900 tracking-tight">
            {totalOpen}
          </div>
          <p className="text-xs text-slate-400 mt-1">Across all support queues</p>
          <div className="absolute left-0 bottom-0 w-full h-1 bg-blue-600" />
        </div>

        <div className="p-6 bg-white border border-slate-200/80 rounded-2xl shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">SLA Breaches This Month</span>
            <div className={`w-8 h-8 rounded-xl flex items-center justify-center ${slaBreaches?.breachCount && slaBreaches.breachCount > 0 ? 'bg-red-50 text-red-600' : 'bg-emerald-50 text-emerald-600'}`}>
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
          </div>
          <div className={`text-3xl font-extrabold tracking-tight ${slaBreaches?.breachCount && slaBreaches.breachCount > 0 ? 'text-red-600' : 'text-slate-900'}`}>
            {slaBreaches?.breachCount || 0}
          </div>
          <p className="text-xs text-slate-400 mt-1">
            {slaBreaches?.breachCount === 0 ? 'All deadlines met on time' : 'Target deadline violations'}
          </p>
          <div className={`absolute left-0 bottom-0 w-full h-1 ${slaBreaches?.breachCount && slaBreaches.breachCount > 0 ? 'bg-red-600' : 'bg-emerald-500'}`} />
        </div>

        <div className="p-6 bg-white border border-slate-200/80 rounded-2xl shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Avg Resolution Time</span>
            <div className="w-8 h-8 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <div className="text-3xl font-extrabold text-slate-900 tracking-tight flex items-baseline gap-1.5">
            {avgHours}
            <span className="text-sm font-semibold text-slate-400">hours</span>
          </div>
          <p className="text-xs text-slate-400 mt-1">Average across closed tickets</p>
          <div className="absolute left-0 bottom-0 w-full h-1 bg-purple-600" />
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Open tickets per agent */}
        <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
          <div className="px-6 py-4.5 border-b border-slate-100 flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider">Open Tickets by Agent</h2>
            <span className="text-xs text-slate-400 font-semibold">{openTickets.length} Agent{openTickets.length !== 1 ? 's' : ''}</span>
          </div>
          {openTickets.length === 0 ? (
            <div className="p-8 text-sm text-slate-500 text-center">No active tickets assigned to agents.</div>
          ) : (
            <ul className="divide-y divide-slate-100">
              {openTickets.map((t, idx) => {
                const agent = getAgentDetails(t.agentId);
                return (
                  <li key={idx} className="px-6 py-4 flex justify-between items-center hover:bg-slate-50/60 transition-colors">
                    <div className="flex items-center gap-3 min-w-0">
                      <div className="w-9 h-9 rounded-full bg-slate-100 text-slate-700 font-bold text-xs flex items-center justify-center flex-shrink-0 border border-slate-200/60">
                        {agent.name ? agent.name[0] : 'U'}
                      </div>
                      <div className="flex flex-col min-w-0">
                        <span className="text-sm font-bold text-slate-900 truncate">
                          {agent.name}
                        </span>
                        {agent.id ? (
                          <span className="text-[11px] font-mono text-slate-400 truncate">
                            {agent.id}
                          </span>
                        ) : (
                          <span className="text-[11px] text-slate-400 italic">Unassigned pool</span>
                        )}
                      </div>
                    </div>
                    <span className="text-xs font-extrabold bg-blue-50 text-blue-700 px-3 py-1.5 rounded-full border border-blue-200/80">
                      {t.openTicketsCount} ticket{t.openTicketsCount !== 1 ? 's' : ''}
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        {/* Avg resolution per category */}
        <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
          <div className="px-6 py-4.5 border-b border-slate-100 flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider">Avg Resolution Time</h2>
            <span className="text-xs text-slate-400 font-semibold">{avgResTime.length} Categories</span>
          </div>
          {avgResTime.length === 0 ? (
            <div className="p-8 text-sm text-slate-500 text-center">No closed ticket metrics available yet.</div>
          ) : (
            <ul className="divide-y divide-slate-100">
              {avgResTime.map((c) => (
                <li key={c.categoryId} className="px-6 py-4 flex justify-between items-center hover:bg-slate-50/60 transition-colors">
                  <div className="flex items-center gap-3">
                    <div className="w-2.5 h-2.5 rounded-full bg-purple-500 flex-shrink-0" />
                    <span className="text-sm font-semibold text-slate-800">
                      {getCategoryName(c.categoryId)}
                    </span>
                  </div>
                  <div className="text-right">
                    <span className="text-sm font-extrabold text-slate-900">{c.averageResolutionTimeInHours.toFixed(1)}</span>
                    <span className="text-xs text-slate-400 ml-1 font-medium">hrs avg</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Recent Tickets Table Section */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4.5 border-b border-slate-100">
          <div>
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider">System Queue Activity</h2>
            <p className="text-xs text-slate-400 mt-0.5">Most recent incoming support tickets</p>
          </div>
          <Link href="/tickets" className="text-xs font-bold text-blue-600 hover:text-blue-700 hover:underline flex items-center gap-1">
            View All Tickets →
          </Link>
        </div>

        {recentTickets.length === 0 ? (
          <div className="py-12 text-center text-sm text-slate-500">No recent tickets found.</div>
        ) : (
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
