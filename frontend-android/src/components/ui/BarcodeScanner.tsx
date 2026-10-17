import { CameraView, useCameraPermissions } from "expo-camera";
import * as Haptics from "expo-haptics";
import { CameraOff, Flashlight, FlashlightOff, ScanLine, X } from "lucide-react-native";
import { useEffect, useMemo, useState } from "react";
import { Modal, Pressable, Text, View } from "react-native";
import Animated, { Easing, useAnimatedStyle, useSharedValue, withRepeat, withTiming } from "react-native-reanimated";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useText } from "@/lib/i18n";
import { duration } from "@/lib/motion";
import { BARCODE_TYPES, scanGate } from "@/lib/scan";
import { Button } from "~/components/ui/Button";
import { Icon } from "~/components/ui/Icon";

/**
 * نظير BarcodeScanner في الويب بكاميرا الجهاز الأصلية (expo-camera): قراءةٌ متتابعة، وكل كودٍ جديد يُبلَّغ مرّةً
 * (scanGate المشترك) باهتزازٍ خفيف — إطار تصويبٍ بخطٍّ يتحرّك، وإضاءةٌ للمكان المعتم.
 */
export function BarcodeScanner({ open, onClose, onScan }: { open: boolean; onClose: () => void; onScan: (code: string) => void }) {
  const t = useText();
  const insets = useSafeAreaInsets();
  const [permission, request] = useCameraPermissions();
  const [torch, setTorch] = useState(false);
  const accept = useMemo(() => scanGate(), [open]);
  const sweep = useSharedValue(0);

  useEffect(() => {
    if (open && permission && !permission.granted && permission.canAskAgain) void request();
  }, [open, permission?.granted]);

  useEffect(() => {
    sweep.value = 0;
    if (open) sweep.value = withRepeat(withTiming(1, { duration: duration.long2 * 3, easing: Easing.inOut(Easing.quad) }), -1, true);
  }, [open, sweep]);
  const line = useAnimatedStyle(() => ({ top: `${8 + sweep.value * 84}%` }));

  return (
    <Modal visible={open} animationType="slide" onRequestClose={onClose} statusBarTranslucent>
      <View className="flex-1 bg-black">
        {permission?.granted ? (
          <CameraView
            style={{ flex: 1 }}
            facing="back"
            enableTorch={torch}
            barcodeScannerSettings={{ barcodeTypes: [...BARCODE_TYPES] }}
            onBarcodeScanned={({ data }) => {
              if (!data || !accept(data)) return;
              void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
              onScan(data);
            }}
          />
        ) : (
          <View className="flex-1 items-center justify-center gap-4 px-8">
            <Icon icon={CameraOff} size={36} tone="on-field-faint" />
            <Text className="text-center font-sans text-sm text-on-field-soft">{t("Str.Scan.Denied")}</Text>
            <Button onPress={() => void request()}>{t("Str.Scan.Allow")}</Button>
          </View>
        )}

        {permission?.granted ? (
          <View pointerEvents="none" className="absolute inset-0 items-center justify-center">
            <View className="h-[34%] w-[78%] overflow-hidden rounded-2xl border-2 border-white/85" style={{ boxShadow: "0 0 0 9999px rgba(0,0,0,0.4)" }}>
              <Animated.View className="absolute inset-x-3 h-0.5 rounded-full bg-chart-5" style={line} />
            </View>
            <View className="mt-5 flex-row items-center gap-2 rounded-full bg-black/50 px-4 py-2">
              <Icon icon={ScanLine} size={16} tone="on-field" />
              <Text className="font-sans text-sm text-on-field">{t("Str.Scan.Hint")}</Text>
            </View>
          </View>
        ) : null}

        <View className="absolute inset-x-0 flex-row items-center justify-between px-4" style={{ top: insets.top + 8 }}>
          <Pressable hitSlop={10} accessibilityLabel={t("Str.Close")} onPress={onClose} className="size-11 items-center justify-center rounded-full bg-black/50 active:bg-black/70">
            <Icon icon={X} size={22} tone="on-field" />
          </Pressable>
          <Text className="font-sans text-base font-semibold text-on-field">{t("Str.Scan.Camera")}</Text>
          {permission?.granted ? (
            <Pressable hitSlop={10} accessibilityLabel={t("Str.Scan.Torch")} onPress={() => setTorch((v) => !v)} className={`size-11 items-center justify-center rounded-full ${torch ? "bg-white" : "bg-black/50"}`}>
              <Icon icon={torch ? FlashlightOff : Flashlight} size={20} tone={torch ? "ink" : "on-field"} />
            </Pressable>
          ) : (
            <View className="size-11" />
          )}
        </View>
      </View>
    </Modal>
  );
}
