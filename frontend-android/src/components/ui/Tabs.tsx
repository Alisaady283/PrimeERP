import type { ReactNode } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";

export type TabItem<K extends string> = { key: K; label: string; icon?: ReactNode };

/** نظير Tabs في الويب: شريطٌ يُمرَّر أفقياً، والمختار بلون الهوية وخطٍّ تحته */
export function Tabs<K extends string>({ items, value, onChange, className = "" }: { items: TabItem<K>[]; value: K; onChange: (key: K) => void; className?: string }) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} className={`grow-0 border-b border-line ${className}`} contentContainerStyle={{ paddingHorizontal: 8, gap: 4 }}>
      {items.map((item) => {
        const active = item.key === value;
        return (
          <Pressable key={item.key} accessibilityRole="tab" accessibilityState={{ selected: active }} onPress={() => onChange(item.key)} className="flex-row items-center gap-2 px-4 py-3">
            {item.icon}
            <Text className={`font-sans text-sm ${active ? "font-semibold text-brand" : "text-ink-tertiary"}`}>{item.label}</Text>
            {active ? <View className="absolute inset-x-2 bottom-0 h-0.5 rounded-full bg-brand" /> : null}
          </Pressable>
        );
      })}
    </ScrollView>
  );
}
