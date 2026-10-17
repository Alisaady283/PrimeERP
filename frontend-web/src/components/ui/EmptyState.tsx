import { Inbox } from "lucide-react";

export function EmptyState({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20 text-ink-faint">
      <span className="grid size-14 place-items-center rounded-2xl bg-paper">
        <Inbox className="size-7" />
      </span>
      <p className="text-sm">{message}</p>
    </div>
  );
}
