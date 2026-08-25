import { fetchClient } from '@/lib/api/client';
import { Agent, CreateAgentRequest, UpdateAgentRequest } from '@/types/api';

export const agentsApi = {
  getAgents: (token: string) =>
    fetchClient<Agent[]>('/api/agents', {
      method: 'GET',
      token,
    }),

  getAgentById: (token: string, userId: string) =>
    fetchClient<Agent>(`/api/agents/${userId}`, {
      method: 'GET',
      token,
    }),

  createAgent: (token: string, data: CreateAgentRequest) =>
    fetchClient<string>('/api/agents', {
      method: 'POST',
      token,
      body: JSON.stringify(data),
    }),

  updateAgent: (token: string, userId: string, data: UpdateAgentRequest) =>
    fetchClient<Agent>(`/api/agents/${userId}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(data),
    }),

  activateAgent: (token: string, userId: string) =>
    fetchClient<void>(`/api/agents/${userId}/activate`, {
      method: 'POST',
      token,
    }),

  deactivateAgent: (token: string, userId: string) =>
    fetchClient<void>(`/api/agents/${userId}/deactivate`, {
      method: 'POST',
      token,
    }),
};
