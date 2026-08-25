import { fetchClient, API_BASE_URL } from '@/lib/api/client';

export const attachmentsApi = {
  uploadAttachment: (token: string, ticketId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    return fetchClient<string>(`/api/tickets/${ticketId}/attachments`, {
      method: 'POST',
      token,
      body: formData,
    });
  },

  downloadAttachmentUrl: (ticketId: string, attachmentId: string) => {
    // This helper returns the URL. If the endpoint requires auth, 
    // the frontend will need to use a fetch-to-Blob approach as noted in the API reference.
    return `/api/tickets/${ticketId}/attachments/${attachmentId}`;
  },

  downloadAttachmentBlob: async (token: string, ticketId: string, attachmentId: string) => {
    // Since fetchClient expects JSON/Text, for binary blobs we use native fetch
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/attachments/${attachmentId}`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      }
    );

    if (!response.ok) {
      throw new Error('Failed to download attachment');
    }

    return response.blob();
  },

  deleteAttachment: (token: string, ticketId: string, attachmentId: string) =>
    fetchClient<void>(`/api/tickets/${ticketId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      token,
    }),
};
