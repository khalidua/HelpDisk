'use client';

import { useRef, useState } from 'react';
import { attachmentsApi } from '@/features/attachments/api';
import { auth } from '@/lib/auth';
import { showToast } from '@/components/Toast';
import { Spinner } from '@/components/Spinner';

const ALLOWED_TYPES = [
  'image/jpeg',
  'image/png',
  'application/pdf',
  'text/plain',
  'application/zip',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
];
const ALLOWED_EXTENSIONS = '.jpg,.jpeg,.png,.pdf,.txt,.zip,.docx,.xlsx';
const MAX_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB
const MAX_ATTACHMENTS = 5;

interface AttachmentUploadProps {
  ticketId: string;
  currentCount: number;
  onUploaded: () => void;
}

export function AttachmentUpload({ ticketId, currentCount, onUploaded }: AttachmentUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  const atLimit = currentCount >= MAX_ATTACHMENTS;

  const handleFile = async (file: File) => {
    setValidationError(null);

    if (!ALLOWED_TYPES.includes(file.type)) {
      setValidationError('File type not supported. Permitted: JPG, PNG, PDF, TXT, ZIP, DOCX, XLSX.');
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      setValidationError('File exceeds the 10 MB maximum limit.');
      return;
    }
    if (currentCount >= MAX_ATTACHMENTS) {
      setValidationError('Maximum of 5 attachments per ticket reached.');
      return;
    }

    const token = auth.getToken();
    if (!token) return;

    setUploading(true);
    try {
      await attachmentsApi.uploadAttachment(token, ticketId, file);
      showToast('success', `"${file.name}" uploaded successfully.`);
      if (inputRef.current) inputRef.current.value = '';
      onUploaded();
    } catch {
      showToast('error', 'Failed to upload file. Please try again.');
    } finally {
      setUploading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const file = e.dataTransfer.files?.[0];
    if (file) handleFile(file);
  };

  return (
    <div className="space-y-2">
      {atLimit ? (
        <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-xl text-xs font-semibold text-slate-500 text-center">
          Maximum of {MAX_ATTACHMENTS} attachments reached for this ticket.
        </div>
      ) : (
        <div
          onDragOver={(e) => e.preventDefault()}
          onDrop={handleDrop}
          className="relative flex flex-col items-center justify-center gap-2.5 p-6 border-2 border-dashed border-slate-200 hover:border-blue-400 rounded-2xl bg-slate-50/50 hover:bg-blue-50/30 transition-all cursor-pointer group"
          role="button"
          tabIndex={0}
          aria-label="Upload attachment"
          onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click(); }}
          onClick={() => inputRef.current?.click()}
        >
          {uploading ? (
            <div className="flex items-center gap-2 text-xs font-bold text-slate-600 py-2">
              <Spinner size="sm" /> Uploading attachment…
            </div>
          ) : (
            <>
              <div className="w-10 h-10 rounded-xl bg-white text-slate-400 group-hover:text-blue-600 group-hover:scale-105 border border-slate-200/80 shadow-2xs flex items-center justify-center transition-all">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.75}
                    d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
              </div>
              <div className="text-center">
                <p className="text-xs font-bold text-slate-700">
                  Drop a file here, or <span className="text-blue-600 hover:underline">browse</span>
                </p>
                <p className="text-[11px] text-slate-400 mt-0.5">
                  PDF, DOCX, XLSX, PNG, JPG, ZIP (Max 10 MB) · {MAX_ATTACHMENTS - currentCount} remaining
                </p>
              </div>
            </>
          )}
          <input
            ref={inputRef}
            type="file"
            accept={ALLOWED_EXTENSIONS}
            onChange={handleChange}
            className="sr-only"
            disabled={uploading || atLimit}
            aria-hidden="true"
            tabIndex={-1}
          />
        </div>
      )}

      {validationError && (
        <p role="alert" className="text-red-600 text-xs font-semibold">{validationError}</p>
      )}
    </div>
  );
}
