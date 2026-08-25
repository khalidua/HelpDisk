import { fetchClient } from '@/lib/api/client';
import {
  OpenTicketsPerAgentReport,
  AverageResolutionTimeReport,
  SlaBreachesReport,
} from '@/types/api';

export const reportsApi = {
  getOpenTicketsPerAgent: (token: string) =>
    fetchClient<OpenTicketsPerAgentReport[]>('/api/reports/opened-tickets-per-agent', {
      method: 'GET',
      token,
    }),

  getAverageResolutionTimePerCategory: (token: string) =>
    fetchClient<AverageResolutionTimeReport[]>('/api/reports/average-resolution-time-per-category', {
      method: 'GET',
      token,
    }),

  getSlaBreachesThisMonth: (token: string) =>
    fetchClient<SlaBreachesReport>('/api/reports/sla-breaches-this-month', {
      method: 'GET',
      token,
    }),
};
