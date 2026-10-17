import { Badge } from "@/components/ui/Badge";
import { formatCount, formatMoney } from "@/lib/format";
import { text } from "@/lib/i18n";
import type { ValueKind } from "@/features/workspace/spec";
import { dateOnly, plainValue, toneOf } from "@/lib/values";

export { dateOnly, plainValue };

export function Value({ value, kind = "text", variant }: { value: unknown; kind?: ValueKind; variant?: unknown }) {
  if (value === null || value === undefined || value === "") return <span className="text-ink-faint">—</span>;
  switch (kind) {
    case "money":
      return <span className="tabular-nums">{formatMoney(Number(value))}</span>;
    case "number":
    case "count":
      return <span className="tabular-nums">{formatCount(Number(value))}</span>;
    case "percent":
      return <span className="tabular-nums">{Number(value).toFixed(1)}%</span>;
    case "days":
      return <span className="tabular-nums">{formatCount(Number(value))}</span>;
    case "date":
      return <span className="tabular-nums">{dateOnly(value)}</span>;
    case "code":
      return <span className="font-mono text-ink-soft">{String(value)}</span>;
    case "bool":
      return <Badge tone={value ? "success" : "neutral"}>{text(value ? "Str.Active" : "Str.Inactive")}</Badge>;
    case "status":
      return <Badge tone={toneOf(variant)}>{String(value)}</Badge>;
    default:
      return <>{String(value)}</>;
  }
}

export const alignOf = (kind?: ValueKind) => (kind && ["money", "number", "count", "percent", "days", "date", "bool", "status", "code"].includes(kind) ? "center" : "start") as "center" | "start";
