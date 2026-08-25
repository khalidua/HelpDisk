'use client';

import { useState } from 'react';
import { Attachment } from '@/types/api';
import { attachmentsApi } from '@/features/attachments/api';
import { auth } from '@/lib/auth';
import { showToast } from '@/components/Toast';
import { Spinner } from '@/components/Spinner';
import { ApiError } from '@/lib/api/errors';

interface AttachmentListProps {
  ticketId: string;
  attachments: Attachment[];
  onDeleted?: () => void;
  canDelete?: boolean;
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function FileIcon({ contentType }: { contentType: string }) {
  if (contentType.startsWith('image/')) {
    return (
      <div className="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 flex items-center justify-center flex-shrink-0 border border-emerald-200/60">
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
      </div>
    );
  }
  if (contentType === 'application/pdf') {
    return (
      <div className="w-8 h-8 rounded-lg bg-red-50 text-red-600 flex items-center justify-center flex-shrink-0 border border-red-200/60">
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
        </svg>
      </div>
    );
  }
  if (contentType === 'application/zip') {
    return (
      <div className="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 flex items-center justify-center flex-shrink-0 border border-amber-200/60">
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8} d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4" />
        </svg>
      </div>
    );
  }
  return (
    <div className="w-8 h-8 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center flex-shrink-0 border border-blue-200/60">
      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
    </div>
  );
}

export function AttachmentList({ ticketId, attachments, onDeleted, canDelete = false }: AttachmentListProps) {
  const [downloading, setDownloading] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null);

  const handleDownload = async (att: Attachment) => {
    const token = auth.getToken();
    if (!token) return;
    setDownloading(att.id);
    try {
      const blob = await attachmentsApi.downloadAttachmentBlob(token, ticketId, att.id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = att.fileName;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      showToast('error', 'Failed to download file.');
    } finally {
      setDownloading(null);
    }
  };

  const handleDelete = async (att: Attachment) => {
    if (!confirm(`Delete "${att.fileName}"?`)) return;
    const token = auth.getToken();
    if (!token) return;
    setDeleting(att.id);
    try {
      await attachmentsApi.deleteAttachment(token, ticketId, att.id);
      showToast('success', `"${att.fileName}" deleted.`);
      onDeleted?.();
    } catch (err) {
      const apiErr = err as ApiError;
      showToast('error', apiErr?.problem?.detail || 'Failed to delete attachment.');
    } finally {
      setDeleting(null);
    }
  };

  if (attachments.length === 0) {
    return (
      <div className="py-6 text-center bg-slate-50/50 rounded-xl border border-dashed border-slate-200">
        <p className="text-xs font-semibold text-slate-400">No attachments uploaded</p>
      </div>
    );
  }

  return (
    <ul className="divide-y divide-slate-100" aria-label="Attachments list">
      {attachments.map((att) => (
        <li key={att.id} className="flex items-center gap-3 py-3 group">
          <FileIcon contentType={att.contentType} />
          <div className="flex-1 min-w-0">
            <p className="text-xs sm:text-sm font-semibold text-slate-800 truncate">{att.fileName}</p>
            <p className="text-[11px] text-slate-400 font-medium">{formatSize(att.fileSize)}</p>
          </div>
          <div className="flex items-center gap-1.5 flex-shrink-0">
            <button
              onClick={() => handleDownload(att)}
              disabled={downloading === att.id}
              className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-bold text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-xl border border-blue-200/80 transition-colors disabled:opacity-50"
              aria-label={`Download ${att.fileName}`}
            >
              {downloading === att.id ? <Spinner size="sm" /> : (
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                </svg>
              )}
              Download
            </button>
            {canDelete && (
              <button
                onClick={() => handleDelete(att)}
                disabled={deleting === att.id}
                className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-bold text-red-700 bg-red-50 hover:bg-red-100 rounded-xl border border-red-200/80 transition-colors disabled:opacity-50"
                aria-label={`Delete ${att.fileName}`}
              >
                {deleting === att.id ? <Spinner size="sm" className="border-red-600" /> : 'Delete'}
              </button>
            )}
          </div>
        </li>
      ))}
    </ul>
  );
}
