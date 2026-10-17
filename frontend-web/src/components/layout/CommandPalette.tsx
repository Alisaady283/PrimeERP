import { useNavigate } from "@tanstack/react-router";
import { AnimatePresence, motion } from "motion/react";
import { CornerDownLeft, Search } from "lucide-react";
import { duration, easing, tween } from "@/lib/motion";
import { useEffect, useMemo, useRef, useState } from "react";
import { sections } from "@/catalog/navigation";
import { sectionIcons } from "@/components/layout/sectionIcons";
import { pageByKey } from "@/catalog/pages";
import { useVisiblePage } from "@/features/auth/interface";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";

/** تنقّلٌ سريع بـCtrl+K */
export function CommandPalette({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useText();
  const navigate = useNavigate();
  const visible = useVisiblePage();
  const [term, setTerm] = useState("");
  const [cursor, setCursor] = useState(0);
  const input = useRef<HTMLInputElement>(null);
  const list = useRef<HTMLUListElement>(null);

  useEffect(() => {
    const keys = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        onOpenChange(!open);
      }
    };
    window.addEventListener("keydown", keys);
    return () => window.removeEventListener("keydown", keys);
  }, [open, onOpenChange]);

  useEffect(() => {
    if (!open) return;
    setTerm("");
    setCursor(0);
    requestAnimationFrame(() => input.current?.focus());
  }, [open]);

  const results = useMemo(() => {
    const q = term.trim().toLowerCase();
    return sections.flatMap((s) =>
      s.pages
        .filter(visible)
        .map((key) => ({ key, title: t(pageByKey.get(key)!.title), section: t(`Str.Nav.${s.key}`), Icon: sectionIcons[s.icon] }))
        .filter((p) => !q || p.title.toLowerCase().includes(q) || p.section.toLowerCase().includes(q)),
    );
  }, [term, visible, t]);

  useEffect(() => {
    setCursor(0);
  }, [term]);
  useEffect(() => {
    list.current?.children[cursor]?.scrollIntoView({ block: "nearest" });
  }, [cursor]);

  const go = (key: string) => {
    onOpenChange(false);
    void navigate({ to: "/p/$page", params: { page: key } });
  };

  return (
    <AnimatePresence>
      {open && (
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1, transition: tween(duration.short4, easing.decelerate) }} exit={{ opacity: 0, transition: tween(duration.short3, easing.accelerate) }} className="fixed inset-0 z-[90] bg-brand/30 backdrop-blur-[2px]" onMouseDown={() => onOpenChange(false)}>
          <motion.div
            initial={{ opacity: 0, y: -12, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1, transition: tween(duration.medium2, easing.decelerate) }}
            exit={{ opacity: 0, y: -8, scale: 0.98, transition: tween(duration.short3, easing.accelerate) }}
            onMouseDown={(e) => e.stopPropagation()}
            className="mx-auto mt-[12vh] w-[min(36rem,calc(100vw-2rem))] overflow-hidden rounded-2xl border border-line bg-card shadow-pop"
          >
            <div className="flex items-center gap-3 border-b border-line px-4">
              <Search className="size-4 text-ink-faint" />
              <input
                ref={input}
                value={term}
                onChange={(e) => setTerm(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Escape") onOpenChange(false);
                  if (e.key === "ArrowDown") { e.preventDefault(); setCursor((c) => Math.min(results.length - 1, c + 1)); }
                  if (e.key === "ArrowUp") { e.preventDefault(); setCursor((c) => Math.max(0, c - 1)); }
                  if (e.key === "Enter" && results[cursor]) go(results[cursor].key);
                }}
                placeholder={t("Str.Command.Placeholder")}
                className="h-12 flex-1 bg-transparent text-sm outline-none placeholder:text-ink-faint"
              />
              <kbd className="rounded-md border border-line px-1.5 py-0.5 font-sans text-[11px] text-ink-faint">Esc</kbd>
            </div>
            <ul ref={list} className="scroll-thin max-h-[50vh] overflow-y-auto p-1.5">
              {results.map((r, i) => (
                <li key={r.key}>
                  <button
                    type="button"
                    onMouseEnter={() => setCursor(i)}
                    onClick={() => go(r.key)}
                    className={cn("flex w-full items-center gap-3 rounded-lg px-3 py-2 text-start text-sm transition-colors", i === cursor ? "bg-brand-soft text-ink" : "text-ink-soft")}
                  >
                    <r.Icon className={cn("size-4 shrink-0", i === cursor ? "text-brand" : "text-ink-faint")} />
                    <span className="flex-1 truncate">{r.title}</span>
                    <span className="text-xs text-ink-faint">{r.section}</span>
                    {i === cursor && <CornerDownLeft className="size-3.5 text-ink-faint" />}
                  </button>
                </li>
              ))}
              {results.length === 0 && <li className="py-8 text-center text-sm text-ink-faint">{t("Str.NoData")}</li>}
            </ul>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
