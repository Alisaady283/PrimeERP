import { Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/Button";
import { confirm } from "@/components/ui/Confirm";
import { Dialog } from "@/components/ui/Dialog";
import { toast } from "@/components/ui/Toast";
import { useCan, usePagePermission } from "@/features/auth/session";
import { RecordActions } from "@/features/workspace/RecordActions";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { useDeletePage, usePageRecord, useSavePage, type Row } from "@/features/workspace/data";
import { FieldInput } from "@/features/workspace/FieldInput";
import { initialValues, resolveField, saveBody, validate, visibleIn } from "@/features/workspace/fieldRules";
import type { ListSpec } from "@/features/workspace/spec";

type Props = { spec: ListSpec; id: number | null; row?: Row; open: boolean; onClose: () => void };

export function EntityDialog({ spec, id, row, open, onClose }: Props) {
  const t = useText();
  const fields = (spec.form ?? []).filter((f) => !(id && f.createOnly));
  const record = usePageRecord(spec.key, open ? id : null);
  const save = useSavePage(spec.key);
  const remove = useDeletePage(spec.key);
  const canDelete = useCan(`${usePagePermission(spec)}.Delete`);
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});

  const source = record.data ?? row;

  useEffect(() => {
    if (!open) return;
    setErrors({});
    setValues(initialValues(fields, id, source));
  }, [open, id, source]);

  const submit = () => {
    const found = validate(fields.filter((f) => visibleIn(f, values)), values, t);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    save.mutate(
      { id, body: saveBody(id, source, values) },
      {
        onSuccess: () => {
          toast.success(t("Str.Success"));
          onClose();
        },
      },
    );
  };

  const titles = spec.formTitles ?? ["Str.Add", "Str.Edit"];
  const error = save.error ?? remove.error;

  return (
    <Dialog
      open={open}
      title={t(id ? titles[1] : titles[0])}
      onClose={onClose}
      footer={
        <>
          {id !== null && canDelete && (
            <Button
              variant="ghost"
              className="me-auto text-danger hover:bg-danger-soft hover:text-danger"
              icon={<Trash2 className="size-4" />}
              loading={remove.isPending}
              onClick={async () => {
                if (await confirm(t("Str.ConfirmDeleteMessage"))) remove.mutate(id, { onSuccess: () => { toast.success(t("Str.Success")); onClose(); } });
              }}
            >
              {t("Str.Delete")}
            </Button>
          )}
          <RecordActions spec={spec} id={id} />
          <Button variant="secondary" onClick={onClose}>
            {t("Str.Cancel")}
          </Button>
          <Button onClick={submit} loading={save.isPending}>
            {t("Str.Save")}
          </Button>
        </>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2">
        {fields.filter((f) => visibleIn(f, values)).map((field) => (
          <div key={field.key} className={field.span === 2 || field.kind === "textarea" ? "sm:col-span-2" : undefined}>
            <FieldInput field={resolveField(field, values)} value={values[field.key]} error={errors[field.key]} disabled={!!id && field.lockOnEdit} onChange={(v) => setValues((s) => ({ ...s, [field.key]: v }))} />
          </div>
        ))}
        {error && (
          <p role="alert" className="rounded-xl bg-danger-soft px-4 py-3 text-sm text-danger sm:col-span-2">
            {messageOf(error)}
          </p>
        )}
      </div>
    </Dialog>
  );
}
