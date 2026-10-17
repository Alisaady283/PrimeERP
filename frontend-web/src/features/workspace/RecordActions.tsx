import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { toast } from "@/components/ui/Toast";
import { usePagePermission, useSession } from "@/features/auth/session";
import { runRecordAction } from "@/features/workspace/pageActions";
import type { RecordAction } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";

/** أزرار السجل المفتوح في حواره (حوار الكيان ومحرّر المستند)، وما تعيده يُعرض مرةً ويُنسخ — نظير RowAction في الدسكتوب */
export function RecordActions({ spec, id }: { spec: { key: string; permission: string; recordActions?: RecordAction[] }; id: number | null }) {
  const t = useText();
  const gate = usePagePermission(spec);
  const permissions = useSession((s) => s.user?.permissions);
  const [shown, setShown] = useState<{ label: string; text: string } | null>(null);

  if (id === null) return null;
  const allowed = (spec.recordActions ?? []).filter((a) => permissions?.includes(a.permission ?? `${gate}.${a.action}`));

  return (
    <>
      {allowed.map((a) => (
        <Button key={a.key} variant="secondary" onClick={async () => {
          try { setShown({ label: a.label, text: await runRecordAction(a, id) }); } catch (failure) { toast.error(messageOf(failure)); }
        }}>
          {t(a.label)}
        </Button>
      ))}
      <Dialog open={shown !== null} title={t(shown?.label ?? "")} onClose={() => setShown(null)}
        footer={<Button onClick={() => { void navigator.clipboard.writeText(shown?.text ?? ""); toast.success(t("Str.Success")); }}>{t("Str.Copy")}</Button>}>
        <code dir="ltr" className="block select-all break-all rounded-lg border border-line bg-paper px-3 py-2.5 font-mono text-sm text-ink">{shown?.text}</code>
      </Dialog>
    </>
  );
}
