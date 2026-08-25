import { fetchClient } from '@/lib/api/client';
import { Company } from '@/types/api';

export const companiesApi = {
  getCompanies: () =>
    fetchClient<Company[]>('/api/companies', {
      method: 'GET',
    }),
};
