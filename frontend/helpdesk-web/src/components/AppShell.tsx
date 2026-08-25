'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { auth } from '@/lib/auth';

interface NavItem {
  href: string;
  label: string;
  icon: (active: boolean) => React.ReactNode;
  roles: string[];
}

const DashboardIcon = (active: boolean) => (
  <svg className={`w-5 h-5 transition-colors ${active ? 'text-blue-600' : 'text-slate-400 group-hover:text-slate-600'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={active ? 2 : 1.75}
      d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
  </svg>
);

const TicketIcon = (active: boolean) => (
  <svg className={`w-5 h-5 transition-colors ${active ? 'text-blue-600' : 'text-slate-400 group-hover:text-slate-600'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={active ? 2 : 1.75}
      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
  </svg>
);

const CategoryIcon = (active: boolean) => (
  <svg className={`w-5 h-5 transition-colors ${active ? 'text-blue-600' : 'text-slate-400 group-hover:text-slate-600'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={active ? 2 : 1.75}
      d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
  </svg>
);

const AgentsIcon = (active: boolean) => (
  <svg className={`w-5 h-5 transition-colors ${active ? 'text-blue-600' : 'text-slate-400 group-hover:text-slate-600'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={active ? 2 : 1.75}
      d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
  </svg>
);

const UserIcon = (active: boolean) => (
  <svg className={`w-5 h-5 transition-colors ${active ? 'text-blue-600' : 'text-slate-400 group-hover:text-slate-600'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={active ? 2 : 1.75}
      d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
  </svg>
);

const navItems: NavItem[] = [
  { href: '/dashboard', label: 'Dashboard', icon: DashboardIcon, roles: ['Customer', 'Agent', 'Admin'] },
  { href: '/tickets', label: 'My Tickets', icon: TicketIcon, roles: ['Customer'] },
  { href: '/tickets', label: 'Ticket Queue', icon: TicketIcon, roles: ['Agent', 'Admin'] },
  { href: '/admin/categories', label: 'Categories', icon: CategoryIcon, roles: ['Admin'] },
  { href: '/admin/agents', label: 'Agents', icon: AgentsIcon, roles: ['Admin'] },
  { href: '/profile', label: 'Profile', icon: UserIcon, roles: ['Customer', 'Agent', 'Admin'] },
];

function decodeJwtPayload(token: string): Record<string, string> | null {
  try {
    const [, payload] = token.split('.');
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded) as Record<string, string>;
  } catch {
    return null;
  }
}

interface AppShellProps {
  children: React.ReactNode;
  title?: string;
}

export function AppShell({ children, title }: AppShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [role, setRole] = useState<string>('Customer');
  const [userName, setUserName] = useState<string>('User');

  useEffect(() => {
    const currentRole = auth.getRole() ?? 'Customer';
    setRole(currentRole);

    const token = auth.getToken();
    if (token) {
      const claims = decodeJwtPayload(token);
      const name =
        claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        claims?.name ??
        (claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ?? '') + ' ' + (claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] ?? '');
      if (name.trim()) setUserName(name.trim());
    }
  }, []);

  const visibleItems = navItems.filter((item) => item.roles.includes(role));

  const handleLogout = () => {
    auth.clearSession();
    router.push('/login');
  };

  const isActive = (href: string) => pathname === href || (href !== '/dashboard' && pathname.startsWith(href + '/'));

  const roleColors: Record<string, { bg: string; text: string; border: string }> = {
    Customer: { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-200' },
    Agent: { bg: 'bg-emerald-50', text: 'text-emerald-700', border: 'border-emerald-200' },
    Admin: { bg: 'bg-purple-50', text: 'text-purple-700', border: 'border-purple-200' },
  };

  const roleBadge = roleColors[role] ?? roleColors.Customer;

  const NavLinks = () => (
    <nav className="flex-1 px-3 py-4 space-y-1.5" aria-label="Main navigation">
      {visibleItems.map((item) => {
        const active = isActive(item.href);
        return (
          <Link
            key={item.label}
            href={item.href}
            onClick={() => setSidebarOpen(false)}
            className={`group flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
              active
                ? 'bg-blue-50/80 text-blue-700 font-semibold shadow-xs'
                : 'text-slate-600 hover:bg-slate-100/80 hover:text-slate-900'
            }`}
            aria-current={active ? 'page' : undefined}
          >
            {item.icon(active)}
            <span className="flex-1">{item.label}</span>
            {active && (
              <span className="w-1.5 h-4 rounded-full bg-blue-600" aria-hidden="true" />
            )}
          </Link>
        );
      })}
    </nav>
  );

  return (
    <div className="flex min-h-screen bg-slate-50 text-slate-900">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex flex-col w-64 bg-white border-r border-slate-200/80 shadow-xs">
        {/* Brand Header */}
        <div className="flex items-center gap-3 px-5 py-4 border-b border-slate-100">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center shadow-xs flex-shrink-0 text-white font-black text-base tracking-wider">
            H
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <span className="font-bold text-slate-900 text-base tracking-tight leading-none">HelpDisk</span>
              <span className="px-1.5 py-0.2 rounded text-[10px] font-bold bg-slate-100 text-slate-600">v1.0</span>
            </div>
            <p className="text-[11px] text-slate-400 font-medium leading-none mt-1">Support Platform</p>
          </div>
        </div>

        <NavLinks />

        {/* User Card & Sign Out */}
        <div className="p-3 border-t border-slate-100 space-y-2">
          <Link
            href="/profile"
            className="flex items-center gap-3 p-2 rounded-xl hover:bg-slate-50 transition-colors group"
          >
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-slate-700 to-slate-900 text-white font-semibold text-xs flex items-center justify-center shadow-xs flex-shrink-0">
              {userName ? userName[0].toUpperCase() : 'U'}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-slate-900 truncate group-hover:text-blue-600 transition-colors">
                {userName}
              </p>
              <span
                className={`inline-flex items-center px-2 py-0.2 rounded-full text-[10px] font-bold border ${roleBadge.bg} ${roleBadge.text} ${roleBadge.border}`}
              >
                {role}
              </span>
            </div>
          </Link>

          <button
            onClick={handleLogout}
            className="w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-semibold text-slate-500 hover:text-red-600 hover:bg-red-50/60 transition-colors"
          >
            <svg className="w-4 h-4 text-slate-400 group-hover:text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
            Sign Out
          </button>
        </div>
      </aside>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="lg:hidden fixed inset-0 z-50 flex">
          <div
            className="fixed inset-0 bg-slate-900/40 backdrop-blur-xs transition-opacity"
            onClick={() => setSidebarOpen(false)}
            aria-hidden="true"
          />
          <aside className="relative flex flex-col w-72 bg-white shadow-2xl z-50 animate-in slide-in-from-left duration-200">
            <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
                  H
                </div>
                <span className="font-bold text-slate-900 text-base">HelpDisk</span>
              </div>
              <button
                onClick={() => setSidebarOpen(false)}
                className="w-8 h-8 rounded-lg flex items-center justify-center text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
                aria-label="Close navigation"
              >
                ✕
              </button>
            </div>

            <NavLinks />

            <div className="p-3 border-t border-slate-100 space-y-2">
              <Link
                href="/profile"
                onClick={() => setSidebarOpen(false)}
                className="flex items-center gap-3 p-2 rounded-xl hover:bg-slate-50 transition-colors"
              >
                <div className="w-9 h-9 rounded-full bg-slate-800 text-white font-semibold text-xs flex items-center justify-center flex-shrink-0">
                  {userName ? userName[0].toUpperCase() : 'U'}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-slate-900 truncate">{userName}</p>
                  <span className={`inline-flex items-center px-2 py-0.2 rounded-full text-[10px] font-bold border ${roleBadge.bg} ${roleBadge.text} ${roleBadge.border}`}>
                    {role}
                  </span>
                </div>
              </Link>
              <button
                onClick={handleLogout}
                className="w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-semibold text-red-600 hover:bg-red-50 transition-colors"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
                    d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
                Sign Out
              </button>
            </div>
          </aside>
        </div>
      )}

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top bar (mobile) */}
        <header className="lg:hidden sticky top-0 z-30 flex items-center justify-between gap-3 px-4 py-3 bg-white/95 backdrop-blur-xs border-b border-slate-200/80 shadow-2xs">
          <div className="flex items-center gap-2.5">
            <button
              onClick={() => setSidebarOpen(true)}
              className="p-2 rounded-xl text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-blue-600 transition-colors"
              aria-label="Open navigation menu"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>
            <span className="font-bold text-slate-900 text-sm">{title ?? 'HelpDisk'}</span>
          </div>

          <Link href="/profile" className="w-8 h-8 rounded-full bg-slate-800 text-white font-semibold text-xs flex items-center justify-center shadow-xs">
            {userName ? userName[0].toUpperCase() : 'U'}
          </Link>
        </header>

        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
          {children}
        </main>
      </div>
    </div>
  );
}
