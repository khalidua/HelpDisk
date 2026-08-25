'use client';

import Link from 'next/link';
import { TicketListItem, Category } from '@/types/api';
import { StatusBadge } from '@/components/StatusBadge';
import { PriorityBadge } from '@/components/PriorityBadge';
import { SlaBadge } from '@/components/SlaBadge';

interface TicketTableProps {
  tickets: TicketListItem[];
  categories: Category[];
  role?: string;
}

function getCategoryName(categories: Category[], id: string): string {
  return categories.find((c) => c.id === id)?.name ?? id;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function TicketTable({ tickets, categories, role = 'Customer' }: TicketTableProps) {
  const isAgent = role !== 'Customer';

  return (
    <div className="w-full">
      {/* Desktop table */}
      <div className="hidden md:block overflow-x-auto rounded-2xl border border-slate-200/80 shadow-xs bg-white">
        <table className="w-full text-left border-collapse" aria-label="Tickets list">
          <thead>
            <tr className="bg-slate-50/80 border-b border-slate-100 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
              <th className="px-5 py-3.5 w-28">Ticket #</th>
              <th className="px-5 py-3.5">Title</th>
              <th className="px-5 py-3.5 w-32">Status</th>
              <th className="px-5 py-3.5 w-28">Priority</th>
              <th className="px-5 py-3.5 w-36">Category</th>
              {isAgent && (
                <th className="px-5 py-3.5 w-36">Assignee</th>
              )}
              <th className="px-5 py-3.5 w-44">SLA</th>
              <th className="px-5 py-3.5 w-32">Created</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 text-sm">
            {tickets.map((ticket) => (
              <tr
                key={ticket.id}
                className={`hover:bg-blue-50/20 transition-colors group ${
                  ticket.slaStatus === 'Breached' ? 'bg-red-50/30' : ''
                }`}
              >
                <td className="px-5 py-4">
                  <Link
                    href={`/tickets/${ticket.id}`}
                    className="font-mono text-xs font-bold text-blue-600 hover:text-blue-800 hover:underline"
                  >
                    #{ticket.ticketNumber}
                  </Link>
                </td>
                <td className="px-5 py-4">
                  <Link
                    href={`/tickets/${ticket.id}`}
                    className="font-semibold text-slate-900 group-hover:text-blue-600 transition-colors line-clamp-2"
                  >
                    {ticket.title}
                  </Link>
                </td>
                <td className="px-5 py-4">
                  <StatusBadge status={ticket.status} />
                </td>
                <td className="px-5 py-4">
                  <PriorityBadge priority={ticket.priority} />
                </td>
                <td className="px-5 py-4">
                  <span className="text-xs font-semibold text-slate-600 bg-slate-100/80 px-2.5 py-1 rounded-lg border border-slate-200/50">
                    {getCategoryName(categories, ticket.categoryId)}
                  </span>
                </td>
                {isAgent && (
                  <td className="px-5 py-4">
                    {ticket.assigneeId ? (
                      <span className="font-mono text-[11px] text-slate-500 bg-slate-50 px-2 py-0.5 rounded border border-slate-200/60 truncate block max-w-[7rem]" title={ticket.assigneeId}>
                        {ticket.assigneeId.split('-')[0]}…
                      </span>
                    ) : (
                      <span className="text-xs text-slate-400 italic">Unassigned</span>
                    )}
                  </td>
                )}
                <td className="px-5 py-4">
                  <SlaBadge slaStatus={ticket.slaStatus} deadline={ticket.responseDeadlineUtc} />
                </td>
                <td className="px-5 py-4 text-xs text-slate-500 font-medium">
                  {formatDate(ticket.createdOnUtc)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile card list */}
      <div className="md:hidden space-y-3.5">
        {tickets.map((ticket) => (
          <Link
            key={ticket.id}
            href={`/tickets/${ticket.id}`}
            className={`block bg-white rounded-2xl border shadow-xs p-4.5 space-y-3 hover:shadow-md transition-all ${
              ticket.slaStatus === 'Breached' ? 'border-red-200 bg-red-50/10' : 'border-slate-200/80'
            }`}
          >
            <div className="flex items-start justify-between gap-2">
              <div className="space-y-1 min-w-0">
                <span className="font-mono text-xs font-bold text-blue-600">#{ticket.ticketNumber}</span>
                <p className="text-sm font-bold text-slate-900 leading-snug line-clamp-2">{ticket.title}</p>
              </div>
              <StatusBadge status={ticket.status} />
            </div>

            <div className="flex flex-wrap items-center gap-2 pt-1 border-t border-slate-100">
              <PriorityBadge priority={ticket.priority} />
              <SlaBadge slaStatus={ticket.slaStatus} />
              <span className="text-xs font-medium text-slate-500 bg-slate-100 px-2 py-0.5 rounded-md">
                {getCategoryName(categories, ticket.categoryId)}
              </span>
            </div>

            <div className="flex items-center justify-between text-xs text-slate-400 pt-1">
              <span>{formatDate(ticket.createdOnUtc)}</span>
              {isAgent && (
                <span className="font-mono text-[11px]">
                  {ticket.assigneeId ? `Assigned: ${ticket.assigneeId.split('-')[0]}` : 'Unassigned'}
                </span>
              )}
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
