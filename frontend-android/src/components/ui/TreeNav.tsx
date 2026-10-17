import { ChevronLeft, ChevronRight } from "lucide-react-native";
import type { ReactNode } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { formatCount } from "@/lib/format";
import { useDirection, useStart } from "~/lib/direction";
import { Card } from "~/components/ui/Card";
import { Icon } from "~/components/ui/Icon";

/** نظائر TreeSplit في الويب على الهاتف: الشجرة تُتصفَّح خطوةً خطوة — الطريق في الأعلى، والفروع تحتها، ولمس الفرع يدخله */

/** طريق العنصر: يُمرَّر أفقياً، والجذر يعيد إلى الأعلى */
export function TreePath({ items, onPick }: { items: Array<{ id: string; label: string }>; onPick: (id: string | null) => void }) {
  const direction = useDirection();
  const Sep = direction === "rtl" ? ChevronLeft : ChevronRight;
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} className="grow-0" contentContainerStyle={{ alignItems: "center", gap: 2 }}>
      {items.map((item, i) => (
        <View key={item.id} className="flex-row items-center gap-0.5">
          {i > 0 ? <Icon icon={Sep} size={12} tone="ink-faint" /> : null}
          <Pressable onPress={() => onPick(i === 0 ? null : item.id)} className="rounded px-1.5 py-1 active:bg-paper">
            <Text className={`font-sans text-xs ${i === items.length - 1 ? "font-medium text-ink" : "text-ink-tertiary"}`}>{item.label}</Text>
          </Pressable>
        </View>
      ))}
    </ScrollView>
  );
}

/** شرائح التصفية */
export function TreeChips<V extends string | number | null>({ items, value, onChange }: { items: Array<{ value: V; label: string }>; value: V; onChange: (value: V) => void }) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} className="grow-0" contentContainerStyle={{ gap: 6 }}>
      {items.map((item) => (
        <Pressable key={String(item.value)} onPress={() => onChange(item.value)} className={`h-8 justify-center rounded-full px-3.5 ${value === item.value ? "bg-brand" : "bg-paper active:bg-brand-soft"}`}>
          <Text className={`font-sans text-xs ${value === item.value ? "font-medium text-on-field" : "text-ink-soft"}`}>{item.label}</Text>
        </Pressable>
      ))}
    </ScrollView>
  );
}

/** خانة ملخّص */
export function DetailTile({ label, value, tone }: { label: string; value: ReactNode; tone?: "brand" }) {
  return (
    <View className={`min-w-28 flex-1 rounded-xl border border-line px-3.5 py-2.5 ${tone === "brand" ? "bg-brand-soft" : "bg-paper-soft"}`}>
      <Text className="font-sans text-xs text-ink-tertiary">{label}</Text>
      <Text numberOfLines={1} adjustsFontSizeToFit className={`mt-0.5 font-sans text-base font-semibold ${tone === "brand" ? "text-brand" : "text-ink"}`}>{value}</Text>
    </View>
  );
}

/** قائمة الفروع: بطاقة صفوف، والسهم لما له فروع */
export function ChildRows<T>({ title, rows, keyOf, label, hint, end, hasChildren, onOpen }: {
  title: string;
  rows: T[];
  keyOf: (row: T) => string;
  label: (row: T) => string;
  hint?: (row: T) => ReactNode;
  end?: (row: T) => ReactNode;
  hasChildren?: (row: T) => boolean;
  onOpen: (row: T) => void;
}) {
  const start = useStart();
  const direction = useDirection();
  const Arrow = direction === "rtl" ? ChevronLeft : ChevronRight;
  return (
    <Card className="overflow-hidden">
      <Text className={`border-b border-line bg-paper-soft px-3 py-2 font-sans text-xs font-medium text-ink-soft ${start}`}>
        {title} · {formatCount(rows.length)}
      </Text>
      {rows.map((row, i) => (
        <Pressable key={keyOf(row)} onPress={() => onOpen(row)} className={`flex-row items-center gap-3 px-3 py-3 active:bg-paper ${i > 0 ? "border-t border-line" : ""}`}>
          <View className="min-w-0 flex-1">
            <Text numberOfLines={1} className={`font-sans text-sm font-medium text-ink ${start}`}>{label(row)}</Text>
            {hint ? <Text numberOfLines={1} className={`font-sans text-xs text-ink-tertiary ${start}`}>{hint(row)}</Text> : null}
          </View>
          {end ? <View>{end(row)}</View> : null}
          {hasChildren?.(row) ? <Icon icon={Arrow} size={16} tone="ink-faint" /> : null}
        </Pressable>
      ))}
    </Card>
  );
}
