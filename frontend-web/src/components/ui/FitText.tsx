import { useLayoutEffect, useRef, type ReactNode } from "react";
import { cn } from "@/lib/cn";

/**
 * حجم الخط محسوبٌ من عرض المكان وعرض النص النهائي (full) مقيساً بخطّ العنصر نفسه:
 * الأقصى من صنف الخط، ويصغر بنسبة ما يسع حتى الأدنى (min).
 */
export function FitText({ full, children, className, min = 0.7 }: { full: string; children: ReactNode; className?: string; min?: number }) {
  const ref = useRef<HTMLSpanElement>(null);
  const probe = useRef<HTMLSpanElement>(null);

  useLayoutEffect(() => {
    const el = ref.current;
    const ruler = probe.current;
    if (!el || !ruler) return;
    const measure = () => {
      el.style.fontSize = "";
      const max = parseFloat(getComputedStyle(el).fontSize);
      ruler.textContent = full;
      el.style.fontSize = `${max * Math.max(min, Math.min(1, el.clientWidth / Math.max(1, ruler.offsetWidth)))}px`;
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    return () => observer.disconnect();
  }, [full, min]);

  return (
    <span ref={ref} title={full} className={cn("relative block overflow-hidden text-ellipsis whitespace-nowrap", className)}>
      <span ref={probe} aria-hidden className="invisible absolute start-0 top-0 w-max whitespace-nowrap" />
      {children}
    </span>
  );
}
