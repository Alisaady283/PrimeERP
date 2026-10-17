import type { ReactNode } from "react";
import { ActivityIndicator, Pressable, Text, View } from "react-native";
import { useCSSVariable } from "uniwind";

/** نظير Button في الويب: المتغيّرات والمقاسات نفسها */
const variants = {
  primary: { box: "bg-brand active:bg-brand-active", text: "text-on-field" },
  secondary: { box: "bg-card border border-line active:bg-paper-soft", text: "text-ink" },
  ghost: { box: "active:bg-brand-soft", text: "text-ink-soft" },
  danger: { box: "bg-danger active:opacity-90", text: "text-white" },
  success: { box: "bg-success", text: "text-white" },
} as const;

const sizes = { sm: { box: "h-9 px-3 gap-1.5", text: "text-[13px]" }, md: { box: "h-11 px-4 gap-2", text: "text-sm" }, lg: { box: "h-12 px-5 gap-2", text: "text-base" } } as const;

type ButtonProps = {
  children?: ReactNode;
  onPress?: () => void;
  variant?: keyof typeof variants;
  size?: keyof typeof sizes;
  icon?: ReactNode;
  loading?: boolean;
  disabled?: boolean;
  className?: string;
  textClassName?: string;
};

export function Button({ children, onPress, variant = "primary", size = "md", icon, loading, disabled, className = "", textClassName = "" }: ButtonProps) {
  const [onField, ink] = useCSSVariable(["--color-on-field", "--color-ink"]);
  const v = variants[variant];
  const s = sizes[size];
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled || loading}
      className={`flex-row items-center justify-center rounded-lg active:scale-[0.97] ${v.box} ${s.box} ${disabled || loading ? "opacity-50" : ""} ${className}`}
    >
      {loading ? <ActivityIndicator size="small" color={String(variant === "primary" || variant === "danger" || variant === "success" ? onField : ink)} /> : icon ? <View>{icon}</View> : null}
      {typeof children === "string" ? <Text className={`font-sans font-medium ${v.text} ${s.text} ${textClassName}`}>{children}</Text> : children}
    </Pressable>
  );
}
