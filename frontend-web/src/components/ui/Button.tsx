import type { ButtonHTMLAttributes, ReactNode } from "react";
import { LoaderCircle } from "lucide-react";
import { cn } from "@/lib/cn";

const variants = {
  primary: "bg-brand bg-linear-to-b from-white/12 to-transparent text-on-field shadow-[inset_0_1px_0_rgb(255_255_255/0.14),0_4px_12px_-6px_var(--color-brand)] hover:bg-brand-hover hover:shadow-[inset_0_1px_0_rgb(255_255_255/0.14),0_6px_16px_-6px_var(--color-brand)] active:bg-brand-active",
  secondary: "bg-card text-ink border border-line hover:border-line-strong hover:bg-paper-soft",
  ghost: "text-ink-soft hover:bg-brand-soft hover:text-ink",
  danger: "bg-danger text-white hover:brightness-95",
} as const;

const sizes = { sm: "h-8 px-2.5 text-[13px] gap-1.5", md: "h-9 px-3.5 text-sm gap-2" } as const;

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: keyof typeof variants;
  size?: keyof typeof sizes;
  icon?: ReactNode;
  loading?: boolean;
};

export function Button({ variant = "primary", size = "md", icon, loading, disabled, className, children, type = "button", ...props }: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={cn(
        "inline-flex select-none items-center justify-center rounded-lg font-medium transition-all duration-150 active:scale-[0.97]",
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand disabled:pointer-events-none disabled:opacity-50",
        variants[variant],
        sizes[size],
        className,
      )}
      {...props}
    >
      {loading ? <LoaderCircle className="size-4 animate-spin" /> : icon}
      {children}
    </button>
  );
}
