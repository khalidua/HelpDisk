'use client';

import { useEffect, useState } from 'react';
import { RoleGuard } from '@/components/RoleGuard';
import { AppShell } from '@/components/AppShell';
import { ToastContainer } from '@/components/Toast';
import { auth } from '@/lib/auth';

function decodeJwtPayload(token: string): Record<string, string> | null {
  try {
    const [, payload] = token.split('.');
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded) as Record<string, string>;
  } catch {
    return null;
  }
}

export function ProfileContent() {
  const [token, setToken] = useState<string | null>(null);
  const [role, setRole] = useState<string | null>(null);
  const [claims, setClaims] = useState<Record<string, string> | null>(null);

  useEffect(() => {
    const t = auth.getToken() ?? null;
    const r = auth.getRole() ?? null;
    setToken(t);
    setRole(r);
    if (t) setClaims(decodeJwtPayload(t));
  }, []);

  const fullNameClaim =
    claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
    claims?.name;

  const givenSurname = (
    (claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ?? '') +
    ' ' +
    (claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] ?? '')
  ).trim();

  const name = fullNameClaim || givenSurname;

  const email =
    claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ??
    claims?.email ??
    '';

  const userId =
    claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
    claims?.sub ??
    '';

  const expiresAt = claims?.exp ? new Date(Number(claims.exp) * 1000).toLocaleString() : '—';

  const roleBadgeColor: Record<string, string> = {
    Customer: 'bg-blue-50 text-blue-700 border-blue-200',
    Agent: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    Admin: 'bg-purple-50 text-purple-700 border-purple-200',
  };

  return (
    <div className="max-w-xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">Account Profile</h1>
        <p className="text-slate-500 text-xs sm:text-sm mt-1">Your session information and active credentials.</p>
      </div>

      <div className="bg-white border border-slate-200/80 rounded-3xl shadow-xs p-6 sm:p-8 space-y-6">
        {/* Avatar + name */}
        <div className="flex items-center gap-4">
          <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-slate-800 to-slate-900 text-white font-extrabold text-2xl flex items-center justify-center flex-shrink-0 shadow-sm border border-slate-700">
            {name.trim() ? name.trim()[0].toUpperCase() : role?.[0] ?? 'U'}
          </div>
          <div className="space-y-1 min-w-0">
            <h2 className="text-lg font-bold text-slate-900 truncate">{name.trim() || 'Active User'}</h2>
            {email && <p className="text-xs text-slate-500 truncate">{email}</p>}
            <div className="pt-0.5">
              <span
                className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-bold border uppercase tracking-wider ${
                  role ? (roleBadgeColor[role] ?? 'bg-slate-100 text-slate-700 border-slate-200') : ''
                }`}
              >
                {role}
              </span>
            </div>
          </div>
        </div>

        <hr className="border-slate-100" />

        {/* Details List */}
        <dl className="space-y-4 text-xs sm:text-sm">
          {userId && (
            <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-1.5 p-3.5 bg-slate-50/60 rounded-xl border border-slate-200/60">
              <dt className="font-bold text-slate-500 uppercase tracking-wider text-[11px]">User Identifier</dt>
              <dd className="font-mono text-xs text-slate-700 break-all">{userId}</dd>
            </div>
          )}
          <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-1.5 p-3.5 bg-slate-50/60 rounded-xl border border-slate-200/60">
            <dt className="font-bold text-slate-500 uppercase tracking-wider text-[11px]">Session Expires</dt>
            <dd className="text-slate-700 font-medium">{expiresAt}</dd>
          </div>
        </dl>

        {!token && (
          <div className="p-4 bg-amber-50 border border-amber-200/80 rounded-xl text-xs font-semibold text-amber-800">
            No active session detected. Please log in again.
          </div>
        )}
      </div>
    </div>
  );
}

export default function ProfilePage() {
  return (
    <RoleGuard>
      <AppShell title="Profile">
        <ProfileContent />
      </AppShell>
      <ToastContainer />
    </RoleGuard>
  );
}
