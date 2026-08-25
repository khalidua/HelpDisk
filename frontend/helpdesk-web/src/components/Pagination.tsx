'use client';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  onPageChange: (page: number) => void;
}

export function Pagination({
  currentPage,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  onPageChange,
}: PaginationProps) {
  if (totalPages <= 1) return null;

  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  const start = Math.max(1, currentPage - 2);
  const end = Math.min(totalPages, currentPage + 2);
  const visiblePages = pages.slice(start - 1, end);

  const btnBase =
    'flex items-center justify-center min-w-[2.25rem] h-9 px-2.5 rounded-xl text-xs font-semibold transition-all focus:outline-none focus:ring-2 focus:ring-blue-600 focus:ring-offset-1';
  const btnActive = 'bg-blue-600 text-white shadow-xs';
  const btnInactive = 'text-slate-600 hover:bg-slate-100 hover:text-slate-900 border border-slate-200/80 bg-white';
  const btnDisabled = 'text-slate-300 border border-slate-100 bg-slate-50 cursor-not-allowed';

  return (
    <nav aria-label="Pagination navigation" className="flex items-center gap-1.5 p-1 bg-slate-100/60 rounded-2xl border border-slate-200/60 w-fit">
      <button
        onClick={() => onPageChange(currentPage - 1)}
        disabled={!hasPreviousPage}
        className={`${btnBase} ${hasPreviousPage ? btnInactive : btnDisabled}`}
        aria-label="Previous page"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
        </svg>
      </button>

      {start > 1 && (
        <>
          <button onClick={() => onPageChange(1)} className={`${btnBase} ${btnInactive}`}>1</button>
          {start > 2 && <span className="px-1 text-slate-400 text-xs select-none">…</span>}
        </>
      )}

      {visiblePages.map((page) => (
        <button
          key={page}
          onClick={() => onPageChange(page)}
          className={`${btnBase} ${page === currentPage ? btnActive : btnInactive}`}
          aria-current={page === currentPage ? 'page' : undefined}
        >
          {page}
        </button>
      ))}

      {end < totalPages && (
        <>
          {end < totalPages - 1 && <span className="px-1 text-slate-400 text-xs select-none">…</span>}
          <button onClick={() => onPageChange(totalPages)} className={`${btnBase} ${btnInactive}`}>{totalPages}</button>
        </>
      )}

      <button
        onClick={() => onPageChange(currentPage + 1)}
        disabled={!hasNextPage}
        className={`${btnBase} ${hasNextPage ? btnInactive : btnDisabled}`}
        aria-label="Next page"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </nav>
  );
}
