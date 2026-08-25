import { fetchClient } from '@/lib/api/client';
import {
  TicketListItem,
  PaginatedList,
  TicketDetail,
  CreateTicketRequest,
  UpdateTicketRequest,
  AssignTicketRequest,
  TicketStatus,
  TicketPriority,
} from '@/types/api';

export interface GetTicketsParams {
  keyword?: string;
  status?: TicketStatus;
  priority?: TicketPriority;
  categoryId?: string;
  assigneeId?: string;
  fromDate?: string;
  toDate?: string;
  sortBy?: 'CreatedOn' | 'Priority' | 'Status';
  descending?: boolean;
  page?: number;
  pageSize?: number;
}

export const ticketsApi = {
  getTickets: (token: string, params?: GetTicketsParams) =>
    fetchClient<PaginatedList<TicketListItem>>('/api/tickets', {
      method: 'GET',
      token,
      params: params as Record<string, string | number | boolean | undefined>,
    }),

  getTicketById: (token: string, ticketId: string) =>
    fetchClient<TicketDetail>(`/api/tickets/${ticketId}`, {
      method: 'GET',
      token,
    }),

  createTicket: (token: string, data: CreateTicketRequest) =>
    fetchClient<string>('/api/tickets', {
      method: 'POST',
      token,
      body: JSON.stringify(data),
    }),

  updateTicket: (token: string, ticketId: string, data: UpdateTicketRequest) =>
    fetchClient<void>(`/api/tickets/${ticketId}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(data),
    }),

  assignTicket: (token: string, ticketId: string, data: AssignTicketRequest) =>
    fetchClient<void>(`/api/tickets/${ticketId}/assign`, {
      method: 'PUT',
      token,
      body: JSON.stringify(data),
    }),

  closeTicket: (token: string, ticketId: string) =>
    fetchClient<void>(`/api/tickets/${ticketId}/close`, {
      method: 'PUT',
      token,
    }),

  reopenTicket: (token: string, ticketId: string) =>
    fetchClient<void>(`/api/tickets/${ticketId}/reopen`, {
      method: 'PUT',
      token,
    }),

  deleteTicket: (token: string, ticketId: string) =>
    fetchClient<void>(`/api/tickets/${ticketId}`, {
      method: 'DELETE',
      token,
    }),
};
