'use client';

import { TicketStatus } from '@/types/api';

interface StatusBadgeProps {
  status: TicketStatus;
}

const statusConfig: Record<
  TicketStatus,
  { label: string; bg: string; text: string; border: string; dot: string; pulse?: boolean }
> = {
  New: {
    label: 'New',
    bg: 'bg-blue-50/80',
    text: 'text-blue-700',
    border: 'border-blue-200/80',
    dot: 'bg-blue-600',
    pulse: true,
  },
  InProgress: {
    label: 'In Progress',
    bg: 'bg-amber-50/80',
    text: 'text-amber-800',
    border: 'border-amber-200/80',
    dot: 'bg-amber-500',
  },
  Closed: {
    label: 'Closed',
    bg: 'bg-slate-100/90',
    text: 'text-slate-600',
    border: 'border-slate-200',
    dot: 'bg-slate-400',
  },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const config = statusConfig[status] ?? statusConfig.New;
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border ${config.bg} ${config.text} ${config.border} shadow-2xs`}
    >
      <span className="relative flex h-1.5 w-1.5">
        {config.pulse && (
          <span className={`animate-ping absolute inline-flex h-full w-full rounded-full ${config.dot} opacity-75`} />
        )}
        <span className={`relative inline-flex rounded-full h-1.5 w-1.5 ${config.dot}`} />
      </span>
      {config.label}
    </span>
  );
}
