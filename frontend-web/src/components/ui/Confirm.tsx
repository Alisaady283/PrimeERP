import { TriangleAlert } from "lucide-react";
import { create } from "zustand";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { text } from "@/lib/i18n";

type Request = { title: string; message: string; danger: boolean; resolve: (ok: boolean) => void };

const useConfirmState = create<{ request: Request | null; set: (request: Request | null) => void }>((set) => ({
  request: null,
  set: (request) => set({ request }),
}));

export const confirm = (message: string, options: { title?: string; danger?: boolean } = {}) =>
  new Promise<boolean>((resolve) =>
    useConfirmState.getState().set({ title: options.title ?? text("Str.Confirm"), message, danger: options.danger ?? true, resolve }),
  );

export function ConfirmHost() {
  const { request, set } = useConfirmState();
  const close = (ok: boolean) => {
    request?.resolve(ok);
    set(null);
  };

  return (
    <Dialog
      open={!!request}
      title={request?.title ?? ""}
      onClose={() => close(false)}
      footer={
        <>
          <Button variant="secondary" onClick={() => close(false)}>
            {text("Str.Cancel")}
          </Button>
          <Button variant={request?.danger ? "danger" : "primary"} onClick={() => close(true)}>
            {text("Str.Confirm")}
          </Button>
        </>
      }
    >
      <div className="flex items-start gap-4">
        <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-warning-soft text-warning">
          <TriangleAlert className="size-5" />
        </span>
        <p className="pt-2 text-sm leading-relaxed text-ink-soft">{request?.message}</p>
      </div>
    </Dialog>
  );
}
