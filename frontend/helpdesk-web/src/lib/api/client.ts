import { ApiError, ProblemDetails } from './errors';
import Cookies from 'js-cookie';

export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081';

export interface RequestOptions extends RequestInit {
  token?: string;
  params?: Record<string, string | number | boolean | undefined>;
}

export async function fetchClient<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const { token, params, headers, ...customConfig } = options;

  let url = `${API_BASE_URL}${endpoint}`;
  if (params) {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) {
        searchParams.append(key, String(value));
      }
    });
    const qs = searchParams.toString();
    if (qs) {
      url += `?${qs}`;
    }
  }

  const defaultHeaders: Record<string, string> = {
    'Accept': 'application/json',
  };

  if (!(customConfig.body instanceof FormData)) {
    defaultHeaders['Content-Type'] = 'application/json';
  }

  const authToken = token || (typeof window !== 'undefined' ? Cookies.get('auth_token') : undefined);
  if (authToken) {
    defaultHeaders['Authorization'] = `Bearer ${authToken}`;
  }

  const mergedHeaders: Record<string, string> = {
    ...defaultHeaders,
    ...(headers as Record<string, string>),
  };

  if (customConfig.body instanceof FormData) {
    delete mergedHeaders['Content-Type'];
    delete mergedHeaders['content-type'];
  }

  const config: RequestInit = {
    ...customConfig,
    headers: mergedHeaders,
  };

  const response = await fetch(url, config);

  if (!response.ok) {
    let problem: ProblemDetails;
    try {
      problem = await response.json();
    } catch (err) {
      // If we can't parse JSON, synthesize a ProblemDetails
      problem = {
        title: 'NetworkError',
        status: response.status,
        detail: response.statusText || 'An unexpected network error occurred.',
      };
    }

    if (response.status === 401) {
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
    }

    throw new ApiError(problem);
  }

  // Handle empty responses (like 204 No Content or endpoints returning empty body)
  const text = await response.text();
  if (!text) {
    return {} as T;
  }

  try {
    return JSON.parse(text) as T;
  } catch {
    // If it's not JSON (e.g. string guid or something), return as is
    return text as unknown as T;
  }
}
