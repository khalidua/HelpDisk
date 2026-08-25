'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { addCommentSchema, AddCommentFormData } from '../schemas';
import { commentsApi } from '@/features/comments/api';
import { auth } from '@/lib/auth';
import { ApiError } from '@/lib/api/errors';
import { showToast } from '@/components/Toast';
import { Spinner } from '@/components/Spinner';

interface AddCommentFormProps {
  ticketId: string;
  allowInternal?: boolean;
  onCommentAdded: () => void;
}

export function AddCommentForm({ ticketId, allowInternal = false, onCommentAdded }: AddCommentFormProps) {
  const [isInternal, setIsInternal] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddCommentFormData>({
    resolver: zodResolver(addCommentSchema),
  });

  const onSubmit = async (data: AddCommentFormData) => {
    setServerError(null);
    const token = auth.getToken();
    if (!token) return;

    try {
      await commentsApi.addComment(token, ticketId, {
        body: data.body,
        isInternal: allowInternal ? isInternal : false,
      });
      reset();
      setIsInternal(false);
      showToast('success', 'Comment posted successfully.');
      onCommentAdded();
    } catch (err) {
      const apiErr = err as ApiError;
      setServerError(apiErr.problem?.detail ?? 'Failed to add comment. Please try again.');
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3.5">
      {serverError && (
        <div role="alert" className="p-3.5 bg-red-50 border border-red-200/80 rounded-xl text-red-800 text-xs font-semibold">
          {serverError}
        </div>
      )}

      <div>
        <label htmlFor="comment-body" className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
          Leave a message
        </label>
        <textarea
          id="comment-body"
          rows={3}
          placeholder="Type your response or update here…"
          {...register('body')}
          className="w-full px-4 py-3 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent text-xs sm:text-sm shadow-2xs resize-y transition-all"
          aria-invalid={!!errors.body}
          aria-describedby={errors.body ? 'comment-body-error' : undefined}
        />
        {errors.body && (
          <p id="comment-body-error" className="text-red-600 text-xs font-semibold mt-1" role="alert">
            {errors.body.message}
          </p>
        )}
      </div>

      <div className="flex items-center justify-between gap-3 flex-wrap">
        {allowInternal ? (
          <label className="flex items-center gap-2 cursor-pointer text-xs text-slate-700 select-none bg-amber-50/60 hover:bg-amber-50 px-3 py-1.5 rounded-xl border border-amber-200/80 transition-colors">
            <input
              type="checkbox"
              id="comment-internal"
              checked={isInternal}
              onChange={(e) => setIsInternal(e.target.checked)}
              className="w-4 h-4 rounded border-amber-300 text-amber-600 focus:ring-amber-500 cursor-pointer"
            />
            <span className="font-bold text-amber-900">Internal Note</span>
            <span className="text-[11px] text-amber-700/80">(visible only to staff)</span>
          </label>
        ) : (
          <div />
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="inline-flex items-center gap-2 px-5 py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-60 disabled:cursor-not-allowed text-white text-xs font-bold rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-600 ml-auto"
        >
          {isSubmitting && <Spinner size="sm" className="border-white" />}
          {isSubmitting ? 'Posting…' : 'Post Response'}
        </button>
      </div>
    </form>
  );
}
