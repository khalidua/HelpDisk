'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { ToastContainer } from '@/components/Toast';
import { CreateTicketForm } from '@/features/tickets/components/CreateTicketForm';

export default function NewTicketPage() {
  return (
    <RoleGuard>
      <AppShell title="New Ticket">
        <div className="max-w-2xl mx-auto">
          <div className="mb-6">
            <h1 className="text-2xl sm:text-3xl font-bold text-slate-900">Submit a New Ticket</h1>
            <p className="text-slate-500 text-sm mt-1">
              Describe your issue and we'll get back to you as soon as possible.
            </p>
          </div>
          <div className="bg-white border border-slate-200 rounded-xl shadow-sm p-6 sm:p-8">
            <CreateTicketForm />
          </div>
        </div>
      </AppShell>
      <ToastContainer />
    </RoleGuard>
  );
}
