'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { loginSchema, LoginFormData } from '../schemas';
import { authApi } from '../api';
import { auth } from '@/lib/auth';
import { Spinner } from '@/components/Spinner';

export function LoginForm() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema)
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      setIsLoading(true);
      setError(null);
      const res = await authApi.login(data);
      auth.setSession(res);
      router.push('/dashboard');
    } catch (err: any) {
      setError(err?.detail || 'Invalid email or password');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="w-full max-w-md p-8 bg-white border border-slate-200/80 rounded-3xl shadow-xs space-y-5">
      <div className="text-center space-y-1">
        <h2 className="text-2xl font-extrabold text-slate-900 tracking-tight">Welcome Back</h2>
        <p className="text-xs text-slate-500">Sign in to access your dashboard and tickets</p>
      </div>

      {error && (
        <div className="p-3.5 bg-red-50 border border-red-200/80 rounded-xl text-red-800 text-xs font-semibold">
          {error}
        </div>
      )}
      
      <div>
        <label htmlFor="email" className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
          Email Address
        </label>
        <input 
          id="email"
          type="email" 
          placeholder="name@company.com"
          {...register('email')} 
          className="w-full px-4 py-2.5 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent shadow-2xs text-xs sm:text-sm transition-all"
        />
        {errors.email && <p className="text-red-600 text-xs font-semibold mt-1">{errors.email.message}</p>}
      </div>

      <div>
        <label htmlFor="password" className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
          Password
        </label>
        <input 
          id="password"
          type="password" 
          placeholder="••••••••"
          {...register('password')} 
          className="w-full px-4 py-2.5 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent shadow-2xs text-xs sm:text-sm transition-all"
        />
        {errors.password && <p className="text-red-600 text-xs font-semibold mt-1">{errors.password.message}</p>}
      </div>

      <button 
        type="submit" 
        disabled={isLoading}
        className="w-full flex justify-center items-center gap-2 py-3 px-4 rounded-xl shadow-xs text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-600 disabled:opacity-60 disabled:cursor-not-allowed transition-all"
      >
        {isLoading && <Spinner size="sm" className="border-white" />}
        {isLoading ? 'Signing In…' : 'Sign In'}
      </button>
    </form>
  );
}
