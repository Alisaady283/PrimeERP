import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { camel } from "@/lib/values";
import type { PullSpec } from "@/features/workspace/spec";

/** السحب من مستند — منطقه مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

export type CandidateLine = { sourceLineId: number; productCode: string; productName: string; originalQty: number; pulledQty: number; remainingQty: number; unitValue: number };
export type Candidate = { sourceType: string; sourceId: number; sourceNo: string; docDate: string; partyName: string; lines: CandidateLine[]; headerValues: Record<string, unknown>; openLineCount: number; remainingQty: number };
export type Pulled = { lines: Array<{ line: CandidateLine; qty: number; candidate: Candidate }>; header: Record<string, unknown> };
/** الكمية المختارة لكل سطر، وnull لغير المختار */
export type Chosen = Record<number, number | null>;

/** المستندات المتاحة للسحب بمطابقة الرأس */
export const useCandidates = (source: PullSpec | null, header: Record<string, unknown>) => {
  const match = source?.match;
  return useQuery({
    queryKey: ["pull", source?.source, source?.scope, match ? header[match.header] : null],
    enabled: !!source,
    queryFn: () =>
      api.post<Candidate[]>("/pull/available", {
        sourceKind: source!.source,
        matchFields: match ? [match.field] : [],
        matchValues: match && header[match.header] != null ? { [match.field]: String(header[match.header]) } : {},
        scope: source!.scope,
      }),
  });
};

/** كل سطرٍ مختارٌ بمتبقّيه عند اختيار المستند */
export const chooseAll = (candidate: Candidate): Chosen => Object.fromEntries(candidate.lines.map((l) => [l.sourceLineId, l.remainingQty]));

/** نتيجة السحب أو رسالة الخطأ */
export function pullResult(picked: Candidate | null, chosen: Chosen, t: (key: string, ...args: string[]) => string): { pulled: Pulled } | { error: string } {
  if (!picked) return { error: t("Str.Document.PickFirst") };
  const lines = picked.lines.filter((l) => chosen[l.sourceLineId] != null && Number(chosen[l.sourceLineId]) > 0);
  const over = lines.find((l) => Number(chosen[l.sourceLineId]) > l.remainingQty);
  if (over) return { error: t("Str.Pull.Exceeds", formatMoney(over.remainingQty), over.productCode) };
  if (lines.length === 0) return { error: t("Str.Pull.NoLines") };
  return {
    pulled: {
      lines: lines.map((line) => ({ line, qty: Number(chosen[line.sourceLineId]), candidate: picked })),
      header: Object.fromEntries(Object.entries(picked.headerValues ?? {}).map(([k, v]) => [camel(k), v])),
    },
  };
}
