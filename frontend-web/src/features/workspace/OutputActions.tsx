import { Printer, Upload } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Menu } from "@/components/ui/Menu";
import { toast } from "@/components/ui/Toast";
import { useInterface } from "@/features/auth/interface";
import { useText } from "@/lib/i18n";
import { exportCsv, exportExcel, printPaper, type OutputTable } from "@/lib/output";

/** طباعة وتصدير */
export function OutputActions({ title, table }: { title: string; table: () => OutputTable }) {
  const t = useText();
  const settings = useInterface();
  const empty = () => table().rows.length === 0;
  const guard = (run: () => void) => () => (empty() ? toast.info(t("Str.Output.NoPrintData")) : run());
  const paper = () => ({ title, company: settings?.companyName, table: table() });

  return (
    <>
      <Button variant="secondary" icon={<Printer className="size-4" />} onClick={guard(() => printPaper(paper()))}>
        {t("Str.Print")}
      </Button>
      <Menu
        icon={<Upload className="size-4" />}
        label={t("Str.Action.Export")}
        items={[
          { label: "Excel (.xlsx)", onSelect: guard(() => exportExcel(title, table())) },
          { label: "CSV (.csv)", onSelect: guard(() => exportCsv(title, table())) },
          { label: "PDF", onSelect: guard(() => printPaper(paper())) },
        ]}
      />
    </>
  );
}
