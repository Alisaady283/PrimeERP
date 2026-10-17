import { useEffect, useState } from "react";
import { ScrollView, Text, View } from "react-native";
import { resolveField, validate } from "@/features/workspace/fieldRules";
import type { FieldSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { emptyValue } from "@/lib/values";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Sheet } from "~/components/ui/Sheet";
import { toast } from "~/components/ui/Snackbar";
import { FieldInput } from "~/features/workspace/FieldInput";

/** نظير FormDialog في الويب: حقولٌ مُعلَنة في ورقة، والتحقق من `fieldRules` المشترك */
export function FormDialog({ open, title, fields, onSubmit, onClose }: { open: boolean; title: string; fields: FieldSpec[]; onSubmit: (values: Record<string, unknown>) => Promise<unknown>; onClose: () => void }) {
  const t = useText();
  const start = useStart();
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [failure, setFailure] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setErrors({});
    setFailure(null);
    setValues(Object.fromEntries(fields.map((f) => [f.key, f.defaultValue ?? emptyValue(f)])));
  }, [open]);

  const submit = async () => {
    const found = validate(fields, values, t);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    setSaving(true);
    try {
      await onSubmit(values);
      toast.success(t("Str.Success"));
      onClose();
    } catch (error) {
      setFailure(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Sheet
      open={open}
      title={title}
      onClose={onClose}
      footer={
        <Button size="lg" loading={saving} onPress={() => void submit()}>
          {t("Str.Save")}
        </Button>
      }
    >
      <ScrollView contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
        {fields.map((field) => (
          <FieldInput key={field.key} field={resolveField(field, values)} value={values[field.key]} error={errors[field.key]} onChange={(v) => setValues((s) => ({ ...s, [field.key]: v }))} />
        ))}
        {failure ? (
          <View className="rounded-xl bg-danger-soft px-4 py-3">
            <Text className={`font-sans text-sm text-danger ${start}`}>{failure}</Text>
          </View>
        ) : null}
      </ScrollView>
    </Sheet>
  );
}
