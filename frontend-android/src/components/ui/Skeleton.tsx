import { useEffect } from "react";
import Animated, { useAnimatedStyle, useSharedValue, withRepeat, withTiming } from "react-native-reanimated";
import { duration } from "@/lib/motion";

/** نظير Skeleton في الويب: نبضٌ هادئ */
export function Skeleton({ className = "" }: { className?: string }) {
  const pulse = useSharedValue(1);
  useEffect(() => {
    pulse.value = withRepeat(withTiming(0.5, { duration: duration.long2 * 2 }), -1, true);
  }, [pulse]);
  const style = useAnimatedStyle(() => ({ opacity: pulse.value }));
  return <Animated.View className={`rounded-xl bg-paper ${className}`} style={style} />;
}
