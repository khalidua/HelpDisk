'use client';

import { useEffect, useCallback, useState } from 'react';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

let _listeners: Array<(toast: Toast) => void> = [];

export function showToast(type: ToastType, message: string) {
  const toast: Toast = { id: Date.now().toString(), type, message };
  _listeners.forEach((fn) => fn(toast));
}

export function useToasts() {
  const [toasts, setToasts] = useState<Toast[]>([]);

  useEffect(() => {
    const handler = (toast: Toast) => {
      setToasts((prev) => [...prev, toast]);
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== toast.id));
      }, 4000);
    };
    _listeners.push(handler);
    return () => {
      _listeners = _listeners.filter((fn) => fn !== handler);
    };
  }, []);

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return { toasts, dismiss };
}

const typeStyles: Record<ToastType, { bg: string; text: string; border: string; iconBg: string }> = {
  success: {
    bg: 'bg-emerald-900/90 text-white',
    text: 'text-white',
    border: 'border-emerald-700/60',
    iconBg: 'bg-emerald-500/20 text-emerald-300',
  },
  error: {
    bg: 'bg-red-900/90 text-white',
    text: 'text-white',
    border: 'border-red-700/60',
    iconBg: 'bg-red-500/20 text-red-300',
  },
  info: {
    bg: 'bg-slate-900/90 text-white',
    text: 'text-white',
    border: 'border-slate-700/60',
    iconBg: 'bg-slate-500/20 text-slate-300',
  },
};

const SuccessIcon = () => (
  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 13l4 4L19 7" />
  </svg>
);

const ErrorIcon = () => (
  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
  </svg>
);

const InfoIcon = () => (
  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
);

const typeIcons: Record<ToastType, React.ReactNode> = {
  success: <SuccessIcon />,
  error: <ErrorIcon />,
  info: <InfoIcon />,
};

export function ToastContainer() {
  const { toasts, dismiss } = useToasts();

  if (toasts.length === 0) return null;

  return (
    <div
      aria-live="assertive"
      className="fixed bottom-5 right-5 z-50 flex flex-col gap-2.5 max-w-sm w-full pointer-events-none"
    >
      {toasts.map((toast) => {
        const style = typeStyles[toast.type] ?? typeStyles.info;
        return (
          <div
            key={toast.id}
            role="alert"
            className={`pointer-events-auto flex items-center gap-3 px-4 py-3.5 rounded-2xl shadow-xl backdrop-blur-md border text-sm font-medium transition-all animate-in slide-in-from-bottom-3 duration-200 ${style.bg} ${style.border}`}
          >
            <span aria-hidden="true" className={`p-1.5 rounded-xl flex-shrink-0 ${style.iconBg}`}>
              {typeIcons[toast.type]}
            </span>
            <p className="flex-1 text-xs sm:text-sm font-medium leading-snug">{toast.message}</p>
            <button
              onClick={() => dismiss(toast.id)}
              className="p-1 rounded-lg opacity-70 hover:opacity-100 hover:bg-white/10 transition-opacity"
              aria-label="Dismiss notification"
            >
              ✕
            </button>
          </div>
        );
      })}
    </div>
  );
}
