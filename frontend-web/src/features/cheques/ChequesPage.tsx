import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Combobox } from "@/components/ui/Combobox";
import { Dialog } from "@/components/ui/Dialog";
import { TextField } from "@/components/ui/Field";
import { toast } from "@/components/ui/Toast";
import { useCan, usePagePermission } from "@/features/auth/session";
import { messageOf } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { useOptions } from "@/features/workspace/data";
import { ListFrame } from "@/features/workspace/ListPage";
import { chequeColumns, useChequeMove } from "@/features/cheques/cheques";
import type { CustomSpec } from "@/features/workspace/spec";

export default function ChequesPage({ spec }: { spec: CustomSpec }) {
  const t = useText();
  const gate = usePagePermission(spec);
  const canEdit = useCan(`${gate}.Edit`);
  const [saving, setSaving] = useState(false);
  const treasuries = useOptions({ page: "Treasuries" });
  const { cheque, open, close, move, setMove, statuses, submit } = useChequeMove(t);

  const save = async () => {
    if (!cheque || move.toStatus === null) return;
    setSaving(true);
    try {
      await submit();
      toast.success(t("Str.Success"));
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <ListFrame pageKey="Cheques" title={spec.title} permission={gate} columns={chequeColumns} onOpen={canEdit ? open : undefined} />
      <Dialog
        open={!!cheque}
        title={`${t("Str.Field.ChequeNo")} ${String(cheque?.chequeNo ?? "")}`}
        onClose={close}
        footer={
          <>
            <Button variant="secondary" onClick={close}>{t("Str.Cancel")}</Button>
            <Button onClick={save} loading={saving} disabled={move.toStatus === null}>{t("Str.Save")}</Button>
          </>
        }
      >
        {cheque && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3 rounded-xl bg-paper p-4 text-sm">
              <span className="text-ink-tertiary">{t("Str.Party")}: <b className="text-ink">{String(cheque.partyName ?? "")}</b></span>
              <span className="text-ink-tertiary">{t("Str.Amount")}: <b className="tabular-nums text-ink">{formatMoney(Number(cheque.amount ?? 0))}</b></span>
              <span className="text-ink-tertiary">{t("Str.Status")}: <b className="text-ink">{String(cheque.statusName ?? "")}</b></span>
            </div>
            <Combobox label={t("Str.Status")} required value={move.toStatus} options={statuses} onChange={(v) => setMove({ ...move, toStatus: v === null ? null : Number(v) })} />
            <TextField label={t("Str.Date")} type="date" value={move.movementDate} onChange={(e) => setMove({ ...move, movementDate: e.target.value })} />
            <Combobox label={t("Str.Treasury")} value={move.treasuryId} options={treasuries.data ?? []} onChange={(v) => setMove({ ...move, treasuryId: v === null ? null : Number(v) })} />
            <TextField label={t("Str.Notes")} value={move.notes} onChange={(e) => setMove({ ...move, notes: e.target.value })} />
          </div>
        )}
      </Dialog>
    </>
  );
}
