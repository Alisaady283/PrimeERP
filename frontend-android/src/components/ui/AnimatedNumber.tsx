import { useEffect, useRef, useState } from "react";
import { Text } from "react-native";
import { useReducedMotion } from "react-native-reanimated";
import { bezier, duration, easing } from "@/lib/motion";

const enter = bezier(easing.decelerate);

/** نظير AnimatedNumber في الويب: يعدّ إلى قيمته بـlong2 ومنحنى decelerate */
export function AnimatedNumber({ value, format, delay = 0, className = "" }: { value: number; format: (value: number) => string; delay?: number; className?: string }) {
  const [shown, setShown] = useState(value);
  const from = useRef(0);
  const reduced = useReducedMotion();

  useEffect(() => {
    if (reduced) {
      setShown(value);
      return;
    }
    const origin = from.current;
    from.current = value;
    let frame = 0;
    const startAt = Date.now() + delay;
    const step = () => {
      const t = Math.min(1, Math.max(0, (Date.now() - startAt) / duration.long2));
      setShown(origin + (value - origin) * enter(t));
      if (t < 1) frame = requestAnimationFrame(step);
    };
    frame = requestAnimationFrame(step);
    return () => cancelAnimationFrame(frame);
  }, [value, delay, reduced]);

  return (
    <Text numberOfLines={1} adjustsFontSizeToFit minimumFontScale={0.7} className={className}>
      {format(shown)}
    </Text>
  );
}
