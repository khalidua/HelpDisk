import { fetchClient } from '@/lib/api/client';
import { Comment, AddCommentRequest } from '@/types/api';

export const commentsApi = {
  getComments: (token: string, ticketId: string) =>
    fetchClient<Comment[]>(`/api/tickets/${ticketId}/comments`, {
      method: 'GET',
      token,
    }),

  addComment: (token: string, ticketId: string, data: AddCommentRequest) =>
    fetchClient<string>(`/api/tickets/${ticketId}/comments`, {
      method: 'POST',
      token,
      body: JSON.stringify(data),
    }),
};
