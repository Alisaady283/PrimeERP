import { useEffect, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { toast } from "@/components/ui/Toast";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { emptyValue, FieldInput, resolveField, validate } from "@/features/workspace/FieldInput";
import type { FieldSpec } from "@/features/workspace/spec";

type Props = {
  open: boolean;
  title: string;
  fields: FieldSpec[];
  onSubmit: (values: Record<string, unknown>) => Promise<unknown>;
  onClose: () => void;
  columns?: 1 | 2;
};

/** حوار حقول مُعلَنة */
export function FormDialog({ open, title, fields, onSubmit, onClose, columns = 1 }: Props) {
  const t = useText();
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
    <Dialog
      open={open}
      title={title}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            {t("Str.Cancel")}
          </Button>
          <Button onClick={submit} loading={saving}>
            {t("Str.Save")}
          </Button>
        </>
      }
    >
      <form
        className={columns === 2 ? "grid gap-4 sm:grid-cols-2" : "grid gap-4"}
        onSubmit={(event) => {
          event.preventDefault();
          void submit();
        }}
      >
        {fields.map((field) => (
          <div key={field.key} className={columns === 2 && (field.span === 2 || field.kind === "textarea") ? "sm:col-span-2" : undefined}>
            <FieldInput field={resolveField(field, values)} value={values[field.key]} error={errors[field.key]} onChange={(v) => setValues((s) => ({ ...s, [field.key]: v }))} />
          </div>
        ))}
        {failure && (
          <p role="alert" className="rounded-lg bg-danger-soft px-4 py-3 text-sm text-danger">
            {failure}
          </p>
        )}
        <button type="submit" hidden />
      </form>
    </Dialog>
  );
}
