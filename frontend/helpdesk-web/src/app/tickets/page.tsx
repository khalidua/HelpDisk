'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { ToastContainer } from '@/components/Toast';
import { auth } from '@/lib/auth';
import { ticketsApi, GetTicketsParams } from '@/features/tickets/api';
import { categoriesApi } from '@/features/categories/api';
import { PaginatedList, TicketListItem, Category, TicketStatus, TicketPriority } from '@/types/api';
import { TicketFilters } from '@/features/tickets/components/TicketFilters';
import { TicketTable } from '@/features/tickets/components/TicketTable';
import { Pagination } from '@/components/Pagination';
import { PageSpinner } from '@/components/Spinner';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';

import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';

function TicketsContent() {
  const searchParams = useSearchParams();
  
  const [filters, setFilters] = useState<GetTicketsParams>({ 
    page: 1, 
    pageSize: 15 
  });
  const [result, setResult] = useState<PaginatedList<TicketListItem> | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [role, setRole] = useState<string | null>(null);

  useEffect(() => {
    const status = searchParams?.get('status') as TicketStatus | null;
    const priority = searchParams?.get('priority') as TicketPriority | null;
    const assigneeId = searchParams?.get('assigneeId');
    
    setFilters(prev => ({
      ...prev,
      page: 1,
      status: status || undefined,
      priority: priority || undefined,
      assigneeId: assigneeId || undefined,
    }));
  }, [searchParams]);

  const load = useCallback(() => {
    const token = auth.getToken();
    if (!token) return;
    setRole(auth.getRole() ?? null);
    setLoading(true);
    setError(null);
    Promise.all([
      ticketsApi.getTickets(token, filters),
      categories.length === 0 ? categoriesApi.getCategories(token) : Promise.resolve(categories),
    ])
      .then(([ticketRes, catRes]) => {
        setResult(ticketRes);
        if (categories.length === 0) setCategories(catRes as Category[]);
      })
      .catch(() => setError('Failed to load tickets. Please try again.'))
      .finally(() => setLoading(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters]);

  useEffect(() => { load(); }, [load]);

  return (
    <RoleGuard>
      <AppShell title={role === 'Customer' ? "My Tickets" : "Ticket Queue"}>
        <div className="max-w-6xl mx-auto space-y-6">
          {/* Page header */}
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div>
              <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
                {role === 'Customer' ? 'My Tickets' : 'Ticket Queue'}
              </h1>
              <p className="text-slate-500 text-sm mt-1">
                {result ? `${result.totalItems} ticket${result.totalItems !== 1 ? 's' : ''} found` : ' '}
              </p>
            </div>
            <Link
              href="/tickets/new"
              className="inline-flex items-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-xs font-bold rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-600"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              New Ticket
            </Link>
          </div>

          {/* Filters Card */}
          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-4 sm:p-5">
            <TicketFilters
              filters={filters}
              categories={categories}
              onChange={setFilters}
              role={role ?? 'Unknown'}
            />
          </div>

          {/* Content states */}
          {loading && <div className="py-12"><PageSpinner /></div>}

          {error && !loading && (
            <ErrorState message={error} onRetry={load} />
          )}

          {!loading && !error && result && result.data.length === 0 && (
            <EmptyState
              title="No tickets found"
              description={
                filters.keyword || filters.status || filters.priority || filters.categoryId || filters.assigneeId
                  ? 'No tickets match the selected filters. Try clearing or adjusting your search filters.'
                  : 'You have not submitted any tickets yet.'
              }
              action={
                <Link
                  href="/tickets/new"
                  className="inline-flex items-center gap-1.5 px-4 py-2 bg-blue-600 text-white text-xs font-bold rounded-xl hover:bg-blue-700 transition-colors shadow-2xs"
                >
                  + Create a Ticket
                </Link>
              }
            />
          )}

          {!loading && !error && result && result.data.length > 0 && (
            <div className="space-y-4">
              <TicketTable tickets={result.data} categories={categories} role={role ?? 'Unknown'} />
              <div className="flex justify-center pt-3 pb-6">
                <Pagination
                  currentPage={result.currentPage}
                  totalPages={result.totalPages}
                  hasPreviousPage={result.hasPreviousPage}
                  hasNextPage={result.hasNextPage}
                  onPageChange={(page) => setFilters((f) => ({ ...f, page }))}
                />
              </div>
            </div>
          )}
        </div>
      </AppShell>
      <ToastContainer />
    </RoleGuard>
  );
}

export default function TicketsPage() {
  return (
    <Suspense fallback={<PageSpinner />}>
      <TicketsContent />
    </Suspense>
  );
}
