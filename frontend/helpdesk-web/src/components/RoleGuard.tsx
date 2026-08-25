'use client';
import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { auth } from '@/lib/auth';

interface RoleGuardProps {
  children: React.ReactNode;
  allowedRoles?: string[];
}

export function RoleGuard({ children, allowedRoles }: RoleGuardProps) {
  const router = useRouter();
  const pathname = usePathname();
  const [isAuthorized, setIsAuthorized] = useState(false);

  useEffect(() => {
    const token = auth.getToken();
    const role = auth.getRole();

    if (!token) {
      router.push('/login');
      return;
    }

    if (allowedRoles && role && !allowedRoles.includes(role)) {
      router.push('/dashboard');
      return;
    }

    setIsAuthorized(true);
  }, [router, pathname, allowedRoles]);

  if (!isAuthorized) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-8">
        <div className="text-center space-y-3">
          <div className="w-8 h-8 border-3 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-sm font-medium text-slate-700">Verifying session...</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
