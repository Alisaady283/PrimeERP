import { Download, Printer, Trash2 } from "lucide-react";
import { useQueries } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { RecordActions } from "@/features/workspace/RecordActions";
import { Button } from "@/components/ui/Button";
import { confirm } from "@/components/ui/Confirm";
import { Dialog } from "@/components/ui/Dialog";
import { LinesGrid, type LineColumn } from "@/components/ui/LinesGrid";
import { Menu } from "@/components/ui/Menu";
import { toast } from "@/components/ui/Toast";
import { useInterface } from "@/features/auth/interface";
import { useCan, usePagePermission, useSession } from "@/features/auth/session";
import { printPaper } from "@/lib/output";
import { documentPaper } from "@/features/workspace/paper";
import { PullDialog } from "@/features/workspace/PullDialog";
import { ApiError, messageOf } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { optionsQuery, staticOptions, useDeletePage, usePageRecord, useSavePage } from "@/features/workspace/data";
import { FieldInput, resolveField, validate } from "@/features/workspace/FieldInput";
import { blankLine, documentBody, documentTotal, mergePulled, openSheet, pick, pullSources as sourcesOf, recordLines, tradeNet, type Line } from "@/features/workspace/documentRules";
import type { DocumentSpec, PullSpec } from "@/features/workspace/spec";

type Props = { spec: DocumentSpec; id: number | null; open: boolean; onClose: () => void };


function useLineColumns(spec: DocumentSpec): LineColumn<Line>[] {
  const t = useText();
  const fields = spec.lines?.fields ?? [];
  const options = fields.map((f) => staticOptions(f.source));
  const remote = useQueries({ queries: fields.map((f) => optionsQuery(f.source)) });
  const loaded = remote.map((r) => r.data);
  return useMemo(() => {
    const columns: LineColumn<Line>[] = fields.map((f, i) => ({
      key: f.key,
      label: t(f.label),
      width: f.width ?? (f.kind === "select" ? "minmax(14rem,2fr)" : f.kind === "number" ? "7rem" : f.kind === "date" ? "9rem" : "minmax(9rem,1fr)"),
      editor: f.kind === "select" ? "select" : f.kind === "number" ? "number" : f.kind === "date" ? "date" : f.readOnly ? undefined : "text",
      options: options[i] ?? loaded[i] ?? [],
      readOnly: f.readOnly,
    }));
    if (spec.lines?.trade) columns.push({ key: "net", label: t("Str.NetTotal"), width: "8rem", readOnly: true, render: (line) => <span className="font-semibold tabular-nums">{formatMoney(tradeNet(line))}</span> });
    return columns;
  }, [fields, t, spec.lines?.trade, ...loaded]);
}

export function DocumentEditor({ spec, id, open, onClose }: Props) {
  const t = useText();
  const record = usePageRecord(spec.key, open ? id : null);
  const save = useSavePage(spec.key, spec.saveTo);
  const remove = useDeletePage(spec.key);
  const canDelete = useCan(`${usePagePermission(spec)}.Delete`);
  const [header, setHeader] = useState<Record<string, unknown>>({});
  const [lines, setLines] = useState<Line[]>([]);
  const [params, setParams] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [opening, setOpening] = useState(false);
  const [pulling, setPulling] = useState<PullSpec | null>(null);
  const columns = useLineColumns(spec);
  const settings = useInterface();
  const permissions = useSession((s) => s.user?.permissions);
  const pullSources = sourcesOf(spec, settings?.simplifiedFlow ?? true, permissions);

  const print = async () => {
    try {
      printPaper(await documentPaper(spec, { id: id ?? 0 }, settings?.companyName));
    } catch (error) {
      toast.error(messageOf(error));
    }
  };

  useEffect(() => {
    if (!open) return;
    setErrors({});
    const source = id ? record.data : undefined;
    setHeader({ ...(source ?? {}), ...pick(source, spec.header) });
    setLines(recordLines(spec, source));
    setParams(pick(undefined, spec.open?.params ?? []));
  }, [open, id, record.data]);

  const newLine = (): Line => blankLine(spec);

  const load = async () => {
    if (!spec.open) return;
    setOpening(true);
    try {
      const sheet = await openSheet(spec, params);
      setHeader(sheet.header);
      setLines(sheet.lines);
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setOpening(false);
    }
  };

  const submit = () => {
    const found = validate(spec.header, header, t);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    save.mutate({ id, body: documentBody(spec, id, header, lines) }, { onSuccess: () => { toast.success(t("Str.Success")); onClose(); } });
  };

  const total = documentTotal(spec, lines);
  const error = save.error ?? remove.error;
  const readOnly = !!id && spec.noUpdate;

  return (
    <Dialog
      open={open}
      size="xl"
      title={t(spec.title)}
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
          {id !== null && (
            <Button variant="secondary" icon={<Printer className="size-4" />} onClick={() => void print()}>
              {t("Str.Print")}
            </Button>
          )}
          {total !== null && (
            <span className="me-4 text-sm text-ink-tertiary">
              {t("Str.NetTotal")}: <b className="text-lg tabular-nums text-ink">{formatMoney(total)}</b>
            </span>
          )}
          <RecordActions spec={spec} id={id} />
          <Button variant="secondary" onClick={onClose}>
            {t("Str.Cancel")}
          </Button>
          {!readOnly && (
            <Button onClick={submit} loading={save.isPending}>
              {t("Str.Save")}
            </Button>
          )}
        </>
      }
    >
      <div className="space-y-5">
        {!id && spec.open && (
          <div className="flex flex-wrap items-end gap-3 rounded-2xl bg-brand-soft p-4">
            {spec.open.params.map((f) => (
              <div key={f.key} className="min-w-48 flex-1">
                <FieldInput field={f} value={params[f.key]} onChange={(v) => setParams((s) => ({ ...s, [f.key]: v }))} />
              </div>
            ))}
            <Button onClick={load} loading={opening}>
              {t("Str.Refresh")}
            </Button>
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {spec.header.map((field) => (
            <div key={field.key} className={field.kind === "textarea" || field.span === 2 ? "sm:col-span-2" : undefined}>
              <FieldInput field={resolveField(field, header)} value={header[field.key]} error={errors[field.key]} disabled={readOnly} onChange={(v) => setHeader((s) => ({ ...s, [field.key]: v }))} />
            </div>
          ))}
        </div>

        {!readOnly && pullSources.length > 0 && (
          <div className="flex justify-end">
            <Menu
              icon={<Download className="size-4" />}
              label={t("Str.Pull.Pull")}
              items={pullSources.map((source) => ({ label: t(`Str.Pull.From.${source.source}`), onSelect: () => setPulling(source) }))}
            />
          </div>
        )}

        {spec.lines && <LinesGrid lines={lines} columns={columns} onChange={setLines} newLine={newLine} readOnly={readOnly} />}

        {error && (
          <p role="alert" className="rounded-xl bg-danger-soft px-4 py-3 text-sm text-danger">
            {error instanceof ApiError ? [error.message, ...error.errors.filter((e) => e !== error.message)].join(" — ") : t("Str.Error")}
          </p>
        )}
      </div>
      <PullDialog
        source={pulling}
        header={header}
        onPull={(pulled) => {
          const merged = mergePulled(spec, lines, header, pulled);
          setLines(merged.lines);
          setHeader(merged.header);
        }}
        onClose={() => setPulling(null)}
      />
    </Dialog>
  );
}
