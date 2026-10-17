import { useNavigate } from "@tanstack/react-router";
import { Bell } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useNotifications } from "@/features/notifications/notifications";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";

const dot = { danger: "bg-danger", warning: "bg-warning", info: "bg-brand" } as const;

/** جرس التنبيهات في الشريط — نظير جرس الدسكتوب: الأحدث أولاً، والنقر يفتح الصفحة ويعلّمه مقروءاً */
export function NotificationBell() {
  const t = useText();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const { count, items, markRead } = useNotifications(open);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  return (
    <div ref={root} className="relative">
      <button
        type="button"
        aria-label={t("Str.Notifications.Title")}
        onClick={() => setOpen(!open)}
        className="relative grid size-9 place-items-center rounded-lg text-on-field-soft transition-colors hover:bg-field-hover hover:text-on-field"
      >
        <Bell className="size-5" />
        {count > 0 && (
          <span className="absolute -end-0.5 -top-0.5 grid h-4 min-w-4 place-items-center rounded-full bg-danger px-1 text-[10px] font-bold tabular-nums text-white">
            {count > 99 ? "99+" : count}
          </span>
        )}
      </button>
      {open && (
        <div className="absolute end-0 top-full z-50 mt-1.5 w-[22rem] animate-rise overflow-hidden rounded-xl border border-line bg-card text-ink shadow-pop">
          <div className="flex items-center justify-between border-b border-line px-3.5 py-2.5">
            <span className="text-sm font-semibold">{t("Str.Notifications.Title")}</span>
            <button type="button" className="text-xs text-brand hover:underline" onClick={() => markRead(items.filter((n) => !n.isRead).map((n) => n.id))}>
              {t("Str.Notifications.MarkAll")}
            </button>
          </div>
          <div className="scroll-thin max-h-[26rem] overflow-y-auto">
            {items.length === 0 && <p className="px-3.5 py-6 text-center text-sm text-ink-tertiary">{t("Str.Notifications.Empty")}</p>}
            {items.map((n) => (
              <button
                key={n.id}
                type="button"
                onClick={() => {
                  markRead([n.id]);
                  setOpen(false);
                  if (n.pageKey) void navigate({ to: "/p/$page", params: { page: n.pageKey } });
                }}
                className="flex w-full gap-2.5 px-3.5 py-2.5 text-start transition-colors hover:bg-paper"
              >
                <span className={cn("mt-1.5 size-2 shrink-0 rounded-full", n.isRead ? "bg-line" : dot[n.severity] ?? dot.info)} />
                <span className="flex min-w-0 flex-col gap-0.5">
                  <span className={cn("text-sm", !n.isRead && "font-semibold")}>{n.title}</span>
                  <span className="text-xs text-ink-soft">{n.body}</span>
                  <span className="text-[11px] tabular-nums text-ink-tertiary">{new Date(n.createdAt).toLocaleString()}</span>
                </span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
