import { fetchClient } from '@/lib/api/client';
import { RegisterRequest, LoginRequest, TokenResponse } from '@/types/api';

export const authApi = {
  register: (data: RegisterRequest) =>
    fetchClient<string>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  login: (data: LoginRequest) =>
    fetchClient<TokenResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
};
