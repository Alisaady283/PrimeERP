import { ChevronDown } from "lucide-react-native";
import { useState, type ReactNode } from "react";
import { Modal, Pressable, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useDirection, useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";

export type MenuItem = { label: string; icon?: ReactNode; danger?: boolean; onSelect: () => void };

/** نظير Menu في الويب: الزرّ نفسه، وأفعاله في ورقةٍ من أسفل الشاشة — في متناول الإبهام */
export function Menu({ label, items, icon, disabled }: { label: string; items: MenuItem[]; icon?: ReactNode; disabled?: boolean }) {
  const start = useStart();
  const direction = useDirection();
  const insets = useSafeAreaInsets();
  const [open, setOpen] = useState(false);

  return (
    <>
      <Pressable disabled={disabled} onPress={() => setOpen(true)} className={`h-9 flex-row items-center gap-1.5 rounded-lg border border-line bg-card px-3 active:bg-paper-soft ${disabled ? "opacity-50" : ""}`}>
        {icon}
        <Text numberOfLines={1} className="max-w-40 font-sans text-[13px] font-medium text-ink">{label}</Text>
        <Icon icon={ChevronDown} size={16} tone="ink-tertiary" />
      </Pressable>
      <Modal visible={open} transparent animationType="fade" onRequestClose={() => setOpen(false)}>
        <Pressable className="flex-1 justify-end bg-black/40" onPress={() => setOpen(false)} style={{ direction }}>
          <View className="rounded-t-2xl border-t border-line bg-card pt-2" style={{ paddingBottom: insets.bottom + 8 }}>
            <View className="mb-1 h-1 w-10 self-center rounded-full bg-line-strong" />
            <Text className={`px-5 py-2 font-sans text-xs font-medium text-ink-tertiary ${start}`}>{label}</Text>
            {items.map((item) => (
              <Pressable
                key={item.label}
                onPress={() => {
                  setOpen(false);
                  item.onSelect();
                }}
                className={`flex-row items-center gap-3 px-5 py-3.5 ${item.danger ? "active:bg-danger-soft" : "active:bg-paper"}`}
              >
                {item.icon}
                <Text className={`flex-1 font-sans text-[15px] ${item.danger ? "text-danger" : "text-ink"} ${start}`}>{item.label}</Text>
              </Pressable>
            ))}
          </View>
        </Pressable>
      </Modal>
    </>
  );
}
