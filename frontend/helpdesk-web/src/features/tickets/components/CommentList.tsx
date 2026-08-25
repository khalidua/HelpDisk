'use client';

import { useState, useEffect } from 'react';
import { Comment, Agent } from '@/types/api';
import { auth } from '@/lib/auth';

interface CommentListProps {
  comments: Comment[];
  customerView?: boolean;
  reporterId?: string;
  agents?: Agent[];
}

function decodeJwtPayload(token: string): Record<string, string> | null {
  try {
    const [, payload] = token.split('.');
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded) as Record<string, string>;
  } catch {
    return null;
  }
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getInitials(name: string, fallbackId: string): string {
  if (name && name !== 'User' && name !== 'Customer') {
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }
  return fallbackId.substring(0, 2).toUpperCase();
}

export function CommentList({ comments, customerView = false, reporterId, agents = [] }: CommentListProps) {
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [currentUserName, setCurrentUserName] = useState<string | null>(null);

  useEffect(() => {
    const token = auth.getToken();
    if (token) {
      const claims = decodeJwtPayload(token);
      const uid =
        claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
        claims?.sub ??
        null;
      const name =
        claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        claims?.name ??
        ((claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ?? '') + ' ' + (claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] ?? '')).trim();

      setCurrentUserId(uid);
      if (name) setCurrentUserName(name);
    }
  }, []);

  const getAuthorDisplay = (authorId: string) => {
    // 1. Current user
    if (currentUserId && authorId === currentUserId && currentUserName) {
      return currentUserName;
    }
    // 2. Matching agent
    if (agents.length > 0) {
      const agent = agents.find((a) => a.userId === authorId);
      if (agent) {
        return `${agent.firstName} ${agent.lastName}`;
      }
    }
    // 3. Reporter / Customer
    if (reporterId && authorId === reporterId) {
      return 'Customer';
    }
    return 'User';
  };

  const visible = customerView ? comments.filter((c) => !c.isInternal) : comments;

  if (visible.length === 0) {
    return (
      <div className="py-8 text-center bg-slate-50/60 rounded-xl border border-dashed border-slate-200">
        <p className="text-xs font-semibold text-slate-400">No comments posted yet</p>
        <p className="text-[11px] text-slate-400 mt-0.5">Start the conversation by adding a message below.</p>
      </div>
    );
  }

  return (
    <ol className="space-y-4" aria-label="Comments list">
      {visible.map((comment) => {
        const authorName = getAuthorDisplay(comment.authorId);
        const initials = getInitials(authorName, comment.authorId);

        return (
          <li key={comment.id} className="flex items-start gap-3 group">
            {/* Avatar */}
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-extrabold flex-shrink-0 shadow-2xs ${
                comment.isInternal
                  ? 'bg-amber-100 text-amber-800 border border-amber-300'
                  : 'bg-gradient-to-tr from-slate-700 to-slate-900 text-white'
              }`}
              aria-hidden="true"
            >
              {initials}
            </div>

            <div className="flex-1 min-w-0 space-y-1">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-xs font-bold text-slate-900">
                  {authorName}{' '}
                  <span className="font-mono text-[11px] text-slate-400 font-normal">
                    ({comment.authorId})
                  </span>
                </span>
                <span className="text-[11px] text-slate-400 font-medium">
                  {formatDateTime(comment.createdOnUtc)}
                </span>
                {!customerView && comment.isInternal && (
                  <span className="inline-flex items-center gap-1 px-2 py-0.2 rounded-full text-[10px] font-extrabold bg-amber-100 text-amber-800 border border-amber-300 shadow-2xs">
                    <svg className="w-2.5 h-2.5 text-amber-700" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                      <path fillRule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clipRule="evenodd" />
                    </svg>
                    Internal Note
                  </span>
                )}
              </div>

              <div
                className={`p-4 rounded-2xl text-xs sm:text-sm leading-relaxed whitespace-pre-wrap ${
                  comment.isInternal && !customerView
                    ? 'bg-amber-50/70 border border-amber-200/90 text-amber-950 shadow-2xs'
                    : 'bg-slate-50/80 border border-slate-200/80 text-slate-800'
                }`}
              >
                {comment.body}
              </div>
            </div>
          </li>
        );
      })}
    </ol>
  );
}
