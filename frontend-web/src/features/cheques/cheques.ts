import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { col, money } from "@/catalog/fields";
import { pageKeys, type Row } from "@/features/workspace/data";
import { api } from "@/lib/api";

/** الشيكات — منطقها مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

const statusName = ["", "InHand", "Deposited", "Collected", "Bounced", "Returned", "Issued", "Paid"];

export const chequeColumns = [
  col("chequeNo", "Str.Field.ChequeNo", "code"), col("directionName", "Str.Cheque.Direction"), col("partyName", "Str.Party", "text", { width: "minmax(14rem,2fr)" }),
  money("amount", "Str.Amount", { total: true }), col("dueDate", "Str.Cheque.DueDate", "date"), col("bankName", "Str.Cheque.Bank"), col("statusName", "Str.Status", "status"),
];

export type ChequeMove = { toStatus: number | null; movementDate: string; treasuryId: number | null; notes: string };

const today = () => new Date().toISOString().slice(0, 10);

/** نقل الشيك بين حالاته: المسموح منها للمختار، والإرسال */
export function useChequeMove(t: (key: string) => string) {
  const client = useQueryClient();
  const [cheque, setCheque] = useState<Row | null>(null);
  const [move, setMove] = useState<ChequeMove>({ toStatus: null, movementDate: today(), treasuryId: null, notes: "" });
  const transitions = useQuery({
    queryKey: ["cheques", "transitions", cheque?.id],
    enabled: !!cheque,
    queryFn: ({ signal }) => api.get<number[]>(`/cheques/${cheque!.id}/transitions`, undefined, signal),
  });
  const statuses = (transitions.data ?? []).map((s) => ({ value: s, label: t(`Str.Cheque.Status.${statusName[s] ?? s}`) }));

  const open = (row: Row) => {
    setCheque(row);
    setMove((m) => ({ ...m, toStatus: null, notes: "" }));
  };

  const submit = async () => {
    await api.post("/cheques/move", { chequeId: cheque!.id, ...move });
    setCheque(null);
    await client.invalidateQueries({ queryKey: pageKeys.all("Cheques") });
  };

  return { cheque, open, close: () => setCheque(null), move, setMove, statuses, submit };
}
