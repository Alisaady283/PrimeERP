import { Save } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Combobox } from "@/components/ui/Combobox";
import { Switch, TextField } from "@/components/ui/Field";
import { Skeleton } from "@/components/ui/Skeleton";
import { Tabs } from "@/components/ui/Tabs";
import { toast } from "@/components/ui/Toast";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { choicesOf, identityOptions, inputOf, isOn, useSettings, type Setting } from "@/features/settings/settings";
import type { CustomSpec } from "@/features/workspace/spec";

export default function SettingsPage(_props: { spec: CustomSpec }) {
  const t = useText();
  const settings = useSettings();
  const [tab, setTab] = useState("Company");
  const [saving, setSaving] = useState(false);

  const save = async () => {
    setSaving(true);
    try {
      await settings.save();
      toast.success(t("Str.Success"));
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  const input = (s: Setting) => {
    const label = t(`Str.Settings.Label.${s.key}`);
    const value = settings.values[s.key] ?? "";
    const set = (v: string) => settings.set(s.key, v);
    switch (inputOf(s)) {
      case "switch":
        return <Switch label={label} checked={isOn(value)} onChange={(c) => set(c ? "true" : "false")} />;
      case "treasury":
        return <Combobox label={label} value={value === "" ? null : Number(value)} options={settings.treasuries} onChange={(v) => set(v === null ? "" : String(v))} />;
      case "customer":
        return <Combobox label={label} value={value === "" ? null : Number(value)} options={settings.customers} onChange={(v) => set(v === null ? "" : String(v))} />;
      case "choice":
        return <Combobox label={label} value={value} options={choicesOf(s, t)} onChange={(v) => set(String(v ?? ""))} clearable={false} />;
      case "identity":
        return <Combobox label={label} value={value} options={identityOptions} onChange={(v) => set(String(v ?? "Navy"))} clearable={false} />;
      case "account":
        return <Combobox label={label} value={value || null} options={settings.accounts} onChange={(v) => set(v === null ? "" : String(v))} />;
      case "number":
        return <TextField label={label} type="number" step="any" value={value} onChange={(e) => set(e.target.value)} />;
      // السرّي لا يعود من الخادم: فارغه يُبقي المحفوظ، والمكتوب يُرسل ليُشفَّر
      case "secret":
        return <TextField label={label} type="password" autoComplete="new-password" value={value} onChange={(e) => set(e.target.value)} />;
      default:
        return <TextField label={label} value={value} onChange={(e) => set(e.target.value)} />;
    }
  };

  return (
    <div className="flex h-full flex-col">
      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <div className="flex items-center gap-3 border-b border-line pe-3">
          <Tabs className="min-w-0 flex-1 border-b-0 px-2" items={settings.categories.map((c) => ({ key: c, label: t(`Str.Settings.${c}`) }))} value={tab} onChange={setTab} />
          <Button icon={<Save className="size-4" />} onClick={save} loading={saving} disabled={settings.changed.length === 0}>{t("Str.Save")}</Button>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-5">
          {settings.loading ? (
            <Skeleton className="h-64" />
          ) : (
            <div className="grid gap-x-5 gap-y-4 md:grid-cols-2 xl:grid-cols-3">
              {settings.inCategory(tab).map((s) => (
                <div key={s.key} className="flex min-h-[4.25rem] flex-col justify-end">{input(s)}</div>
              ))}
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}
