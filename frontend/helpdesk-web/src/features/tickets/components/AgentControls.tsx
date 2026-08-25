'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ticketsApi } from '../api';
import { agentsApi } from '@/features/agents/api';
import { auth } from '@/lib/auth';
import { showToast } from '@/components/Toast';
import { TicketDetail, TicketPriority, Agent } from '@/types/api';
import { Spinner } from '@/components/Spinner';

interface AgentControlsProps {
  ticket: TicketDetail;
  onRefresh: () => void;
}

const priorities: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent'];

export function AgentControls({ ticket, onRefresh }: AgentControlsProps) {
  const router = useRouter();
  const role = auth.getRole();
  const [agents, setAgents] = useState<Agent[]>([]);
  const [assigning, setAssigning] = useState(false);
  const [updating, setUpdating] = useState(false);
  const [closing, setClosing] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    const token = auth.getToken();
    if (token && role === 'Admin') {
      agentsApi.getAgents(token).then(setAgents).catch(() => {});
    }
  }, [role]);

  const handleAssign = async (assigneeId: string) => {
    const token = auth.getToken();
    if (!token) return;
    setAssigning(true);
    try {
      await ticketsApi.assignTicket(token, ticket.id, { assigneeId });
      showToast('success', 'Ticket assigned successfully.');
      onRefresh();
    } catch {
      showToast('error', 'Failed to assign ticket.');
    } finally {
      setAssigning(false);
    }
  };

  const handlePriorityChange = async (priority: TicketPriority) => {
    const token = auth.getToken();
    if (!token) return;
    setUpdating(true);
    try {
      await ticketsApi.updateTicket(token, ticket.id, {
        title: ticket.title,
        description: ticket.description,
        priority
      });
      showToast('success', 'Priority updated.');
      onRefresh();
    } catch {
      showToast('error', 'Failed to update priority.');
    } finally {
      setUpdating(false);
    }
  };

  const handleClose = async () => {
    if (!confirm('Are you sure you want to close this ticket?')) return;
    const token = auth.getToken();
    if (!token) return;
    setClosing(true);
    try {
      await ticketsApi.closeTicket(token, ticket.id);
      showToast('success', 'Ticket closed.');
      onRefresh();
    } catch {
      showToast('error', 'Failed to close ticket.');
    } finally {
      setClosing(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to permanently delete this ticket? This action cannot be undone.')) return;
    const token = auth.getToken();
    if (!token) return;
    setDeleting(true);
    try {
      await ticketsApi.deleteTicket(token, ticket.id);
      showToast('success', 'Ticket deleted successfully.');
      router.push('/tickets');
    } catch {
      showToast('error', 'Failed to delete ticket.');
      setDeleting(false);
    }
  };

  const isClosed = ticket.status === 'Closed';

  return (
    <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-5 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider">Agent Controls</h3>
        {role === 'Admin' && (
          <button
            onClick={handleDelete}
            disabled={deleting}
            className="inline-flex items-center gap-1 text-xs font-bold text-red-600 hover:text-red-700 hover:bg-red-50 px-2 py-1 rounded-lg transition-colors disabled:opacity-50"
          >
            {deleting ? 'Deleting…' : 'Delete Ticket'}
          </button>
        )}
      </div>

      {isClosed ? (
        <p className="text-xs text-slate-500 italic bg-slate-50 p-3 rounded-xl border border-slate-200/60">
          This ticket is closed. Controls are disabled.
        </p>
      ) : (
        <div className="space-y-3.5">
          {/* Assign To */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Assign Agent
            </label>
            {role === 'Admin' ? (
              <div className="relative">
                <select
                  disabled={assigning}
                  value={ticket.assigneeId ?? ''}
                  onChange={(e) => handleAssign(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50/60 hover:bg-white focus:bg-white border border-slate-200 rounded-xl text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-600 disabled:opacity-60 transition-all cursor-pointer shadow-2xs"
                >
                  <option value="">Unassigned</option>
                  {agents.map((a) => (
                    <option key={a.userId} value={a.userId}>
                      {a.firstName} {a.lastName}
                    </option>
                  ))}
                </select>
              </div>
            ) : (
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-500">
                  {ticket.assigneeId ? (ticket.assigneeId === auth.getUserId() ? 'Assigned to you' : 'Assigned to another agent') : 'Unassigned'}
                </span>
                {ticket.assigneeId !== auth.getUserId() && (
                  <button
                    onClick={() => handleAssign(auth.getUserId()!)}
                    disabled={assigning || !auth.getUserId()}
                    className="px-3 py-1.5 bg-blue-50 text-blue-600 hover:bg-blue-100 text-xs font-bold rounded-lg transition-colors disabled:opacity-50"
                  >
                    Assign to Me
                  </button>
                )}
              </div>
            )}
          </div>

          {/* Change Priority */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Ticket Priority
            </label>
            <select
              disabled={updating}
              value={ticket.priority}
              onChange={(e) => handlePriorityChange(e.target.value as TicketPriority)}
              className="w-full px-3 py-2 bg-slate-50/60 hover:bg-white focus:bg-white border border-slate-200 rounded-xl text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-600 disabled:opacity-60 transition-all cursor-pointer shadow-2xs"
            >
              {priorities.map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>

          {/* Close Ticket Button */}
          <div className="pt-1">
            <button
              onClick={handleClose}
              disabled={closing}
              className="w-full flex items-center justify-center gap-1.5 px-4 py-2.5 bg-slate-900 hover:bg-black active:bg-slate-800 disabled:opacity-60 text-white text-xs font-bold rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-slate-900"
            >
              {closing && <Spinner size="sm" className="border-white" />}
              {closing ? 'Closing Ticket…' : 'Mark as Closed'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
