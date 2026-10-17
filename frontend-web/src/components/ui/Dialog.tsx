import { useEffect, useRef, type ReactNode } from "react";
import { X } from "lucide-react";

type DialogProps = { open: boolean; title: string; onClose: () => void; children: ReactNode; footer?: ReactNode; size?: "md" | "xl" };

export function Dialog({ open, title, onClose, children, footer, size = "md" }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      className={`m-auto ${size === "xl" ? "w-[min(80rem,calc(100vw-2rem))]" : "w-[min(40rem,calc(100vw-2rem))]"} overflow-hidden rounded-2xl bg-card p-0 text-ink shadow-pop backdrop:bg-brand/40 backdrop:backdrop-blur-[2px] open:animate-rise`}
    >
      <div className="flex items-center justify-between bg-field px-6 py-4 text-on-field">
        <h2 className="text-base font-semibold">{title}</h2>
        <button type="button" onClick={onClose} className="rounded-lg p-1.5 text-on-field-soft transition-colors hover:bg-field-hover hover:text-on-field">
          <X className="size-4" />
        </button>
      </div>
      <div className={`${size === "xl" ? "max-h-[78vh]" : "max-h-[70vh]"} overflow-y-auto px-6 py-5`}>{children}</div>
      {footer && <div className="flex items-center justify-end gap-2 border-t border-line bg-paper-soft px-6 py-4">{footer}</div>}
    </dialog>
  );
}
