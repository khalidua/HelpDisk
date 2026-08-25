'use client';

import { TicketPriority } from '@/types/api';

interface PriorityBadgeProps {
  priority: TicketPriority;
}

const UrgentIcon = () => (
  <svg className="w-3 h-3 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M13 10V3L4 14h7v7l9-11h-7z" />
  </svg>
);

const HighIcon = () => (
  <svg className="w-3 h-3 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 15l7-7 7 7" />
  </svg>
);

const NormalIcon = () => (
  <svg className="w-3 h-3 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 12h14" />
  </svg>
);

const LowIcon = () => (
  <svg className="w-3 h-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M19 9l-7 7-7-7" />
  </svg>
);

const priorityConfig: Record<
  TicketPriority,
  { label: string; bg: string; text: string; border: string; icon: React.ReactNode }
> = {
  Low: {
    label: 'Low',
    bg: 'bg-slate-50/80',
    text: 'text-slate-600',
    border: 'border-slate-200',
    icon: <LowIcon />,
  },
  Normal: {
    label: 'Normal',
    bg: 'bg-emerald-50/80',
    text: 'text-emerald-700',
    border: 'border-emerald-200/80',
    icon: <NormalIcon />,
  },
  High: {
    label: 'High',
    bg: 'bg-orange-50/80',
    text: 'text-orange-700',
    border: 'border-orange-200/80',
    icon: <HighIcon />,
  },
  Urgent: {
    label: 'Urgent',
    bg: 'bg-red-50/80',
    text: 'text-red-700 font-bold',
    border: 'border-red-200/80',
    icon: <UrgentIcon />,
  },
};

export function PriorityBadge({ priority }: PriorityBadgeProps) {
  const config = priorityConfig[priority] ?? priorityConfig.Normal;
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border ${config.bg} ${config.text} ${config.border} shadow-2xs`}
    >
      <span aria-hidden="true" className="flex items-center justify-center">
        {config.icon}
      </span>
      {config.label}
    </span>
  );
}
