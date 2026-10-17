import { staticOptions, useOptions } from "@/features/workspace/data";
import type { FieldSpec } from "@/features/workspace/spec";
import { useText } from "@/lib/i18n";
import { dateOnly } from "@/lib/values";
import { Combobox } from "~/components/ui/Combobox";
import { ImageInput } from "~/components/ui/ImageInput";
import { DateField, Switch, TextArea, TextField } from "~/components/ui/Field";

type Props = { field: FieldSpec; value: unknown; onChange: (value: unknown) => void; error?: string; disabled?: boolean; compact?: boolean };

/** نظير FieldInput في الويب: الحقل بنوعه من الدليل */
export function FieldInput({ field, value, onChange, error, disabled, compact }: Props) {
  const t = useText();
  const label = compact ? undefined : t(field.label);
  const fixed = staticOptions(field.source);
  const remote = useOptions(fixed ? undefined : field.source);
  const readOnly = disabled || field.readOnly;
  const dense = compact ? "h-9 rounded-lg" : "";

  switch (field.kind) {
    case "select":
      return (
        <Combobox
          label={label}
          placeholder={compact ? t(field.label) : undefined}
          value={(value as string | number | null) ?? null}
          options={fixed ?? remote.data ?? []}
          onChange={onChange}
          required={field.required}
          error={error}
          disabled={readOnly}
          clearable={!field.required}
          dense={compact}
        />
      );
    case "switch":
      return <Switch label={t(field.label)} checked={!!value} onChange={(checked) => onChange(checked)} disabled={readOnly} />;
    case "textarea":
      return <TextArea label={label} value={(value as string) ?? ""} onChangeText={onChange} maxLength={field.max} error={error} editable={!readOnly} required={field.required} />;
    case "number":
      return (
        <TextField
          label={label}
          keyboardType="decimal-pad"
          value={value === null || value === undefined ? "" : String(value)}
          onChangeText={(v) => onChange(v === "" ? null : Number(v.replace(",", ".")))}
          required={field.required}
          error={error}
          editable={!readOnly}
          className={dense}
        />
      );
    case "date":
      return <DateField label={label} placeholder={compact ? t(field.label) : undefined} value={dateOnly(value)} onChange={onChange} required={field.required} error={error} disabled={readOnly} dense={compact} />;
    case "time":
      return <DateField mode="time" label={label} placeholder={compact ? t(field.label) : undefined} value={(value as string) ?? ""} onChange={onChange} error={error} disabled={readOnly} dense={compact} />;
    case "password":
      return <TextField label={label} secureTextEntry autoComplete="new-password" value={(value as string) ?? ""} onChangeText={onChange} required={field.required} error={error} editable={!readOnly} />;
    case "image":
      return <ImageInput label={t(field.label)} value={value} onChange={onChange} disabled={readOnly} />;
    default:
      return <TextField label={label} value={(value as string) ?? ""} onChangeText={onChange} maxLength={field.max} required={field.required} error={error} editable={!readOnly} className={dense} />;
  }
}
