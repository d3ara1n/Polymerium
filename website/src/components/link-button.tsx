'use client';

import Link from 'next/link';
import { Button, type buttonVariants } from '@/components/ui/button';
import type { VariantProps } from 'class-variance-authority';

type ButtonProps = VariantProps<typeof buttonVariants> & {
  className?: string;
  children: React.ReactNode;
  href: string;
  variant?: 'default' | 'outline' | 'secondary' | 'ghost' | 'destructive' | 'link';
  size?: 'default' | 'xs' | 'sm' | 'lg' | 'icon' | 'icon-xs' | 'icon-sm' | 'icon-lg';
};

export function LinkButton({ href, children, ...props }: ButtonProps) {
  return (
    <Button nativeButton={false} render={(renderProps) => <Link href={href} {...renderProps} />} {...props}>
      {children}
    </Button>
  );
}
