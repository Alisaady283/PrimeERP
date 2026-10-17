import { ArrowLeft, ArrowRight } from "lucide-react-native";
import type { ReactNode } from "react";
import { KeyboardAvoidingView, Modal, Pressable, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useText } from "@/lib/i18n";
import { useDirection, useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";

/** نظير Dialog في الويب: ورقةٌ كاملة — العنوان وإلغاء، ثم المحتوى، ثم التذييل ثابتاً أسفلها؛ ورجوع الجهاز يرجع خطوةً أو يغلق */
export function Sheet({ open, title, onClose, onBack, footer, children }: { open: boolean; title: string; onClose: () => void; onBack?: () => void; footer?: ReactNode; children: ReactNode }) {
  const t = useText();
  const start = useStart();
  const direction = useDirection();
  const insets = useSafeAreaInsets();
  return (
    <Modal visible={open} animationType="slide" onRequestClose={onBack ?? onClose}>
      <KeyboardAvoidingView behavior="height" className="flex-1 bg-paper" style={{ direction, paddingTop: insets.top + 12 }}>
        <View className="flex-row items-center gap-3 border-b border-line px-4 pb-3">
          {onBack ? (
            <Pressable hitSlop={8} onPress={onBack} className="rounded-lg p-1 active:bg-paper-soft">
              <Icon icon={direction === "rtl" ? ArrowRight : ArrowLeft} size={20} tone="ink" />
            </Pressable>
          ) : null}
          <Text numberOfLines={1} className={`flex-1 font-sans text-lg font-bold text-ink ${start}`}>{title}</Text>
          <Pressable hitSlop={12} onPress={onClose}>
            <Text className="font-sans text-sm font-semibold text-brand">{t("Str.Cancel")}</Text>
          </Pressable>
        </View>
        <View className="flex-1">{children}</View>
        {footer ? (
          <View className="gap-2 border-t border-line bg-card px-4 pt-3" style={{ paddingBottom: insets.bottom + 12 }}>
            {footer}
          </View>
        ) : null}
      </KeyboardAvoidingView>
    </Modal>
  );
}
