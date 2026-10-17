import { useState } from "react";
import { View } from "react-native";
import { lookupLists } from "@/catalog/pages";
import { useText } from "@/lib/i18n";
import { Tabs } from "~/components/ui/Tabs";
import { EntityPage } from "~/features/workspace/Pages";

/** نظير HrListsPage في الويب: تبويبٌ لكل قائمة، وتحته صفحتها */
export default function HrListsPage() {
  const t = useText();
  const [active, setActive] = useState(lookupLists[0]!.key);
  const spec = lookupLists.find((l) => l.key === active)!;
  return (
    <View className="flex-1 bg-card">
      <Tabs items={lookupLists.map((l) => ({ key: l.key, label: t(l.title) }))} value={active} onChange={setActive} />
      <EntityPage key={spec.key} spec={spec} />
    </View>
  );
}
