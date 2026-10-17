import { View, type ViewProps } from "react-native";

/** نظير Card في الويب */
export function Card({ className = "", ...props }: ViewProps & { className?: string }) {
  return <View className={`rounded-2xl border border-line/70 bg-card shadow-card ${className}`} {...props} />;
}
