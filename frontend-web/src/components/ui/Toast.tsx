import { CheckCircle2, Info, TriangleAlert, X, XCircle } from "lucide-react";
import { AnimatePresence, motion } from "motion/react";
import { create } from "zustand";
import { duration, easing, tween } from "@/lib/motion";
import { cn } from "@/lib/cn";

type Tone = "success" | "error" | "warning" | "info";
type ToastItem = { id: number; tone: Tone; message: string };

type ToastState = { items: ToastItem[]; push: (tone: Tone, message: string) => void; dismiss: (id: number) => void };

let next = 1;

const useToasts = create<ToastState>((set, get) => ({
  items: [],
  push: (tone, message) => {
    const id = next++;
    set({ items: [...get().items, { id, tone, message }] });
    setTimeout(() => get().dismiss(id), tone === "error" ? 6000 : 3500);
  },
  dismiss: (id) => set({ items: get().items.filter((item) => item.id !== id) }),
}));

export const toast = {
  success: (message: string) => useToasts.getState().push("success", message),
  error: (message: string) => useToasts.getState().push("error", message),
  warning: (message: string) => useToasts.getState().push("warning", message),
  info: (message: string) => useToasts.getState().push("info", message),
};

const style: Record<Tone, { icon: typeof Info; className: string }> = {
  success: { icon: CheckCircle2, className: "text-success" },
  error: { icon: XCircle, className: "text-danger" },
  warning: { icon: TriangleAlert, className: "text-warning" },
  info: { icon: Info, className: "text-info" },
};

export function ToastHost() {
  const { items, dismiss } = useToasts();
  return (
    <div className="pointer-events-none fixed inset-x-0 bottom-4 z-[100] flex flex-col items-center gap-2 px-4">
      <AnimatePresence initial={false}>
      {items.map((item) => {
        const { icon: Icon, className } = style[item.tone];
        return (
          <motion.div
            key={item.id}
            layout
            role="status"
            initial={{ opacity: 0, y: 16, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1, transition: tween(duration.medium2, easing.decelerate) }}
            exit={{ opacity: 0, scale: 0.96, transition: tween(duration.short3, easing.accelerate) }}
            className="pointer-events-auto flex w-full max-w-md items-start gap-3 rounded-2xl border border-line bg-card px-4 py-3 shadow-pop"
          >
            <Icon className={cn("mt-0.5 size-5 shrink-0", className)} />
            <p className="flex-1 text-sm text-ink">{item.message}</p>
            <button type="button" onClick={() => dismiss(item.id)} className="text-ink-faint hover:text-ink">
              <X className="size-4" />
            </button>
          </motion.div>
        );
      })}
      </AnimatePresence>
    </div>
  );
}
