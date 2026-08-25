'use client';

import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import { createTicketSchema, CreateTicketFormData } from '../schemas';
import { ticketsApi } from '../api';
import { categoriesApi } from '@/features/categories/api';
import { auth } from '@/lib/auth';
import { Category } from '@/types/api';
import { ApiError } from '@/lib/api/errors';
import { showToast } from '@/components/Toast';
import { Spinner } from '@/components/Spinner';

const inputClass =
  'w-full px-4 py-2.5 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent text-xs sm:text-sm shadow-2xs transition-all';
const labelClass = 'block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5';
const errorClass = 'text-red-600 text-xs font-semibold mt-1';

export function CreateTicketForm() {
  const router = useRouter();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(true);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateTicketFormData>({
    resolver: zodResolver(createTicketSchema),
  });

  useEffect(() => {
    const token = auth.getToken();
    if (!token) return;
    categoriesApi
      .getCategories(token)
      .then(setCategories)
      .catch(() => setCategories([]))
      .finally(() => setLoadingCategories(false));
  }, []);

  const onSubmit = async (data: CreateTicketFormData) => {
    setServerError(null);
    const token = auth.getToken();
    if (!token) return;

    try {
      const ticketId = await ticketsApi.createTicket(token, data);
      showToast('success', 'Ticket created successfully!');
      router.push(`/tickets/${ticketId}`);
    } catch (err) {
      const apiErr = err as ApiError;
      setServerError(apiErr.problem?.detail ?? 'Failed to create ticket. Please try again.');
    }
  };

  return (
    <div className="bg-white border border-slate-200/80 rounded-2xl shadow-xs p-6 sm:p-8">
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
        {serverError && (
          <div role="alert" className="p-4 bg-red-50 border border-red-200/80 rounded-xl text-red-800 text-xs font-semibold">
            {serverError}
          </div>
        )}

        {/* Title */}
        <div>
          <label htmlFor="ticket-title" className={labelClass}>
            Ticket Title <span className="text-red-500" aria-hidden="true">*</span>
          </label>
          <input
            id="ticket-title"
            type="text"
            placeholder="Brief summary of your request or issue"
            {...register('title')}
            className={inputClass}
            aria-invalid={!!errors.title}
            aria-describedby={errors.title ? 'title-error' : undefined}
          />
          {errors.title && <p id="title-error" className={errorClass} role="alert">{errors.title.message}</p>}
        </div>

        {/* Description */}
        <div>
          <label htmlFor="ticket-description" className={labelClass}>
            Detailed Description <span className="text-red-500" aria-hidden="true">*</span>
          </label>
          <textarea
            id="ticket-description"
            rows={5}
            placeholder="Please provide steps to reproduce, error messages, or context…"
            {...register('description')}
            className={`${inputClass} resize-y leading-relaxed`}
            aria-invalid={!!errors.description}
            aria-describedby={errors.description ? 'desc-error' : undefined}
          />
          {errors.description && <p id="desc-error" className={errorClass} role="alert">{errors.description.message}</p>}
        </div>

        {/* Priority & Category side by side on sm+ */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
          {/* Priority */}
          <div>
            <label htmlFor="ticket-priority" className={labelClass}>
              Priority <span className="text-red-500" aria-hidden="true">*</span>
            </label>
            <select
              id="ticket-priority"
              {...register('priority')}
              className={inputClass}
              aria-invalid={!!errors.priority}
              aria-describedby={errors.priority ? 'priority-error' : undefined}
            >
              <option value="">Select priority…</option>
              <option value="Low">Low — Non-critical inquiry</option>
              <option value="Normal">Normal — Standard request</option>
              <option value="High">High — Impeding productivity</option>
              <option value="Urgent">Urgent — System down / critical</option>
            </select>
            {errors.priority && <p id="priority-error" className={errorClass} role="alert">{errors.priority.message}</p>}
          </div>

          {/* Category */}
          <div>
            <label htmlFor="ticket-category" className={labelClass}>
              Category <span className="text-red-500" aria-hidden="true">*</span>
            </label>
            {loadingCategories ? (
              <div className="flex items-center gap-2 py-2 text-xs font-semibold text-slate-500">
                <Spinner size="sm" /> Loading categories…
              </div>
            ) : (
              <select
                id="ticket-category"
                {...register('categoryId')}
                className={inputClass}
                aria-invalid={!!errors.categoryId}
                aria-describedby={errors.categoryId ? 'category-error' : undefined}
              >
                <option value="">Select category…</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.name} (SLA: {cat.responseTimeTargetHours}h target)
                  </option>
                ))}
              </select>
            )}
            {errors.categoryId && <p id="category-error" className={errorClass} role="alert">{errors.categoryId.message}</p>}
          </div>
        </div>

        {/* Form Actions */}
        <div className="flex items-center gap-3 pt-3 border-t border-slate-100">
          <button
            type="submit"
            disabled={isSubmitting || loadingCategories}
            className="inline-flex items-center gap-2 px-6 py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-60 disabled:cursor-not-allowed text-white text-xs font-bold rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-600"
          >
            {isSubmitting && <Spinner size="sm" className="border-white" />}
            {isSubmitting ? 'Creating Ticket…' : 'Submit Ticket'}
          </button>
          <button
            type="button"
            onClick={() => router.back()}
            className="px-5 py-2.5 text-xs font-bold text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-xl transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
