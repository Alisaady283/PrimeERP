import { useEffect, type ReactNode } from "react";
import Animated, { Easing, useAnimatedStyle, useReducedMotion, useSharedValue, withDelay, withTiming } from "react-native-reanimated";
import { duration, easing, ENTRANCE_STEP } from "@/lib/motion";

const [x1, y1, x2, y2] = easing.decelerate;
const decelerate = Easing.bezier(x1, y1, x2, y2);

/** دخول عنصرٍ في التتابع — نظير enterAt في الويب: long2 وفاصل ENTRANCE_STEP */
export function Enter({ index = 0, children, className, from = 16 }: { index?: number; children: ReactNode; className?: string; from?: number }) {
  const reduced = useReducedMotion();
  const shown = useSharedValue(reduced ? 1 : 0);

  useEffect(() => {
    if (!reduced) shown.value = withDelay(index * ENTRANCE_STEP, withTiming(1, { duration: duration.long2, easing: decelerate }));
  }, [index, reduced, shown]);

  const style = useAnimatedStyle(() => ({ opacity: shown.value, transform: [{ translateY: (1 - shown.value) * from }] }));
  return (
    <Animated.View className={className} style={style}>
      {children}
    </Animated.View>
  );
}
