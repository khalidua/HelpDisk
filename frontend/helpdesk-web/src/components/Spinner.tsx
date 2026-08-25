'use client';

interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  label?: string;
  className?: string;
}

const sizes = {
  sm: 'w-4 h-4 border-2',
  md: 'w-6 h-6 border-2.5',
  lg: 'w-8 h-8 border-3',
};

export function Spinner({ size = 'md', label = 'Loading…', className = 'border-blue-600' }: SpinnerProps) {
  return (
    <div className="inline-flex items-center justify-center" role="status" aria-label={label}>
      <div className={`${sizes[size]} ${className} border-t-transparent rounded-full animate-spin`} />
      <span className="sr-only">{label}</span>
    </div>
  );
}

export function PageSpinner() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[35vh] gap-3">
      <div className="relative flex items-center justify-center">
        <div className="w-10 h-10 border-3 border-blue-100 border-t-blue-600 rounded-full animate-spin" />
        <div className="absolute w-5 h-5 bg-blue-600/10 rounded-full animate-pulse" />
      </div>
      <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Loading</p>
    </div>
  );
}
