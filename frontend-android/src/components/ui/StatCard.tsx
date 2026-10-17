import { ArrowDownRight, ArrowUpRight } from "lucide-react-native";
import type { ReactNode } from "react";
import { Pressable, Text, View } from "react-native";
import { alpha, toHex } from "@/lib/color";
import { useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";

type StatCardProps = {
  label: string;
  value: ReactNode;
  icon?: ReactNode;
  change?: number | null;
  lowerIsBetter?: boolean;
  onPress?: () => void;
  /** لون البطاقة: قيمة رمزٍ مقروءة من tokens.css */
  accent?: string;
};

/** نظير StatCard في الويب: شريط اللون وخلفيته الفاتحة وتغيّر النسبة */
export function StatCard({ label, value, icon, change, lowerIsBetter, onPress, accent }: StatCardProps) {
  const start = useStart();
  const good = change == null ? null : lowerIsBetter ? change <= 0 : change >= 0;
  return (
    <Pressable
      disabled={!onPress}
      onPress={onPress}
      className="flex-row items-start gap-3 overflow-hidden rounded-2xl border border-line/70 bg-card p-3 shadow-card active:scale-[0.98]"
    >
      {accent ? <View className="absolute inset-y-0 start-0 w-1" style={{ backgroundColor: toHex(accent) }} /> : null}
      {accent ? <View className="absolute inset-0" style={{ backgroundColor: alpha(accent, 0.06) }} /> : null}
      {icon ? (
        <View className="size-10 items-center justify-center rounded-xl" style={accent ? { backgroundColor: alpha(accent, 0.15) } : undefined}>
          {icon}
        </View>
      ) : null}
      <View className="min-w-0 flex-1">
        <Text numberOfLines={1} className={`font-sans text-xs font-medium text-ink-tertiary ${start}`}>{label}</Text>
        <View className="mt-1">{typeof value === "string" ? <Text numberOfLines={1} className={`font-sans text-xl font-bold text-ink ${start}`}>{value}</Text> : value}</View>
        {change != null ? (
          <View className={`mt-1 flex-row items-center gap-0.5 self-start rounded-full px-1.5 ${good ? "bg-success-soft" : "bg-danger-soft"}`}>
            <Icon icon={change >= 0 ? ArrowUpRight : ArrowDownRight} size={13} tone={good ? "success" : "danger"} />
            <Text className={`font-sans text-xs font-medium ${good ? "text-success" : "text-danger"}`}>{Math.abs(change).toFixed(1)}%</Text>
          </View>
        ) : null}
      </View>
    </Pressable>
  );
}
