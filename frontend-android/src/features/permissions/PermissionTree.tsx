import { ChevronDown } from "lucide-react-native";
import { useState, type ReactNode } from "react";
import { FlatList, Pressable, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { usePermissionGroups, type PermissionNode } from "@/features/permissions/permissions";
import { useOptions } from "@/features/workspace/data";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Card } from "~/components/ui/Card";
import { Combobox } from "~/components/ui/Combobox";
import { EmptyState } from "~/components/ui/EmptyState";
import { Icon } from "~/components/ui/Icon";
import { SearchBox } from "~/components/ui/SearchBox";
import { Skeleton } from "~/components/ui/Skeleton";

type Props = {
  sourceLabel: string;
  sourcePage: string;
  sourceId: number | null;
  onSource: (id: number | null) => void;
  /** تحت القائمة ثابتةً: الحفظ وما معه */
  footer?: ReactNode;
  renderKey: (key: string) => ReactNode;
  renderGroup?: (group: PermissionNode) => ReactNode;
  /** المفتاح وتحكّمه في سطرين (اختيار الحال) أو سطرٍ واحد (التأشير) */
  stacked?: boolean;
};

/** نظير PermissionScreen في الويب: المصدر والبحث ثم المجموعات تُطوى — والمنطق من `permissions.ts` المشترك */
export function PermissionScreen({ sourceLabel, sourcePage, sourceId, onSource, footer, renderKey, renderGroup, stacked }: Props) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const [search, setSearch] = useState("");
  const [open, setOpen] = useState<Record<string, boolean>>({});
  const sources = useOptions({ page: sourcePage });
  const tree = usePermissionGroups(search);

  return (
    <View className="flex-1 bg-paper">
      <View className="gap-2 border-b border-line bg-card px-3 py-2.5">
        <Combobox placeholder={t(sourceLabel)} value={sourceId} options={sources.data ?? []} onChange={(v) => onSource(v === null ? null : Number(v))} />
        {sourceId ? (
          <View className="flex-row">
            <SearchBox value={search} onChange={setSearch} />
          </View>
        ) : null}
      </View>
      {!sourceId ? (
        <EmptyState message={t(sourceLabel)} />
      ) : tree.loading ? (
        <View className="p-3">
          <Skeleton className="h-96" />
        </View>
      ) : (
        <FlatList
          data={tree.groups}
          keyExtractor={(g) => g.key}
          contentContainerStyle={{ padding: 12, gap: 8, paddingBottom: footer ? 16 : insets.bottom + 24 }}
          renderItem={({ item: group }) => {
            const expanded = tree.searching || !!open[group.key];
            return (
              <Card className="overflow-hidden">
                <View className="flex-row items-center gap-3 px-3 py-3">
                  <Pressable onPress={() => setOpen((o) => ({ ...o, [group.key]: !expanded }))} className="flex-1 flex-row items-center gap-2">
                    <View style={{ transform: [{ rotate: expanded ? "0deg" : start === "text-right" ? "90deg" : "-90deg" }] }}>
                      <Icon icon={ChevronDown} size={16} tone="ink-faint" />
                    </View>
                    <Text className={`flex-1 font-sans text-sm font-semibold text-ink ${start}`}>{group.label}</Text>
                  </Pressable>
                  {renderGroup?.(group)}
                </View>
                {expanded ? (
                  <View className="border-t border-line px-1 py-1">
                    {group.children.map((child) => (
                      <View key={child.key} className={`gap-2 rounded-lg px-2.5 py-2 ${stacked ? "" : "flex-row items-center justify-between"}`}>
                        <Text className={`font-sans text-[13px] text-ink-soft ${stacked ? start : "flex-1"}`}>{child.label}</Text>
                        {renderKey(child.key)}
                      </View>
                    ))}
                  </View>
                ) : null}
              </Card>
            );
          }}
        />
      )}
      {sourceId && footer ? (
        <View className="flex-row gap-2 border-t border-line bg-card px-4 pt-3" style={{ paddingBottom: insets.bottom + 12 }}>
          {footer}
        </View>
      ) : null}
    </View>
  );
}
