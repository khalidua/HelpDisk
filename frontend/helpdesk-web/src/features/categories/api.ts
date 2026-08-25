import { fetchClient } from '@/lib/api/client';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/api';

export const categoriesApi = {
  getCategories: (token: string) =>
    fetchClient<Category[]>('/api/categories', {
      method: 'GET',
      token,
    }),

  createCategory: (token: string, data: CreateCategoryRequest) =>
    fetchClient<string>('/api/categories', {
      method: 'POST',
      token,
      body: JSON.stringify(data),
    }),

  updateCategory: (token: string, categoryId: string, data: UpdateCategoryRequest) =>
    fetchClient<void>(`/api/categories/${categoryId}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(data),
    }),

  deleteCategory: (token: string, categoryId: string) =>
    fetchClient<void>(`/api/categories/${categoryId}`, {
      method: 'DELETE',
      token,
    }),
};
