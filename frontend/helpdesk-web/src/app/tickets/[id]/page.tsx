'use client';

import { use, useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { ToastContainer } from '@/components/Toast';
import { auth } from '@/lib/auth';
import { ticketsApi } from '@/features/tickets/api';
import { categoriesApi } from '@/features/categories/api';
import { TicketDetail, Category } from '@/types/api';
import { TicketDetailView } from '@/features/tickets/components/TicketDetailView';
import { PageSpinner } from '@/components/Spinner';
import { ErrorState } from '@/components/ErrorState';
import { ApiError } from '@/lib/api/errors';

interface TicketDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function TicketDetailPage({ params }: TicketDetailPageProps) {
  const { id } = use(params);
  const [ticket, setTicket] = useState<TicketDetail | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [role, setRole] = useState<string>('Customer');

  const load = useCallback(() => {
    const token = auth.getToken();
    if (!token) return;
    setRole(auth.getRole() ?? 'Customer');
    setLoading(true);
    setError(null);
    setNotFound(false);

    Promise.all([
      ticketsApi.getTicketById(token, id),
      categoriesApi.getCategories(token),
    ])
      .then(([ticketData, catData]) => {
        setTicket(ticketData);
        setCategories(catData);
      })
      .catch((err) => {
        if (err instanceof ApiError && err.problem.status === 404) {
          setNotFound(true);
        } else {
          setError('Failed to load ticket details. Please try again.');
        }
      })
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const category = ticket ? categories.find((c) => c.id === ticket.categoryId) : undefined;

  return (
    <RoleGuard>
      <AppShell title={ticket ? `Ticket #${ticket.ticketNumber}` : 'Ticket Details'}>
        {loading && <div className="py-12"><PageSpinner /></div>}

        {notFound && !loading && (
          <div className="max-w-md mx-auto my-16 p-8 bg-white border border-slate-200/80 rounded-2xl shadow-xs text-center space-y-4">
            <div className="w-14 h-14 rounded-2xl bg-slate-100 flex items-center justify-center mx-auto text-slate-400">
              <svg className="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <div className="space-y-1">
              <h1 className="text-lg font-bold text-slate-900">Ticket Not Found</h1>
              <p className="text-xs text-slate-500 max-w-xs mx-auto">
                This ticket may have been deleted, moved, or you don't have access to view it.
              </p>
            </div>
            <Link
              href="/tickets"
              className="inline-flex items-center gap-1.5 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors"
            >
              ← Return to Tickets
            </Link>
          </div>
        )}

        {error && !loading && !notFound && (
          <ErrorState message={error} onRetry={load} />
        )}

        {!loading && !error && !notFound && ticket && (
          <TicketDetailView
            ticket={ticket}
            category={category}
            role={role}
            onRefresh={load}
          />
        )}
      </AppShell>
      <ToastContainer />
    </RoleGuard>
  );
}
