import type { ReactNode } from 'react';

type Props = { children: ReactNode; className?: string };

export default function PageContainer({ children, className = '' }: Props) {
  return (
    <main
      className={
        'min-h-screen w-full px-4 sm:px-6 lg:px-8 mx-auto max-w-7xl ' +
        'flex flex-col items-center justify-center gap-4 bg-slate-50 ' +
        className
      }
    >
      {children}
    </main>
  );
}
