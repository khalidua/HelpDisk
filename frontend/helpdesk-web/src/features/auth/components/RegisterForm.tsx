'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { registerSchema, RegisterFormData } from '../schemas';
import { authApi } from '../api';
import { companiesApi } from '@/features/companies/api';
import { Company } from '@/types/api';
import { Spinner } from '@/components/Spinner';

const inputClass =
  'w-full px-4 py-2.5 bg-slate-50/50 hover:bg-white focus:bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent shadow-2xs text-xs sm:text-sm transition-all';
const labelClass = 'block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5';
const errorClass = 'text-red-600 text-xs font-semibold mt-1';

export function RegisterForm() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const [companies, setCompanies] = useState<Company[]>([]);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [companiesError, setCompaniesError] = useState<string | null>(null);

  const { register, handleSubmit, formState: { errors } } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema)
  });

  useEffect(() => {
    companiesApi.getCompanies()
      .then((data) => {
        setCompanies(data);
      })
      .catch((err) => {
        setCompaniesError(err?.detail || 'Failed to load companies');
      })
      .finally(() => {
        setLoadingCompanies(false);
      });
  }, []);

  const onSubmit = async (data: RegisterFormData) => {
    try {
      setIsLoading(true);
      setError(null);
      await authApi.register(data);
      setSuccessMsg('Registration successful! Redirecting to login…');
      setTimeout(() => router.push('/login'), 1500);
    } catch (err: any) {
      setError(err?.detail || 'Registration failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="w-full max-w-md p-8 bg-white border border-slate-200/80 rounded-3xl shadow-xs space-y-4">
      <div className="text-center space-y-1">
        <h2 className="text-2xl font-extrabold text-slate-900 tracking-tight">Create an Account</h2>
        <p className="text-xs text-slate-500">Register as a customer to submit and track support tickets</p>
      </div>

      {error && (
        <div role="alert" className="p-3.5 bg-red-50 border border-red-200/80 rounded-xl text-red-800 text-xs font-semibold">
          {error}
        </div>
      )}
      {successMsg && (
        <div role="alert" className="p-3.5 bg-emerald-50 border border-emerald-200/80 rounded-xl text-emerald-800 text-xs font-semibold">
          {successMsg}
        </div>
      )}
      
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label htmlFor="firstName" className={labelClass}>First Name</label>
          <input 
            id="firstName"
            placeholder="Jane"
            {...register('firstName')} 
            className={inputClass} 
          />
          {errors.firstName && <p className={errorClass} role="alert">{errors.firstName.message}</p>}
        </div>

        <div>
          <label htmlFor="lastName" className={labelClass}>Last Name</label>
          <input 
            id="lastName"
            placeholder="Doe"
            {...register('lastName')} 
            className={inputClass} 
          />
          {errors.lastName && <p className={errorClass} role="alert">{errors.lastName.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="email" className={labelClass}>Email Address</label>
        <input 
          id="email"
          type="email" 
          placeholder="jane.doe@company.com"
          {...register('email')} 
          className={inputClass} 
        />
        {errors.email && <p className={errorClass} role="alert">{errors.email.message}</p>}
      </div>

      <div>
        <label htmlFor="password" className={labelClass}>Password</label>
        <input 
          id="password"
          type="password" 
          placeholder="••••••••"
          {...register('password')} 
          className={inputClass} 
        />
        {errors.password && <p className={errorClass} role="alert">{errors.password.message}</p>}
      </div>

      <div>
        <label htmlFor="companyId" className={labelClass}>Company</label>
        {loadingCompanies ? (
          <div className="flex items-center gap-2 py-2 text-xs font-semibold text-slate-500">
            <Spinner size="sm" /> Loading companies…
          </div>
        ) : companiesError ? (
          <p className="text-xs text-red-600 font-semibold">{companiesError}</p>
        ) : (
          <select 
            id="companyId"
            {...register('companyId')} 
            className={inputClass}
          >
            <option value="" className="text-slate-500">Select your organization…</option>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.name}
              </option>
            ))}
          </select>
        )}
        {errors.companyId && <p className={errorClass} role="alert">{errors.companyId.message}</p>}
      </div>

      <button 
        type="submit" 
        disabled={isLoading || loadingCompanies || !!successMsg}
        className="w-full flex justify-center items-center gap-2 py-3 px-4 rounded-xl shadow-xs text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-600 disabled:opacity-60 disabled:cursor-not-allowed transition-all mt-2"
      >
        {isLoading && <Spinner size="sm" className="border-white" />}
        {isLoading ? 'Creating Account…' : 'Create Account'}
      </button>
    </form>
  );
}
