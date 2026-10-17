import { Combobox } from "@/components/ui/Combobox";
import { Switch, TextArea, TextField } from "@/components/ui/Field";
import { ImageInput } from "@/components/ui/ImageInput";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";
import { staticOptions, useOptions } from "@/features/workspace/data";
import { dateOnly } from "@/features/workspace/format";
import type { FieldSpec } from "@/features/workspace/spec";
import { emptyValue } from "@/lib/values";
import { resolveField, validate, visibleIn } from "@/features/workspace/fieldRules";

export { emptyValue, resolveField, validate, visibleIn };

type Props = { field: FieldSpec; value: unknown; onChange: (value: unknown) => void; error?: string; disabled?: boolean; compact?: boolean };

export function FieldInput({ field, value, onChange, error, disabled, compact }: Props) {
  const t = useText();
  const label = compact ? undefined : t(field.label);
  const fixed = staticOptions(field.source);
  const remote = useOptions(fixed ? undefined : field.source);
  const readOnly = disabled || field.readOnly;
  const dense = compact ? "h-9 rounded-lg" : undefined;

  switch (field.kind) {
    case "select":
      return (
        <Combobox
          label={label}
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
    case "image":
      return <ImageInput label={label} value={value} onChange={onChange} disabled={readOnly} />;
    case "switch":
      return <Switch label={t(field.label)} checked={!!value} onChange={(checked) => onChange(checked)} />;
    case "textarea":
      return <TextArea label={label} value={(value as string) ?? ""} onChange={(e) => onChange(e.target.value)} maxLength={field.max} error={error} disabled={readOnly} />;
    case "number":
      return (
        <TextField
          label={label}
          type="number"
          step="any"
          min={field.min}
          value={value === null || value === undefined ? "" : String(value)}
          onChange={(e) => onChange(e.target.value === "" ? null : Number(e.target.value))}
          required={field.required}
          error={error}
          disabled={readOnly}
          className={cn("tabular-nums", dense)}
        />
      );
    case "date":
      return <TextField label={label} type="date" value={dateOnly(value)} onChange={(e) => onChange(e.target.value || null)} required={field.required} error={error} disabled={readOnly} className={dense} />;
    case "time":
      return <TextField label={label} type="time" value={(value as string) ?? ""} onChange={(e) => onChange(e.target.value)} error={error} disabled={readOnly} className={dense} />;
    case "password":
      return <TextField label={label} type="password" autoComplete="new-password" value={(value as string) ?? ""} onChange={(e) => onChange(e.target.value)} required={field.required} error={error} disabled={readOnly} />;
    default:
      return <TextField label={label} value={(value as string) ?? ""} onChange={(e) => onChange(e.target.value)} maxLength={field.max} required={field.required} error={error} disabled={readOnly} className={dense} />;
  }
}
