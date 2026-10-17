import { CheckCircle2, Info, XCircle } from "lucide-react-native";
import { useEffect } from "react";
import { Pressable, Text } from "react-native";
import Animated, { FadeInDown, FadeOutDown } from "react-native-reanimated";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { create } from "zustand";
import { duration } from "@/lib/motion";
import { useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";

type Tone = "success" | "error" | "info";
type Action = { label: string; onPress: () => void };
type ToastState = { message: string | null; tone: Tone; action?: Action; push: (tone: Tone, message: string, action?: Action) => void; dismiss: () => void };

const useToasts = create<ToastState>((set) => ({ message: null, tone: "success", push: (tone, message, action) => set({ tone, message, action }), dismiss: () => set({ message: null, action: undefined }) }));

const glyphs = { success: CheckCircle2, error: XCircle, info: Info } as const;
const tones = { success: "success", error: "danger", info: "brand" } as const;

/** نظير toast في الويب؛ و`undo` رسالةٌ بزرّ تراجع (حذف سطرٍ بالسحب) */
export const toast = {
  success: (message: string) => useToasts.getState().push("success", message),
  error: (message: string) => useToasts.getState().push("error", message),
  info: (message: string) => useToasts.getState().push("info", message),
  undo: (message: string, label: string, onPress: () => void) => useToasts.getState().push("info", message, { label, onPress }),
};

/** نظير ToastHost في الويب: البطاقة نفسها بأيقونتها، وتختفي كمدّتها هناك */
export function SnackbarHost() {
  const insets = useSafeAreaInsets();
  const start = useStart();
  const { message, tone, action, dismiss } = useToasts();
  useEffect(() => {
    if (!message) return;
    const timer = setTimeout(dismiss, tone === "error" || action ? 6000 : 3500);
    return () => clearTimeout(timer);
  }, [message, tone, action, dismiss]);
  if (!message) return null;
  return (
    <Animated.View
      entering={FadeInDown.duration(duration.medium2)}
      exiting={FadeOutDown.duration(duration.short3)}
      className="absolute inset-x-4 z-50 flex-row items-center gap-3 rounded-2xl border border-line bg-card px-4 py-3 shadow-pop"
      style={{ bottom: insets.bottom + 16 }}
    >
      <Icon icon={glyphs[tone]} size={20} tone={tones[tone]} />
      <Text className={`flex-1 font-sans text-sm text-ink ${start}`}>{message}</Text>
      {action ? (
        <Pressable
          hitSlop={10}
          onPress={() => {
            action.onPress();
            dismiss();
          }}
          className="rounded-lg px-2 py-1 active:bg-brand-soft"
        >
          <Text className="font-sans text-sm font-semibold text-brand">{action.label}</Text>
        </Pressable>
      ) : null}
    </Animated.View>
  );
}
