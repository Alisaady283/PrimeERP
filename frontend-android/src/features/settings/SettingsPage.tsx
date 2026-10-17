import { Save } from "lucide-react-native";
import { useState } from "react";
import { KeyboardAvoidingView, ScrollView, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { choicesOf, identityOptions, inputOf, isOn, useSettings, type Setting } from "@/features/settings/settings";
import type { CustomSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { Button } from "~/components/ui/Button";
import { Combobox } from "~/components/ui/Combobox";
import { Enter } from "~/components/ui/Enter";
import { Switch, TextField } from "~/components/ui/Field";
import { Icon } from "~/components/ui/Icon";
import { Skeleton } from "~/components/ui/Skeleton";
import { toast } from "~/components/ui/Snackbar";
import { Tabs } from "~/components/ui/Tabs";

/** نظير SettingsPage في الويب: التبويبات ثم حقول الفئة عموداً واحداً، والحفظ أسفل الشاشة — والمنطق من `settings.ts` المشترك */
export default function SettingsPage(_props: { spec: CustomSpec }) {
  const t = useText();
  const insets = useSafeAreaInsets();
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
        return <TextField label={label} keyboardType="decimal-pad" value={value} onChangeText={set} />;
      case "secret":
        return <TextField label={label} secureTextEntry value={value} onChangeText={set} />;
      default:
        return <TextField label={label} value={value} onChangeText={set} />;
    }
  };

  return (
    <KeyboardAvoidingView behavior="height" className="flex-1 bg-card">
      <Tabs items={settings.categories.map((c) => ({ key: c, label: t(`Str.Settings.${c}`) }))} value={tab} onChange={setTab} />
      <ScrollView className="flex-1" contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
        {settings.loading ? (
          <Skeleton className="h-64" />
        ) : (
          settings.inCategory(tab).map((s, i) => (
            <Enter key={`${tab}.${s.key}`} index={i}>
              {input(s)}
            </Enter>
          ))
        )}
      </ScrollView>
      <View className="border-t border-line bg-card px-4 pt-3" style={{ paddingBottom: insets.bottom + 12 }}>
        <Button size="lg" icon={<Icon icon={Save} size={18} tone="on-field" />} loading={saving} disabled={settings.changed.length === 0} onPress={() => void save()}>
          {t("Str.Save")}
        </Button>
      </View>
    </KeyboardAvoidingView>
  );
}
