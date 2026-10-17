import { Camera, LogOut, Minus, Package, PauseCircle, Plus, RotateCcw, ScanLine, ShoppingCart, Trash2, Undo2, Wallet } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { motion } from "motion/react";
import { AnimatedNumber } from "@/components/ui/AnimatedNumber";
import { BarcodeScanner } from "@/components/ui/BarcodeScanner";
import { duration, easing, tween } from "@/lib/motion";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Combobox } from "@/components/ui/Combobox";
import { Dialog } from "@/components/ui/Dialog";
import { TextField } from "@/components/ui/Field";
import { Skeleton } from "@/components/ui/Skeleton";
import { toast } from "@/components/ui/Toast";
import { cn } from "@/lib/cn";
import { formatMoney } from "@/lib/format";
import { imageUrl } from "@/lib/image";
import { useText } from "@/lib/i18n";
import { pageByKey } from "@/catalog/pages";
import { DocumentEditor } from "@/features/workspace/DocumentEditor";
import type { DocumentSpec } from "@/features/workspace/spec";
import { cartNet, expectedCash, usePos } from "@/features/pos/pos";

const returnSpec = pageByKey.get("PosReturn") as DocumentSpec;

export default function PosPage() {
  const t = useText();
  const scanRef = useRef<HTMLInputElement>(null);
  const [closing, setClosing] = useState<string | null>(null);
  const [heldOpen, setHeldOpen] = useState(false);
  const [camera, setCamera] = useState(false);
  const [returning, setReturning] = useState(false);
  const pos = usePos(t, toast, () => scanRef.current?.focus());
  const { shift, cart, hold, pay } = pos;

  useEffect(() => {
    scanRef.current?.focus();
  }, [shift.data]);

  useEffect(() => {
    const keys = (e: KeyboardEvent) => {
      if (e.key === "F2") { e.preventDefault(); scanRef.current?.focus(); }
      if (e.key === "F8" && cart.length) { e.preventDefault(); hold.mutate(); }
      if (e.key === "F12" && cart.length) { e.preventDefault(); pay.mutate(); }
    };
    window.addEventListener("keydown", keys);
    return () => window.removeEventListener("keydown", keys);
  }, [cart.length, hold, pay]);

  if (shift.isLoading) return <Skeleton className="h-full" />;

  if (!shift.data)
    return (
      <div className="grid h-full place-items-center">
        <Card className="w-full max-w-md animate-rise space-y-4 p-6">
          <div className="flex items-center gap-3">
            <span className="grid size-12 place-items-center rounded-2xl bg-brand text-on-field"><Wallet className="size-5" /></span>
            <div>
              <h2 className="text-lg font-bold text-ink">{t("Str.Pos.OpenShift")}</h2>
              <p className="text-sm text-ink-tertiary">{t("Str.Pos.NoShift")}</p>
            </div>
          </div>
          <TextField label={t("Str.Pos.Opening")} type="number" value={pos.opening.cash} onChange={(e) => pos.setOpening({ ...pos.opening, cash: e.target.value })} />
          <Combobox label={t("Str.Treasury")} value={pos.opening.treasuryId} options={pos.treasuries.data ?? []} onChange={(v) => pos.setOpening({ ...pos.opening, treasuryId: v === null ? null : Number(v) })} />
          <Combobox label={t("Str.Warehouse")} value={pos.opening.warehouseId} options={pos.warehouses.data ?? []} onChange={(v) => pos.setOpening({ ...pos.opening, warehouseId: v === null ? null : Number(v) })} />
          <Button className="w-full" onClick={pos.openShift}>{t("Str.Pos.OpenShift")}</Button>
        </Card>
      </div>
    );

  return (
    <div className="grid h-full gap-4 max-lg:grid-rows-[minmax(0,1fr)_auto] lg:grid-cols-[minmax(0,1fr)_24rem]">
      <Card className="flex min-h-0 flex-col overflow-hidden">
        <div className="flex flex-wrap items-center gap-2 border-b border-line px-3 py-2.5">
          <div className="relative min-w-56 flex-1">
            <ScanLine className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-brand" />
            <input
              ref={scanRef}
              value={pos.scan}
              onChange={(e) => pos.setScan(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && void pos.submitScan()}
              placeholder={t("Str.Pos.Scan")}
              className="h-10 w-full rounded-lg border border-line bg-paper-soft ps-9 pe-11 text-sm outline-none transition focus:border-brand focus:bg-card focus:ring-4 focus:ring-brand/10"
            />
            <button
              type="button"
              onClick={() => setCamera(true)}
              title={t("Str.Scan.Camera")}
              aria-label={t("Str.Scan.Camera")}
              className="absolute end-1 top-1/2 grid size-8 -translate-y-1/2 place-items-center rounded-md text-brand transition-colors hover:bg-brand-soft"
            >
              <Camera className="size-[18px]" />
            </button>
          </div>
          <span className="text-xs text-ink-tertiary">{t("Str.Pos.ShiftInfo", shift.data.userName ?? "", new Date(shift.data.openedAt).toLocaleTimeString().slice(0, 5))}</span>
          <Button variant="ghost" size="sm" icon={<RotateCcw className="size-4" />} onClick={() => setReturning(true)}>{t("Str.Pos.ReturnTab")}</Button>
          <Button variant="ghost" size="sm" icon={<LogOut className="size-4" />} onClick={() => setClosing("")}>{t("Str.Pos.CloseShift")}</Button>
        </div>
        {pos.categoryChips.length > 1 && (
          <div className="scroll-thin flex gap-1.5 overflow-x-auto border-b border-line px-3 py-2">
            {pos.categoryChips.map((c) => (
              <button
                key={String(c.value)}
                type="button"
                onClick={() => pos.setCategoryId(c.value)}
                className={cn("h-7 shrink-0 rounded-full px-3 text-xs transition-colors", pos.categoryId === c.value ? "bg-brand font-medium text-on-field" : "bg-paper text-ink-soft hover:bg-brand-soft hover:text-ink")}
              >
                {c.label}
              </button>
            ))}
          </div>
        )}
        <div className="scroll-thin grid min-h-0 flex-1 auto-rows-min grid-cols-[repeat(auto-fill,minmax(9.5rem,1fr))] gap-3 overflow-y-auto p-3">
          {(pos.products.data?.items ?? []).map((p) => {
            const image = imageUrl(p.imageData);
            return (
              <button
                key={p.id}
                type="button"
                onClick={() => pos.addProduct(p)}
                className="group relative flex flex-col overflow-hidden rounded-xl border border-line bg-card text-start transition-all hover:-translate-y-0.5 hover:border-brand hover:shadow-pop active:scale-[0.97]"
              >
                {image ? (
                  <span className="block aspect-[4/3] w-full overflow-hidden bg-paper"><img src={image} alt="" loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-105" /></span>
                ) : (
                  <span className="grid aspect-[4/3] w-full place-items-center bg-paper text-ink-faint"><Package className="size-7" /></span>
                )}
                <span className="flex flex-1 flex-col gap-1 px-3 py-2">
                  <span className="line-clamp-2 text-[13px] font-medium leading-snug text-ink">{p.name}</span>
                  <span className="mt-auto text-sm font-bold tabular-nums text-brand">{formatMoney(p.salePrice)}</span>
                </span>
                {/* بيانات الصنف عند المرور — بسطح تلميح الرسوم */}
                <span className="pointer-events-none absolute inset-x-2 top-2 flex flex-col gap-1 rounded-xl border border-line bg-card px-3 py-2.5 text-xs text-ink opacity-0 shadow-pop transition-opacity duration-150 group-hover:opacity-100">
                  <span className="font-semibold">{p.name}</span>
                  <span className="tabular-nums">{t("Str.SalePrice")}: {formatMoney(p.salePrice)}</span>
                  {p.description && <span className="line-clamp-4 text-ink-soft">{p.description}</span>}
                </span>
              </button>
            );
          })}
        </div>
      </Card>

      <Card className="flex min-h-0 flex-col overflow-hidden">
        <div className="flex items-center gap-2 border-b border-line px-3 py-2.5">
          <ShoppingCart className="size-4 shrink-0 text-brand" />
          <Combobox dense className="flex-1" value={pos.customerId} disabled={pos.customerLocked} clearable={!pos.customerLocked} options={pos.customers.data ?? []} onChange={(v) => pos.setCustomerId(v === null ? null : Number(v))} placeholder={t("Str.Customer")} />
        </div>
        <ul className="min-h-0 flex-1 divide-y divide-line overflow-y-auto">
          {cart.map((line, i) => (
            <motion.li key={`${line.productCode}-${line.unitPrice}-${i}`} layout initial={{ opacity: 0, x: 16, backgroundColor: "var(--color-brand-soft)" }} animate={{ opacity: 1, x: 0, backgroundColor: "rgba(0,0,0,0)" }} transition={tween(duration.medium2, easing.decelerate)} className="flex items-center gap-2 px-3 py-2.5">
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-ink">{line.name}</p>
                <p className="text-xs tabular-nums text-ink-tertiary">{formatMoney(line.unitPrice)}</p>
              </div>
              <div className="flex items-center gap-1 rounded-xl bg-paper p-1">
                <button type="button" onClick={() => pos.step(i, -1)} className="grid size-7 place-items-center rounded-lg hover:bg-card"><Minus className="size-3.5" /></button>
                <span className="w-8 text-center text-sm font-semibold tabular-nums">{line.qty}</span>
                <button type="button" onClick={() => pos.step(i, 1)} className="grid size-7 place-items-center rounded-lg hover:bg-card"><Plus className="size-3.5" /></button>
              </div>
              <span className="w-20 text-end text-sm font-semibold tabular-nums">{formatMoney(cartNet(line))}</span>
              <button type="button" onClick={() => pos.removeLine(i)} className="text-ink-faint hover:text-danger"><Trash2 className="size-4" /></button>
            </motion.li>
          ))}
        </ul>
        <div className="space-y-2.5 border-t border-line bg-paper-soft p-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-ink-tertiary">{t("Str.NetTotal")}</span>
            <span className="text-2xl font-bold tabular-nums text-ink"><AnimatedNumber value={pos.total} format={formatMoney} /></span>
          </div>
          <TextField type="number" placeholder={t("Str.Pos.Paid")} value={pos.paid} onChange={(e) => pos.setPaid(e.target.value)} />
          {pos.change !== null && <p className="text-sm text-success">{formatMoney(pos.change)}</p>}
          <div className="grid grid-cols-2 gap-2">
            <Button variant="secondary" icon={<PauseCircle className="size-4" />} disabled={!cart.length} loading={hold.isPending} onClick={() => hold.mutate()}>{t("Str.Pos.Hold")}</Button>
            <Button variant="secondary" icon={<Undo2 className="size-4" />} onClick={() => setHeldOpen(true)}>{t("Str.Pos.Recall", pos.held.data?.length ?? 0)}</Button>
          </div>
          <Button className="h-12 w-full text-base" icon={<Wallet className="size-5" />} disabled={!cart.length} loading={pay.isPending} onClick={() => pay.mutate()}>
            {t("Str.Pos.Pay")}
          </Button>
        </div>
      </Card>

      <BarcodeScanner open={camera} onClose={() => setCamera(false)} onScan={(code) => void pos.submitScan(code)} />

      <Dialog open={heldOpen} title={t("Str.Pos.HeldTitle")} onClose={() => setHeldOpen(false)}>
        <ul className="space-y-2">
          {(pos.held.data ?? []).map((h) => (
            <li key={h.id} className="flex items-center justify-between rounded-xl border border-line px-4 py-3">
              <span className="text-sm text-ink">{h.label || `#${h.id}`} <span className="text-xs text-ink-faint">{new Date(h.heldAt).toLocaleTimeString()}</span></span>
              <Button size="sm" onClick={async () => (await pos.take(h.id)) && setHeldOpen(false)}>{t("Str.Pos.NewSale")}</Button>
            </li>
          ))}
          {!pos.held.data?.length && <li className="py-6 text-center text-sm text-ink-faint">{t("Str.NoData")}</li>}
        </ul>
      </Dialog>

      <DocumentEditor spec={returnSpec} id={null} open={returning} onClose={() => { setReturning(false); void shift.refetch(); }} />

      <Dialog
        open={closing !== null}
        title={t("Str.Pos.CloseShift")}
        onClose={() => setClosing(null)}
        footer={<><Button variant="secondary" onClick={() => setClosing(null)}>{t("Str.Cancel")}</Button><Button onClick={async () => (await pos.closeShift(closing ?? "")) && setClosing(null)}>{t("Str.Pos.CloseShift")}</Button></>}
      >
        <div className="space-y-4">
          <div className={cn("grid grid-cols-3 gap-3 rounded-xl bg-paper p-4 text-sm")}>
            <span className="text-ink-tertiary">{t("Str.Pos.Opening")}<b className="block tabular-nums text-ink">{formatMoney(shift.data.openingCash)}</b></span>
            <span className="text-ink-tertiary">{t("Str.Pos.CashIn")}<b className="block tabular-nums text-ink">{formatMoney(shift.data.cashIn)}</b></span>
            <span className="text-ink-tertiary">{t("Str.Pos.Expected")}<b className="block tabular-nums text-ink">{formatMoney(expectedCash(shift.data))}</b></span>
          </div>
          <TextField label={t("Str.Pos.Counted")} type="number" value={closing ?? ""} onChange={(e) => setClosing(e.target.value)} />
        </div>
      </Dialog>
    </div>
  );
}
