import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { useInterface } from "@/features/auth/interface";
import { useOptions } from "@/features/workspace/data";
import { api, messageOf, type PageResponse } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { lineNet } from "@/lib/lineCalc";
import { useDebounced } from "@/lib/useDebounced";

/** نقطة البيع — منطقها مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

export type Shift = { id: number; treasuryId: number; warehouseId: number; userName: string; openedAt: string; openingCash: number; cashIn: number; cashOut: number; salesCount: number };
export type PosProduct = { id: number; code: string; name: string; salePrice: number; imageData?: string | null; description?: string | null };
export type CartLine = { productCode: string; name: string; qty: number; unitPrice: number };
type Receipt = { documentId: number; documentNo: string; net: number; collected: number; change: number };
export type Held = { id: number; label: string; heldAt: string };
type Sale = { invoiceDate: string; customerId: number; warehouseId: number; notes: string; lines: Array<{ lineNo: number; productCode: string; qty: number; unitPrice: number }> };

/** رسائل العميل: إشعار الويب أو الأندرويد */
export type Notify = { success: (message: string) => void; error: (message: string) => void };

/** صافي السطر */
export const cartNet = (line: CartLine) => lineNet(line.qty, line.unitPrice);

/** المتوقّع في الصندوق */
export const expectedCash = (shift: Shift) => shift.openingCash + shift.cashIn - shift.cashOut;

