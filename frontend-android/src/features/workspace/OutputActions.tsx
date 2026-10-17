import { FileSpreadsheet, FileText, Printer, Upload } from "lucide-react-native";
import { useInterface } from "@/features/auth/interface";
import { useText } from "@/lib/i18n";
import { exportCsv, exportExcel, printPaper, type OutputTable } from "@/lib/output";
import { Icon } from "~/components/ui/Icon";
import { Menu } from "~/components/ui/Menu";
import { toast } from "~/components/ui/Snackbar";

/** نظير OutputActions في الويب: المحتوى من `lib/output` المشترك، والطباعة حوار أندرويد والملفات ورقة المشاركة */
export function OutputActions({ title, table }: { title: string; table: () => OutputTable }) {
  const t = useText();
  const settings = useInterface();
  const guard = (run: () => void) => () => (table().rows.length === 0 ? toast.info(t("Str.Output.NoPrintData")) : run());
  return (
    <Menu
      icon={<Icon icon={Upload} size={16} tone="ink-soft" />}
      label={t("Str.Action.Export")}
      items={[
        { label: `${t("Str.Print")} · PDF`, icon: <Icon icon={Printer} size={20} tone="ink-soft" />, onSelect: guard(() => printPaper({ title, company: settings?.companyName, table: table() })) },
        { label: "Excel (.xlsx)", icon: <Icon icon={FileSpreadsheet} size={20} tone="success" />, onSelect: guard(() => exportExcel(title, table())) },
        { label: "CSV (.csv)", icon: <Icon icon={FileText} size={20} tone="ink-soft" />, onSelect: guard(() => exportCsv(title, table())) },
      ]}
    />
  );
}
