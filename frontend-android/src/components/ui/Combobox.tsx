import { Check, ChevronDown, X } from "lucide-react-native";
import { useMemo, useState } from "react";
import { FlatList, Modal, Pressable, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useText } from "@/lib/i18n";
import { useDirection, useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";
import { SearchBox } from "~/components/ui/SearchBox";

export type Option = { value: string | number; label: string; hint?: string };

type ComboboxProps = {
  label?: string;
  value: string | number | null;
  options: Option[];
  onChange: (value: string | number | null) => void;
  placeholder?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  clearable?: boolean;
  dense?: boolean;
  className?: string;
};

/** نظير Combobox في الويب — على الهاتف ورقةٌ كاملة ببحث */
export function Combobox({ label, value, options, onChange, placeholder, error, required, disabled, clearable = true, dense, className = "" }: ComboboxProps) {
  const t = useText();
  const start = useStart();
  const direction = useDirection();
  const insets = useSafeAreaInsets();
  const [open, setOpen] = useState(false);
  const [term, setTerm] = useState("");
  const selected = options.find((o) => o.value === value) ?? null;
  const shown = useMemo(() => {
    const q = term.trim().toLowerCase();
    return q ? options.filter((o) => o.label.toLowerCase().includes(q) || o.hint?.toLowerCase().includes(q)) : options;
  }, [options, term]);

  const pick = (next: string | number | null) => {
    onChange(next);
    setOpen(false);
    setTerm("");
  };

  return (
    <View className={`gap-1.5 ${className}`}>
      {label ? (
        <Text className={`font-sans text-xs font-medium text-ink-soft ${start}`}>
          {label}
          {required ? <Text className="text-danger"> *</Text> : null}
        </Text>
      ) : null}
      <Pressable
        disabled={disabled}
        onPress={() => setOpen(true)}
        className={`flex-row items-center gap-2 border bg-card px-3.5 ${dense ? "h-9 rounded-lg" : "h-11 rounded-xl"} ${error ? "border-danger" : "border-line"} ${disabled ? "bg-paper-soft opacity-70" : ""}`}
      >
        <Text numberOfLines={1} className={`flex-1 font-sans text-sm ${selected ? "text-ink" : "text-ink-faint"} ${start}`}>{selected?.label ?? placeholder ?? "—"}</Text>
        {clearable && selected && !disabled ? (
          <Pressable hitSlop={8} onPress={() => onChange(null)}>
            <Icon icon={X} size={14} tone="ink-faint" />
          </Pressable>
        ) : null}
        <Icon icon={ChevronDown} size={16} tone="ink-tertiary" />
      </Pressable>
      {error ? <Text className={`font-sans text-xs text-danger ${start}`}>{error}</Text> : null}

      <Modal visible={open} animationType="slide" onRequestClose={() => setOpen(false)}>
        <View className="flex-1 gap-3 bg-paper px-4" style={{ paddingTop: insets.top + 12, direction }}>
          <View className="flex-row items-center gap-3">
            <Text className={`flex-1 font-sans text-lg font-bold text-ink ${start}`}>{label ?? placeholder ?? ""}</Text>
            <Pressable hitSlop={12} onPress={() => setOpen(false)}>
              <Text className="font-sans text-sm font-semibold text-brand">{t("Str.Cancel")}</Text>
            </Pressable>
          </View>
          <View className="flex-row">
            <SearchBox value={term} onChange={setTerm} />
          </View>
          <FlatList
            data={shown}
            keyExtractor={(o) => String(o.value)}
            keyboardShouldPersistTaps="handled"
            contentContainerStyle={{ paddingBottom: insets.bottom + 24, gap: 2 }}
            renderItem={({ item }) => {
              const active = item.value === value;
              return (
                <Pressable onPress={() => pick(item.value)} className={`flex-row items-center gap-3 rounded-xl px-3 py-3 ${active ? "bg-brand-soft" : "active:bg-paper-soft"}`}>
                  <Text className={`flex-1 font-sans text-sm text-ink ${start}`}>{item.label}</Text>
                  {item.hint ? <Text className="font-sans text-xs text-ink-faint">{item.hint}</Text> : null}
                  {active ? <Icon icon={Check} size={16} tone="brand" /> : null}
                </Pressable>
              );
            }}
          />
        </View>
      </Modal>
    </View>
  );
}
