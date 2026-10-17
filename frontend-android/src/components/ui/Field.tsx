import { DateTimePickerAndroid } from "@react-native-community/datetimepicker";
import { CalendarDays, Clock } from "lucide-react-native";
import type { ReactNode, Ref } from "react";
import { Pressable, Text, TextInput, View, type TextInputProps } from "react-native";
import { useCSSVariable } from "uniwind";
import { useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";

/** نظير Field في الويب: TextField · TextArea · Switch */
const control = "w-full rounded-xl border border-line bg-card px-3.5 font-sans text-sm text-ink focus:border-brand";

type FrameProps = { label?: string; error?: string; required?: boolean; className?: string; children: ReactNode };

function Frame({ label, error, required, className = "", children }: FrameProps) {
  const start = useStart();
  return (
    <View className={`gap-1.5 ${className}`}>
      {label ? (
        <Text className={`font-sans text-xs font-medium text-ink-soft ${start}`}>
          {label}
          {required ? <Text className="text-danger"> *</Text> : null}
        </Text>
      ) : null}
      {children}
      {error ? <Text className={`font-sans text-xs text-danger ${start}`}>{error}</Text> : null}
    </View>
  );
}

type Shared = { label?: string; error?: string; required?: boolean; frameClassName?: string; icon?: ReactNode; end?: ReactNode; className?: string; ref?: Ref<TextInput> };

export function TextField({ label, error, required, frameClassName, icon, end, className = "", ref, ...props }: Shared & TextInputProps) {
  const start = useStart();
  const faint = useCSSVariable("--color-ink-faint");
  return (
    <Frame label={label} error={error} required={required} className={frameClassName}>
      <View className="justify-center">
        {icon ? <View className="absolute start-3.5 z-10">{icon}</View> : null}
        <TextInput
          ref={ref}
          placeholderTextColor={String(faint)}
          className={`${control} h-11 ${start} ${icon ? "ps-10" : ""} ${end ? "pe-10" : ""} ${error ? "border-danger" : ""} ${props.editable === false ? "bg-paper-soft text-ink-tertiary" : ""} ${className}`}
          {...props}
        />
        {end ? <View className="absolute end-2">{end}</View> : null}
      </View>
    </Frame>
  );
}

export function TextArea({ label, error, required, frameClassName, className = "", ref, ...props }: Shared & TextInputProps) {
  const start = useStart();
  return (
    <Frame label={label} error={error} required={required} className={frameClassName}>
      <TextInput ref={ref} multiline textAlignVertical="top" className={`${control} min-h-24 py-2.5 ${start} ${error ? "border-danger" : ""} ${className}`} {...props} />
    </Frame>
  );
}

/** المفتاح: كمفتاح الويب شكلاً */
export function Switch({ label, checked, onChange, disabled }: { label: string; checked: boolean; onChange: (checked: boolean) => void; disabled?: boolean }) {
  const start = useStart();
  return (
    <Pressable disabled={disabled} onPress={() => onChange(!checked)} className={`flex-row items-center gap-3 py-1 ${disabled ? "opacity-50" : ""}`}>
      <View className={`h-6 w-11 justify-center rounded-full px-0.5 ${checked ? "items-end bg-success" : "items-start bg-line-strong"}`}>
        <View className="size-5 rounded-full bg-white shadow-card" />
      </View>
      <Text className={`flex-1 font-sans text-sm text-ink ${start}`}>{label}</Text>
    </Pressable>
  );
}

const pad = (n: number) => String(n).padStart(2, "0");
const asDate = (value: string, mode: "date" | "time") => {
  if (!value) return new Date();
  if (mode === "time") {
    const [h = 0, m = 0] = value.split(":").map(Number);
    const d = new Date();
    d.setHours(h, m, 0, 0);
    return d;
  }
  const [y, mo, d] = value.slice(0, 10).split("-").map(Number);
  return y ? new Date(y, (mo ?? 1) - 1, d ?? 1) : new Date();
};

/** التاريخ والوقت: شكل الحقل، واللمس يفتح منتقي أندرويد الأصلي — القيمة YYYY-MM-DD أو HH:MM كالويب */
export function DateField({ label, value, onChange, mode = "date", error, required, disabled, dense, placeholder }: {
  label?: string;
  value: string;
  onChange: (value: string | null) => void;
  mode?: "date" | "time";
  error?: string;
  required?: boolean;
  disabled?: boolean;
  dense?: boolean;
  placeholder?: string;
}) {
  const start = useStart();
  const pick = () =>
    DateTimePickerAndroid.open({
      value: asDate(value, mode),
      mode,
      is24Hour: true,
      onChange: (event, date) => {
        if (event.type !== "set" || !date) return;
        onChange(mode === "time" ? `${pad(date.getHours())}:${pad(date.getMinutes())}` : `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`);
      },
    });
  return (
    <Frame label={label} error={error} required={required}>
      <Pressable
        disabled={disabled}
        onPress={pick}
        className={`flex-row items-center gap-2 border bg-card px-3.5 ${dense ? "h-9 rounded-lg" : "h-11 rounded-xl"} ${error ? "border-danger" : "border-line"} ${disabled ? "bg-paper-soft opacity-70" : ""}`}
      >
        <Icon icon={mode === "time" ? Clock : CalendarDays} size={16} tone="ink-tertiary" />
        <Text numberOfLines={1} className={`flex-1 font-sans text-sm ${value ? "text-ink" : "text-ink-faint"} ${start}`} style={{ fontVariant: ["tabular-nums"] }}>
          {value || placeholder || (mode === "time" ? "HH:MM" : "YYYY-MM-DD")}
        </Text>
      </Pressable>
    </Frame>
  );
}
