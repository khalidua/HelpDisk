'use client';

import { TicketStatus, TicketPriority, Category } from '@/types/api';
import { GetTicketsParams } from '../api';

interface TicketFiltersProps {
  filters: GetTicketsParams;
  categories: Category[];
  onChange: (filters: GetTicketsParams) => void;
  role?: string;
}

const statusOptions: { value: TicketStatus | ''; label: string }[] = [
  { value: '', label: 'All Statuses' },
  { value: 'New', label: 'New' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Closed', label: 'Closed' },
];

const priorityOptions: { value: TicketPriority | ''; label: string }[] = [
  { value: '', label: 'All Priorities' },
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
  { value: 'Urgent', label: 'Urgent' },
];

const sortOptions: { value: GetTicketsParams['sortBy'] | ''; label: string }[] = [
  { value: '', label: 'Sort: Default' },
  { value: 'CreatedOn', label: 'Sort: Date Created' },
  { value: 'Priority', label: 'Sort: Priority' },
  { value: 'Status', label: 'Sort: Status' },
];

const selectClass =
  'px-3.5 py-2 bg-slate-50/50 hover:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-xs font-semibold text-slate-700 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all shadow-2xs cursor-pointer';

export function TicketFilters({ filters, categories, onChange, role }: TicketFiltersProps) {
  const update = (partial: Partial<GetTicketsParams>) => {
    onChange({ ...filters, ...partial, page: 1 });
  };

  const hasActiveFilters = Boolean(
    filters.keyword ||
    filters.status ||
    filters.priority ||
    filters.categoryId ||
    filters.assigneeId ||
    filters.sortBy
  );

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2.5 items-center">
        {/* Keyword Search */}
        <div className="relative flex-1 min-w-[220px]">
          <svg
            className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z" />
          </svg>
          <input
            id="ticket-search"
            type="search"
            placeholder="Search by title, number, keyword…"
            value={filters.keyword ?? ''}
            onChange={(e) => update({ keyword: e.target.value || undefined })}
            className="w-full pl-10 pr-9 py-2 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-xs font-medium text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all shadow-2xs"
            aria-label="Search tickets"
          />
          {filters.keyword && (
            <button
              onClick={() => update({ keyword: undefined })}
              className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 p-1 text-xs"
              aria-label="Clear search"
            >
              ✕
            </button>
          )}
        </div>

        {/* Status Filter */}
        <select
          id="ticket-status-filter"
          value={filters.status ?? ''}
          onChange={(e) => update({ status: (e.target.value as TicketStatus) || undefined })}
          className={selectClass}
          aria-label="Filter by status"
        >
          {statusOptions.map((opt) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>

        {/* Priority Filter */}
        <select
          id="ticket-priority-filter"
          value={filters.priority ?? ''}
          onChange={(e) => update({ priority: (e.target.value as TicketPriority) || undefined })}
          className={selectClass}
          aria-label="Filter by priority"
        >
          {priorityOptions.map((opt) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>

        {/* Category Filter */}
        {categories.length > 0 && (
          <select
            id="ticket-category-filter"
            value={filters.categoryId ?? ''}
            onChange={(e) => update({ categoryId: e.target.value || undefined })}
            className={selectClass}
            aria-label="Filter by category"
          >
            <option value="">All Categories</option>
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id}>{cat.name}</option>
            ))}
          </select>
        )}

        {/* Assignee Filter (Agents / Admins) */}
        {role && role !== 'Customer' && (
          <input
            id="ticket-assignee-filter"
            type="text"
            placeholder="Assignee ID…"
            value={filters.assigneeId ?? ''}
            onChange={(e) => update({ assigneeId: e.target.value || undefined })}
            className="px-3.5 py-2 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-xs font-medium text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition-all shadow-2xs max-w-[150px]"
            aria-label="Filter by Assignee ID"
          />
        )}

        {/* Sort Field */}
        <select
          id="ticket-sort"
          value={filters.sortBy ?? ''}
          onChange={(e) => update({ sortBy: (e.target.value as GetTicketsParams['sortBy']) || undefined })}
          className={selectClass}
          aria-label="Sort tickets"
        >
          {sortOptions.map((opt) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>

        {/* Sort Direction Toggle */}
        {filters.sortBy && (
          <button
            onClick={() => update({ descending: !filters.descending })}
            className={`px-3 py-2 rounded-xl text-xs font-bold border transition-all shadow-2xs flex items-center gap-1.5 ${
              filters.descending
                ? 'bg-blue-50 text-blue-700 border-blue-200'
                : 'bg-white text-slate-700 border-slate-200 hover:bg-slate-50'
            }`}
            aria-pressed={filters.descending ?? false}
            title="Toggle sort direction"
          >
            <span>{filters.descending ? '↓ Descending' : '↑ Ascending'}</span>
          </button>
        )}

        {/* Reset Filters */}
        {hasActiveFilters && (
          <button
            onClick={() => onChange({ page: 1, pageSize: filters.pageSize })}
            className="px-3 py-2 text-xs font-bold text-red-600 hover:text-red-700 hover:bg-red-50/60 rounded-xl transition-colors"
          >
            Reset Filters
          </button>
        )}
      </div>
    </div>
  );
}
