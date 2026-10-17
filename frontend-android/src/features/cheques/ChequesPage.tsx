import { useState } from "react";
import { ScrollView, Text, View } from "react-native";
import { useCan, usePagePermission } from "@/features/auth/session";
import { chequeColumns, useChequeMove } from "@/features/cheques/cheques";
import type { CustomSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Combobox } from "~/components/ui/Combobox";
import { TextField } from "~/components/ui/Field";
import { Sheet } from "~/components/ui/Sheet";
import { toast } from "~/components/ui/Snackbar";
import { FieldInput } from "~/features/workspace/FieldInput";
import { ListFrame } from "~/features/workspace/ListPage";
import { useOptions } from "@/features/workspace/data";

/** نظير ChequesPage في الويب: القائمة، ولمس الشيك ينقله بين حالاته — والمنطق من `cheques.ts` المشترك */
export default function ChequesPage({ spec }: { spec: CustomSpec }) {
  const t = useText();
  const start = useStart();
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

  const fact = (label: string, value: string) => (
    <View className="flex-row justify-between gap-3">
      <Text className="font-sans text-xs text-ink-tertiary">{label}</Text>
      <Text className="font-sans text-sm font-semibold text-ink">{value}</Text>
    </View>
  );

  return (
    <>
      <ListFrame pageKey="Cheques" title={spec.title} permission={gate} columns={chequeColumns} onOpen={canEdit ? open : undefined} />
      <Sheet
        open={!!cheque}
        title={`${t("Str.Field.ChequeNo")} ${String(cheque?.chequeNo ?? "")}`}
        onClose={close}
        footer={
          <Button size="lg" loading={saving} disabled={move.toStatus === null} onPress={() => void save()}>
            {t("Str.Save")}
          </Button>
        }
      >
        {cheque ? (
          <ScrollView contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
            <View className={`gap-2 rounded-xl bg-paper-soft p-4 ${start}`}>
              {fact(t("Str.Party"), String(cheque.partyName ?? ""))}
              {fact(t("Str.Amount"), formatMoney(Number(cheque.amount ?? 0)))}
              {fact(t("Str.Status"), String(cheque.statusName ?? ""))}
            </View>
            <Combobox label={t("Str.Status")} required value={move.toStatus} options={statuses} onChange={(v) => setMove({ ...move, toStatus: v === null ? null : Number(v) })} />
            <FieldInput field={{ key: "movementDate", label: "Str.Date", kind: "date" }} value={move.movementDate} onChange={(v) => setMove({ ...move, movementDate: String(v ?? "") })} />
            <Combobox label={t("Str.Treasury")} value={move.treasuryId} options={treasuries.data ?? []} onChange={(v) => setMove({ ...move, treasuryId: v === null ? null : Number(v) })} />
            <TextField label={t("Str.Notes")} value={move.notes} onChangeText={(notes) => setMove({ ...move, notes })} />
          </ScrollView>
        ) : null}
      </Sheet>
    </>
  );
}
