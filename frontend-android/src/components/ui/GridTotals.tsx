import { Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";

/** نظير GridTotals في الويب: شريط المجاميع أسفل القائمة — عنوانٌ صغير وقيمةٌ واضحة في شريحة */
export function GridTotals({ items }: { items: Array<[string, string]> }) {
  const insets = useSafeAreaInsets();
  if (items.length === 0) return null;
  return (
    <View className="flex-row flex-wrap justify-end gap-2 border-t border-line bg-paper-soft px-3 py-2" style={{ paddingBottom: insets.bottom + 8 }}>
      {items.map(([label, value]) => (
        <View key={label} className="flex-row items-baseline gap-2 rounded-lg border border-line bg-card px-3 py-1">
          <Text className="font-sans text-xs text-ink-tertiary">{label}</Text>
          <Text className="font-sans text-sm font-semibold text-ink" style={{ fontVariant: ["tabular-nums"] }}>{value}</Text>
        </View>
      ))}
    </View>
  );
}
