import { animate, useReducedMotion } from "motion/react";
import { useEffect, useRef, useState } from "react";
import { duration, easing, tween } from "@/lib/motion";

/** رقمٌ يعدّ إلى قيمته */
export function AnimatedNumber({ value, format, delay = 0 }: { value: number; format: (value: number) => string; delay?: number }) {
  const [shown, setShown] = useState(value);
  const from = useRef(0);
  const reduced = useReducedMotion();

  useEffect(() => {
    if (reduced) return setShown(value);
    const controls = animate(from.current, value, { ...tween(duration.long2, easing.decelerate, delay), onUpdate: setShown });
    from.current = value;
    return () => controls.stop();
  }, [value, reduced, delay]);

  return <>{format(shown)}</>;
}
