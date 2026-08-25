import Cookies from 'js-cookie';
import { TokenResponse } from '@/types/api';

const TOKEN_KEY = 'auth_token';
const ROLE_KEY = 'auth_role';

export const auth = {
  setSession: (data: TokenResponse) => {
    Cookies.set(TOKEN_KEY, data.token, { expires: new Date(data.expiresAt) });
    Cookies.set(ROLE_KEY, data.role, { expires: new Date(data.expiresAt) });
  },
  clearSession: () => {
    Cookies.remove(TOKEN_KEY);
    Cookies.remove(ROLE_KEY);
  },
  getToken: () => {
    if (typeof window !== 'undefined') {
      return Cookies.get(TOKEN_KEY);
    }
    return undefined;
  },
  getRole: () => {
    if (typeof window !== 'undefined') {
      return Cookies.get(ROLE_KEY);
    }
    return undefined;
  },
  isAuthenticated: () => {
    return !!auth.getToken();
  },
  getUserId: () => {
    const token = auth.getToken();
    if (!token) return undefined;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.sub || payload.nameid; // 'sub' or 'nameidentifier' mapped claim
    } catch {
      return undefined;
    }
  }
};
