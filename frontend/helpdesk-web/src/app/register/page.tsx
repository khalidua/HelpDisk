import { RegisterForm } from '@/features/auth/components/RegisterForm';
import Link from 'next/link';

export default function RegisterPage() {
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-center items-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-6">
        <div className="flex items-center justify-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center shadow-xs text-white font-extrabold text-lg">
            H
          </div>
          <span className="text-2xl font-extrabold text-slate-900 tracking-tight">HelpDisk</span>
        </div>

        <RegisterForm />

        <p className="text-center text-xs font-medium text-slate-500">
          Already have an account?{' '}
          <Link href="/login" className="font-bold text-blue-600 hover:text-blue-700 hover:underline">
            Login here
          </Link>
        </p>
      </div>
    </div>
  );
}
