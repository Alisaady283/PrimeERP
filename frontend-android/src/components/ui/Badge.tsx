import type { ReactNode } from "react";
import { Text, View } from "react-native";

/** نظير Badge في الويب: الدرجات نفسها — الأصناف حرفيةٌ ليولّدها Tailwind */
const tones = {
  brand: { box: "bg-brand-soft", dot: "bg-brand", text: "text-brand" },
  success: { box: "bg-success-soft", dot: "bg-success", text: "text-success" },
  danger: { box: "bg-danger-soft", dot: "bg-danger", text: "text-danger" },
  warning: { box: "bg-warning-soft", dot: "bg-warning", text: "text-warning" },
  neutral: { box: "bg-paper", dot: "bg-ink-tertiary", text: "text-ink-tertiary" },
} as const;

export function Badge({ tone = "neutral", children }: { tone?: keyof typeof tones; children: ReactNode }) {
  const t = tones[tone];
  return (
    <View className={`flex-row items-center gap-1.5 self-start rounded-full px-2.5 py-0.5 ${t.box}`}>
      <View className={`size-1.5 rounded-full ${t.dot}`} />
      <Text className={`font-sans text-xs font-medium ${t.text}`}>{children}</Text>
    </View>
  );
}
