import { LoaderCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { EmptyState } from "@/components/ui/EmptyState";
import { toast } from "@/components/ui/Toast";
import { cn } from "@/lib/cn";
import { formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { dateOnly } from "@/features/workspace/format";
import { chooseAll, pullResult, useCandidates, type Candidate, type Chosen, type Pulled } from "@/features/workspace/pull";
import type { PullSpec } from "@/features/workspace/spec";

/** السحب من مستند */
export function PullDialog({ source, header, onPull, onClose }: { source: PullSpec | null; header: Record<string, unknown>; onPull: (pulled: Pulled) => void; onClose: () => void }) {
  const t = useText();
  const [picked, setPicked] = useState<Candidate | null>(null);
  const [chosen, setChosen] = useState<Chosen>({});
  const candidates = useCandidates(source, header);

  useEffect(() => {
    setPicked(null);
    setChosen({});
  }, [source]);

  const choose = (candidate: Candidate) => {
    setPicked(candidate);
    setChosen(chooseAll(candidate));
  };

  const pull = () => {
    const result = pullResult(picked, chosen, t);
    if ("error" in result) return toast.error(result.error);
    onPull(result.pulled);
    onClose();
  };

  const list = candidates.data ?? [];

  return (
    <Dialog
      open={!!source}
      size="xl"
      title={source ? t(`Str.Pull.From.${source.source}`) : ""}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            {t("Str.Cancel")}
          </Button>
          <Button onClick={pull} disabled={!picked}>
            {t("Str.Pull.Pull")}
          </Button>
        </>
      }
    >
      {candidates.isLoading ? (
        <div className="flex justify-center py-10 text-ink-faint">
          <LoaderCircle className="size-5 animate-spin" />
        </div>
      ) : list.length === 0 ? (
        <EmptyState message={t("Str.Pull.NoDocuments")} />
      ) : (
        <div className="grid gap-4 lg:grid-cols-[2fr_3fr]">
          <section>
            <h3 className="mb-2 text-sm font-semibold text-ink">{t("Str.Pull.Documents")}</h3>
            <div className="overflow-hidden rounded-2xl border border-line">
              <div className="grid h-11 grid-cols-[7rem_6rem_1fr_4rem] items-center gap-2 bg-field px-3 text-xs font-semibold text-on-field">
                <span>{t("Str.Doc")}</span>
                <span>{t("Str.Date")}</span>
                <span>{t("Str.Party")}</span>
                <span className="text-center">{t("Str.Pull.OpenLines")}</span>
              </div>
              {list.map((c) => (
                <button
                  key={`${c.sourceType}-${c.sourceId}`}
                  type="button"
                  onClick={() => choose(c)}
                  className={cn(
                    "grid h-10 w-full grid-cols-[7rem_6rem_1fr_4rem] items-center gap-2 border-b border-paper px-2.5 text-start text-sm transition-colors",
                    picked?.sourceId === c.sourceId ? "bg-selected font-medium" : "hover:bg-brand-soft",
                  )}
                >
                  <span className="truncate font-mono">{c.sourceNo}</span>
                  <span className="tabular-nums">{dateOnly(c.docDate)}</span>
                  <span className="truncate">{c.partyName}</span>
                  <span className="text-center tabular-nums">{c.openLineCount}</span>
                </button>
              ))}
            </div>
          </section>
          <section>
            <h3 className="mb-2 text-sm font-semibold text-ink">{t("Str.Pull.SelectedLines")}</h3>
            {picked ? (
              <div className="overflow-hidden rounded-2xl border border-line">
                <div className="grid h-11 grid-cols-[2rem_1fr_9rem_7rem] items-center gap-2 bg-field px-3 text-xs font-semibold text-on-field">
                  <span />
                  <span>{t("Str.Product")}</span>
                  <span className="text-center">{t("Str.Pull.OriginalPulled")}</span>
                  <span className="text-center">{t("Str.Qty")}</span>
                </div>
                {picked.lines.map((l) => {
                  const on = chosen[l.sourceLineId] != null;
                  return (
                    <div key={l.sourceLineId} className="grid h-10 grid-cols-[2rem_1fr_9rem_7rem] items-center gap-2 border-b border-paper px-2.5 text-sm">
                      <input
                        type="checkbox"
                        checked={on}
                        onChange={(e) => setChosen((s) => ({ ...s, [l.sourceLineId]: e.target.checked ? l.remainingQty : null }))}
                        className="size-4 accent-brand"
                      />
                      <span className="truncate">{l.productName || l.productCode}</span>
                      <span className="text-center tabular-nums text-ink-tertiary">
                        {formatMoney(l.originalQty)} / {formatMoney(l.pulledQty)}
                      </span>
                      <input
                        type="number"
                        step="any"
                        min={0}
                        max={l.remainingQty}
                        disabled={!on}
                        value={chosen[l.sourceLineId] ?? ""}
                        onChange={(e) => setChosen((s) => ({ ...s, [l.sourceLineId]: e.target.value === "" ? 0 : Number(e.target.value) }))}
                        className="h-8 w-full rounded-md border border-line bg-card px-2 text-center text-sm tabular-nums outline-none focus:border-brand disabled:bg-paper"
                      />
                    </div>
                  );
                })}
              </div>
            ) : (
              <EmptyState message={t("Str.Document.PickFirst")} />
            )}
          </section>
        </div>
      )}
    </Dialog>
  );
}
