'use client';

import { useState, useEffect, useCallback } from 'react';
import Link from 'next/link';
import { TicketDetail, Category, Agent } from '@/types/api';
import { StatusBadge } from '@/components/StatusBadge';
import { PriorityBadge } from '@/components/PriorityBadge';
import { SlaBadge } from '@/components/SlaBadge';
import { CommentList } from './CommentList';
import { AddCommentForm } from './AddCommentForm';
import { AttachmentList } from './AttachmentList';
import { AttachmentUpload } from './AttachmentUpload';
import { AgentControls } from './AgentControls';
import { ticketsApi } from '../api';
import { agentsApi } from '@/features/agents/api';
import { auth } from '@/lib/auth';
import { showToast } from '@/components/Toast';

interface TicketDetailViewProps {
  ticket: TicketDetail;
  category: Category | undefined;
  role: string;
  onRefresh: () => void;
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function canReopen(ticket: TicketDetail, role: string): boolean {
  if (ticket.status !== 'Closed') return false;
  if (role !== 'Customer') return false;
  if (!ticket.closedOnUtc) return false;
  const closedAt = new Date(ticket.closedOnUtc);
  const daysSinceClosed = (Date.now() - closedAt.getTime()) / (1000 * 60 * 60 * 24);
  return daysSinceClosed <= 14;
}

export function TicketDetailView({ ticket, category, role, onRefresh }: TicketDetailViewProps) {
  const [reopening, setReopening] = useState(false);
  const [agents, setAgents] = useState<Agent[]>([]);
  const isCustomer = role === 'Customer';
  const isClosed = ticket.status === 'Closed';
  const isOpen = !isClosed;

  useEffect(() => {
    const token = auth.getToken();
    if (token) {
      agentsApi.getAgents(token).then(setAgents).catch(() => {});
    }
  }, []);

  const handleReopen = async () => {
    const token = auth.getToken();
    if (!token) return;
    setReopening(true);
    try {
      await ticketsApi.reopenTicket(token, ticket.id);
      showToast('success', 'Ticket reopened successfully.');
      onRefresh();
    } catch {
      showToast('error', 'Failed to reopen ticket. The 14-day window may have expired.');
    } finally {
      setReopening(false);
    }
  };

  const handleCommentAdded = useCallback(() => {
    onRefresh();
  }, [onRefresh]);

  const handleAttachmentChange = useCallback(() => {
    onRefresh();
  }, [onRefresh]);

  return (
    <div className="space-y-6 max-w-6xl mx-auto">
      {/* Top Breadcrumb & Actions */}
      <div className="flex items-center justify-between gap-4 flex-wrap pb-1">
        <Link
          href="/tickets"
          className="inline-flex items-center gap-1.5 text-xs font-bold text-slate-600 hover:text-blue-600 transition-colors group"
        >
          <span className="group-hover:-translate-x-0.5 transition-transform">←</span> Back to tickets
        </Link>
        <div className="flex items-center gap-2">
          <span className="font-mono text-xs font-bold text-slate-400 bg-slate-100 px-2.5 py-1 rounded-lg border border-slate-200/60">
            #{ticket.ticketNumber}
          </span>
          <StatusBadge status={ticket.status} />
          <PriorityBadge priority={ticket.priority} />
        </div>
      </div>

      {/* Main Grid: 2 columns on lg screens */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Column: Description, Attachments, Comments (7 cols) */}
        <div className="lg:col-span-8 space-y-6">
          {/* Ticket Header & Description Card */}
          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-6 sm:p-7 space-y-5">
            <div>
              <h1 className="text-xl sm:text-2xl font-extrabold text-slate-900 leading-tight">
                {ticket.title}
              </h1>
              <p className="text-xs text-slate-400 mt-1">
                Opened by {isCustomer ? 'you' : `Customer (${ticket.reporterId ? ticket.reporterId.split('-')[0] : 'User'})`} on {formatDateTime(ticket.createdOnUtc)}
              </p>
            </div>

            <hr className="border-slate-100" />

            <div>
              <h2 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-2">Description</h2>
              <div className="text-sm text-slate-800 leading-relaxed whitespace-pre-wrap bg-slate-50/60 p-4.5 rounded-xl border border-slate-200/60">
                {ticket.description}
              </div>
            </div>
          </div>

          {/* Reopen Banner (Customer only) */}
          {canReopen(ticket, role) && (
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 bg-amber-50/80 border border-amber-200/80 rounded-2xl">
              <div className="space-y-0.5">
                <p className="text-sm font-bold text-amber-900">This ticket is currently closed.</p>
                <p className="text-xs text-amber-700">You can reopen this ticket within 14 days of closing if your issue persists.</p>
              </div>
              <button
                onClick={handleReopen}
                disabled={reopening}
                className="px-4 py-2 bg-amber-600 hover:bg-amber-700 disabled:opacity-60 text-white text-xs font-bold rounded-xl shadow-xs transition-colors self-start sm:self-center"
              >
                {reopening ? 'Reopening…' : 'Reopen Ticket'}
              </button>
            </div>
          )}

          {/* Attachments Section */}
          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-6 sm:p-7 space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                <span>Attachments</span>
                <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                  {ticket.attachments.length}/5
                </span>
              </h2>
            </div>

            <AttachmentList
              ticketId={ticket.id}
              attachments={ticket.attachments}
              onDeleted={handleAttachmentChange}
              canDelete={isOpen}
            />

            {isOpen && (
              <div className="pt-3 border-t border-slate-100">
                <AttachmentUpload
                  ticketId={ticket.id}
                  currentCount={ticket.attachments.length}
                  onUploaded={handleAttachmentChange}
                />
              </div>
            )}
          </div>

          {/* Comments Section */}
          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-6 sm:p-7 space-y-5">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                <span>Activity & Comments</span>
                <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                  {isCustomer ? ticket.comments.filter((c) => !c.isInternal).length : ticket.comments.length}
                </span>
              </h2>
            </div>

            <CommentList
              comments={ticket.comments}
              customerView={isCustomer}
              reporterId={ticket.reporterId}
              agents={agents}
            />

            {isOpen ? (
              <div className="pt-4 border-t border-slate-100">
                <AddCommentForm
                  ticketId={ticket.id}
                  allowInternal={!isCustomer}
                  onCommentAdded={handleCommentAdded}
                />
              </div>
            ) : (
              <div className="p-3.5 bg-slate-50 border border-slate-200/80 rounded-xl text-xs font-medium text-slate-500 text-center">
                This ticket is closed. Commenting is disabled.
              </div>
            )}
          </div>
        </div>

        {/* Right Column: Metadata, SLA, Agent Controls (5 cols) */}
        <div className="lg:col-span-4 space-y-6">
          {/* Agent Action Controls (Agents/Admins only) */}
          {role !== 'Customer' && (
            <AgentControls ticket={ticket} onRefresh={onRefresh} />
          )}

          {/* SLA Card */}
          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-5 space-y-3">
            <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider">Service Level Agreement</h3>
            <div>
              <SlaBadge slaStatus={ticket.slaStatus} deadline={ticket.responseDeadlineUtc} />
            </div>
            <p className="text-xs text-slate-500 leading-relaxed pt-1">
              Target response time based on category: <span className="font-semibold text-slate-700">{category ? `${category.responseTimeTargetHours} hours` : 'Standard'}</span>.
            </p>
          </div>

          {/* Ticket Information Card */}
          <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-5 space-y-4">
            <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider">Ticket Details</h3>

            <dl className="space-y-3.5 text-xs">
              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500 font-medium">Category</dt>
                <dd className="font-semibold text-slate-800 text-right">
                  {category?.name ?? 'General'}
                </dd>
              </div>

              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500 font-medium">Priority</dt>
                <dd className="text-right">
                  <PriorityBadge priority={ticket.priority} />
                </dd>
              </div>

              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500 font-medium">Status</dt>
                <dd className="text-right">
                  <StatusBadge status={ticket.status} />
                </dd>
              </div>

              {ticket.assigneeId && (
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-slate-500 font-medium">Assigned Agent</dt>
                  <dd className="font-mono text-[11px] text-slate-700 bg-slate-100 px-2 py-0.5 rounded text-right truncate max-w-[120px]" title={ticket.assigneeId}>
                    {ticket.assigneeId.split('-')[0]}…
                  </dd>
                </div>
              )}

              <hr className="border-slate-100" />

              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500 font-medium">Created On</dt>
                <dd className="text-slate-700 text-right font-medium">{formatDateTime(ticket.createdOnUtc)}</dd>
              </div>

              {ticket.closedOnUtc && (
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-slate-500 font-medium">Closed On</dt>
                  <dd className="text-slate-700 text-right font-medium">{formatDateTime(ticket.closedOnUtc)}</dd>
                </div>
              )}

              {ticket.modifiedOnUtc && (
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-slate-500 font-medium">Last Activity</dt>
                  <dd className="text-slate-700 text-right font-medium">{formatDateTime(ticket.modifiedOnUtc)}</dd>
                </div>
              )}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
