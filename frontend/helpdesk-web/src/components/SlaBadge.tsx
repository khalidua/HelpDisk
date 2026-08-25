'use client';

import { TicketSlaStatus } from '@/types/api';

interface SlaBadgeProps {
  slaStatus: TicketSlaStatus;
  deadline?: string; // ISO date string
}

const PendingIcon = () => (
  <svg className="w-3 h-3 text-blue-600 animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
);

const MetIcon = () => (
  <svg className="w-3 h-3 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 13l4 4L19 7" />
  </svg>
);

const BreachedIcon = () => (
  <svg className="w-3 h-3 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
  </svg>
);

const slaConfig: Record<
  TicketSlaStatus,
  { label: string; bg: string; text: string; border: string; icon: React.ReactNode }
> = {
  Pending: {
    label: 'SLA Pending',
    bg: 'bg-blue-50/80',
    text: 'text-blue-700',
    border: 'border-blue-200/80',
    icon: <PendingIcon />,
  },
  Met: {
    label: 'SLA Met',
    bg: 'bg-emerald-50/80',
    text: 'text-emerald-700',
    border: 'border-emerald-200/80',
    icon: <MetIcon />,
  },
  Breached: {
    label: 'SLA Breached',
    bg: 'bg-red-50/80',
    text: 'text-red-700 font-bold',
    border: 'border-red-200/80',
    icon: <BreachedIcon />,
  },
};

function formatDeadline(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function SlaBadge({ slaStatus, deadline }: SlaBadgeProps) {
  const config = slaConfig[slaStatus] ?? slaConfig.Pending;
  return (
    <div className="inline-flex flex-wrap items-center gap-1.5">
      <span
        className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border ${config.bg} ${config.text} ${config.border} shadow-2xs`}
        aria-label={config.label}
      >
        <span aria-hidden="true" className="flex items-center justify-center">
          {config.icon}
        </span>
        {config.label}
      </span>
      {deadline && (
        <span className="text-[11px] text-slate-500 font-medium bg-slate-100/80 px-2 py-0.5 rounded-md border border-slate-200/60" title="SLA response deadline">
          Due: {formatDeadline(deadline)}
        </span>
      )}
    </div>
  );
}
