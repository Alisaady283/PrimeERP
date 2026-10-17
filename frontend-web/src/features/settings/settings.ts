import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useOptions } from "@/features/workspace/data";
import { api } from "@/lib/api";
import { identities } from "@/lib/identities";

/** الإعدادات — منطقها مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

/** options: قائمة اختيار · shownWhen: «مفتاح=قيمة» يظهر الحقل بتحقّقها */
export type Setting = { key: string; category: string; dataType: string; value: string; isSystem: boolean; options?: string[] | null; shownWhen?: string | null };

const hidden = new Set(["Company.LogoData"]);
const order = ["Company", "Financial", "Accounts", "Documents", "UI", "Print", "Backup", "Security", "HR", "Projects", "System", "Integration"];

/** نوع حقل الإعداد */
export type SettingInput = "switch" | "treasury" | "customer" | "choice" | "identity" | "account" | "number" | "secret" | "text";

export const inputOf = (s: Setting): SettingInput =>
  s.options ? "choice"
  : s.dataType === "bool" ? "switch"
  : s.dataType === "treasury" ? "treasury"
  : s.dataType === "customer" ? "customer"
  : s.key === "UI.Identity" ? "identity"
  : s.category === "Accounts" && s.dataType === "string" ? "account"
  : s.dataType === "int" || s.dataType === "decimal" ? "number"
  : s.dataType === "secret" ? "secret"
  : "text";

export const isOn = (value: string) => value === "true" || value === "True";

/** خيارات الإعداد بنصوصها */
export const choicesOf = (s: Setting, t: (key: string) => string) => (s.options ?? []).map((o) => ({ value: o, label: t(`Str.Settings.Option.${s.key}.${o}`) }));

export const identityOptions = identities.map((i) => ({ value: i, label: i }));

/** الإعدادات بقيمها المعدَّلة وتبويباتها وحفظ المتغيّر منها */
export function useSettings() {
  const client = useQueryClient();
  const settings = useQuery({ queryKey: ["settings"], queryFn: ({ signal }) => api.get<Setting[]>("/settings", undefined, signal) });
  const accounts = useOptions({ accounts: true });
  const treasuries = useOptions({ page: "Treasuries" });
  const customers = useOptions({ page: "Customers" });
  const [values, setValues] = useState<Record<string, string>>({});

  useEffect(() => {
    if (settings.data) setValues(Object.fromEntries(settings.data.map((s) => [s.key, s.value ?? ""])));
  }, [settings.data]);

  const categories = useMemo(() => order.filter((c) => settings.data?.some((s) => s.category === c && !hidden.has(s.key))), [settings.data]);
  const changed = (settings.data ?? []).filter((s) => (values[s.key] ?? "") !== (s.value ?? ""));
  const shown = (s: Setting) => {
    if (!s.shownWhen) return true;
    const [on, value] = s.shownWhen.split("=");
    return (values[on ?? ""] ?? "") === value;
  };
  const inCategory = (category: string) => (settings.data ?? []).filter((s) => s.category === category && !hidden.has(s.key) && shown(s));
  const set = (key: string, value: string) => setValues((all) => ({ ...all, [key]: value }));

  const save = async () => {
    await api.put("/settings", Object.fromEntries(changed.map((s) => [s.key, values[s.key] ?? ""])));
    await Promise.all([client.invalidateQueries({ queryKey: ["settings"] }), client.invalidateQueries({ queryKey: ["interface"] })]);
  };

  return { loading: settings.isLoading, values, set, categories, changed, inCategory, save, accounts: accounts.data ?? [], treasuries: treasuries.data ?? [], customers: customers.data ?? [] };
}