/** الوردية والأصناف والسلّة والدفع والتعليق والاسترجاع — `onReset` يعيد التركيز على حقل المسح في العرض */
export function usePos(t: (key: string, ...args: Array<string | number>) => string, notify: Notify, onReset?: () => void) {
  const client = useQueryClient();
  const [cart, setCart] = useState<CartLine[]>([]);
  const [chosenCustomer, setCustomerId] = useState<number | null>(null);
  /** العميل النقدي افتراضاً ومُقفلاً، إلا إن سمحت الإعدادات بتغييره — والخادم يفرضه كذلك */
  const ui = useInterface();
  const customerLocked = !ui?.posCustomerEditable;
  const customerId = customerLocked ? (ui?.cashCustomerId ?? null) : (chosenCustomer ?? ui?.cashCustomerId ?? null);
  const [scan, setScan] = useState("");
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [paid, setPaid] = useState("");
  const [opening, setOpening] = useState({ cash: "0", treasuryId: null as number | null, warehouseId: null as number | null });
  const fail = (error: unknown, fallback?: string) => notify.error(messageOf(error, fallback));

  const shift = useQuery({ queryKey: ["pos", "shift"], retry: false, queryFn: ({ signal }) => api.get<Shift>("/pos/shift", undefined, signal) });
  const customers = useOptions({ page: "Customers" });
  const treasuries = useOptions({ page: "Treasuries" });
  const warehouses = useOptions({ page: "Warehouses" });
  const categories = useOptions({ categories: "Products" });
  const term = useDebounced(scan.trim());
  const products = useQuery({
    queryKey: ["pos", "products", term, categoryId],
    enabled: !!shift.data,
    queryFn: ({ signal }) => api.get<PageResponse<PosProduct>>("/pages/Products", { search: term, categoryId, pageSize: 60, isActive: true }, signal),
  });
  const held = useQuery({ queryKey: ["pos", "held"], enabled: !!shift.data, queryFn: ({ signal }) => api.get<Held[]>("/pos/held", undefined, signal) });

  const total = useMemo(() => cart.reduce((sum, l) => sum + cartNet(l), 0), [cart]);
  const change = paid !== "" && Number(paid) > total ? Number(paid) - total : null;
  const categoryChips = [{ value: null as number | null, label: t("Str.All") }, ...(categories.data ?? []).map((c) => ({ value: Number(c.value), label: c.label }))];

  const add = (product: Pick<CartLine, "productCode" | "name" | "unitPrice">, qty = 1) =>
    setCart((lines) => {
      const at = lines.findIndex((l) => l.productCode === product.productCode && l.unitPrice === product.unitPrice);
      if (at < 0) return [...lines, { ...product, qty }];
      return lines.map((l, i) => (i === at ? { ...l, qty: l.qty + qty } : l));
    });
  const addProduct = (p: PosProduct) => add({ productCode: p.code, name: p.name, unitPrice: p.salePrice });
  const step = (index: number, by: number) => setCart((c) => c.map((l, j) => (j === index ? { ...l, qty: Math.max(1, l.qty + by) } : l)));
  const removeLine = (index: number) => setCart((c) => c.filter((_, j) => j !== index));

  const sale = (): Sale => ({
    invoiceDate: new Date().toISOString().slice(0, 10),
    customerId: customerId ?? 0,
    warehouseId: shift.data?.warehouseId ?? 0,
    notes: "",
    lines: cart.map((l, i) => ({ lineNo: i + 1, productCode: l.productCode, qty: l.qty, unitPrice: l.unitPrice })),
  });

  const reset = () => {
    setCart([]);
    setPaid("");
    setCustomerId(null);
    onReset?.();
  };

  const refresh = () => client.invalidateQueries({ queryKey: ["pos"] });

  const pay = useMutation({
    mutationFn: () => api.post<Receipt>("/pos/sell", { sale: sale(), paid: paid === "" ? 0 : Number(paid) }),
    onSuccess: (r) => {
      notify.success(t("Str.Pos.Done", r.documentNo, formatMoney(r.net), formatMoney(r.change)));
      reset();
      void refresh();
    },
    onError: (e) => fail(e),
  });

  const hold = useMutation({
    mutationFn: () => api.post<void>("/pos/hold", { sale: sale(), label: "" }),
    onSuccess: () => {
      reset();
      void refresh();
    },
    onError: (e) => fail(e),
  });

  /** استرجاع بيعٍ معلَّق */
  const take = async (id: number) => {
    try {
      const restored = await api.post<Sale>(`/pos/held/${id}/take`, {});
      setCart(restored.lines.map((l) => ({ productCode: l.productCode, name: l.productCode, qty: l.qty, unitPrice: l.unitPrice })));
      setCustomerId(restored.customerId || null);
      void refresh();
      return true;
    } catch (e) {
      fail(e);
      return false;
    }
  };

  /** المسح: الكود أو الباركود، وإلا الصنف الوحيد في نتيجة البحث */
  const submitScan = async (value = scan) => {
    const code = value.trim();
    if (!code) return;
    try {
      const found = await api.get<{ product: PosProduct; code: { qty: number | null; price: number | null } }>("/products/scan", { code });
      add({ productCode: found.product.code, name: found.product.name, unitPrice: found.code.price ?? found.product.salePrice }, found.code.qty ?? 1);
      setScan("");
    } catch (e) {
      const only = products.data?.items.length === 1 ? products.data.items[0] : null;
      if (!only) return fail(e, "Str.Pos.NotFound");
      addProduct(only);
      setScan("");
    }
  };

  const openShift = async () => {
    try {
      await api.post("/pos/shift/open", { openingCash: Number(opening.cash || 0), treasuryId: opening.treasuryId, warehouseId: opening.warehouseId });
      await refresh();
    } catch (e) {
      fail(e);
    }
  };

  /** إغلاق الوردية بالنقد المعدود */
  const closeShift = async (counted: string) => {
    try {
      const closed = await api.post<{ difference: number | null }>(`/pos/shift/close?countedCash=${Number(counted || 0)}`, {});
      notify.success(t("Str.Pos.ShiftClosed", formatMoney(closed.difference ?? 0)));
      await refresh();
      return true;
    } catch (e) {
      fail(e);
      return false;
    }
  };

  return {
    shift, products, held, customers, treasuries, warehouses, categoryChips,
    cart, customerId, customerLocked, setCustomerId, scan, setScan, categoryId, setCategoryId, paid, setPaid, opening, setOpening,
    total, change, addProduct, step, removeLine, pay, hold, take, submitScan, openShift, closeShift,
  };
}
