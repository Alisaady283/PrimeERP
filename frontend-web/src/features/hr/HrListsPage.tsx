import { useState } from "react";
import { Tabs } from "@/components/ui/Tabs";
import { lookupLists } from "@/catalog/pages";
import { useText } from "@/lib/i18n";
import { EntityPage } from "@/features/workspace/Pages";

export default function HrListsPage() {
  const t = useText();
  const [active, setActive] = useState(lookupLists[0]!.key);
  const spec = lookupLists.find((l) => l.key === active)!;
  return (
    <div className="flex h-full flex-col gap-4">
      <Tabs items={lookupLists.map((l) => ({ key: l.key, label: t(l.title) }))} value={active} onChange={setActive} />
      <div className="min-h-0 flex-1">
        <EntityPage key={spec.key} spec={spec} />
      </div>
    </div>
  );
}
