import { Search } from "lucide-react-native";
import { Pressable, Text, TextInput, View } from "react-native";
import { useCSSVariable } from "uniwind";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";

/** نظير SearchBox في الويب */
export function SearchBox({ value, onChange, className = "" }: { value: string; onChange: (value: string) => void; className?: string }) {
  const t = useText();
  const start = useStart();
  const faint = useCSSVariable("--color-ink-faint");
  return (
    <View className={`min-w-0 flex-1 justify-center ${className}`}>
      <View className="absolute start-3 z-10">
        <Icon icon={Search} size={16} tone="ink-faint" />
      </View>
      <TextInput
        value={value}
        onChangeText={onChange}
        placeholder={t("Str.SearchPlaceholder")}
        placeholderTextColor={String(faint)}
        className={`h-10 w-full rounded-lg border border-line bg-paper-soft ps-9 pe-3 font-sans text-sm text-ink focus:border-brand focus:bg-card ${start}`}
      />
    </View>
  );
}

/** نظير Segmented في الويب: اختيارٌ واحد من شرائح متجاورة */
export function Segmented<V extends string | number | boolean | null>({ items, value, onChange }: { items: Array<{ value: V; label: string }>; value: V; onChange: (value: V) => void }) {
  return (
    <View className="flex-row rounded-xl bg-paper p-1">
      {items.map((item) => (
        <Pressable key={String(item.value)} onPress={() => onChange(item.value)} className={`flex-1 items-center rounded-lg px-2.5 py-1.5 ${value === item.value ? "bg-card shadow-card" : ""}`}>
          <Text numberOfLines={1} className={`font-sans text-[13px] ${value === item.value ? "font-medium text-ink" : "text-ink-tertiary"}`}>{item.label}</Text>
        </Pressable>
      ))}
    </View>
  );
}
