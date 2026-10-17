import { router } from "expo-router";
import { Camera, LogOut, Minus, Package, PauseCircle, Plus, RotateCcw, ScanLine, ShoppingCart, Trash2, Undo2, Wallet } from "lucide-react-native";
import { useState } from "react";
import { FlatList, Image, Pressable, ScrollView, Text, TextInput, View } from "react-native";
import Animated, { FadeInRight } from "react-native-reanimated";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCSSVariable } from "uniwind";
import { cartNet, expectedCash, usePos } from "@/features/pos/pos";
import { formatCount, formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { imageUrl } from "@/lib/image";
import { duration } from "@/lib/motion";
import { useStart } from "~/lib/direction";
import { AnimatedNumber } from "~/components/ui/AnimatedNumber";
import { BarcodeScanner } from "~/components/ui/BarcodeScanner";
import { Button } from "~/components/ui/Button";
import { Card } from "~/components/ui/Card";
import { Combobox } from "~/components/ui/Combobox";
import { EmptyState } from "~/components/ui/EmptyState";
import { TextField } from "~/components/ui/Field";
import { Icon } from "~/components/ui/Icon";
import { Sheet } from "~/components/ui/Sheet";
import { Skeleton } from "~/components/ui/Skeleton";
import { toast } from "~/components/ui/Snackbar";
import { TreeChips } from "~/components/ui/TreeNav";

/** نظير PosPage في الويب على الهاتف: الأصناف تملأ الشاشة، والسلّة شريطٌ ثابت أسفلها يفتح ورقتها — والمنطق من `pos.ts` المشترك */
export default function PosPage() {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const faint = useCSSVariable("--color-ink-faint");
  const [cartOpen, setCartOpen] = useState(false);
  const [heldOpen, setHeldOpen] = useState(false);
  const [closing, setClosing] = useState<string | null>(null);
  const [camera, setCamera] = useState(false);
  const pos = usePos(t, toast, () => setCartOpen(false));
  const { shift, cart, hold, pay } = pos;
  const count = cart.reduce((sum, l) => sum + l.qty, 0);

  if (shift.isLoading)
    return (
      <View className="p-3">
        <Skeleton className="h-96" />
      </View>
    );

  if (!shift.data)
    return (
      <ScrollView className="flex-1 bg-paper" contentContainerStyle={{ padding: 16, paddingBottom: insets.bottom + 24 }} keyboardShouldPersistTaps="handled">
        <Card className="gap-4 p-5">
          <View className="flex-row items-center gap-3">
            <View className="size-12 items-center justify-center rounded-2xl bg-brand">
              <Icon icon={Wallet} size={20} tone="on-field" />
            </View>
            <View className="flex-1">
              <Text className={`font-sans text-lg font-bold text-ink ${start}`}>{t("Str.Pos.OpenShift")}</Text>
              <Text className={`font-sans text-sm text-ink-tertiary ${start}`}>{t("Str.Pos.NoShift")}</Text>
            </View>
          </View>
          <TextField label={t("Str.Pos.Opening")} keyboardType="decimal-pad" value={pos.opening.cash} onChangeText={(cash) => pos.setOpening({ ...pos.opening, cash })} />
          <Combobox label={t("Str.Treasury")} value={pos.opening.treasuryId} options={pos.treasuries.data ?? []} onChange={(v) => pos.setOpening({ ...pos.opening, treasuryId: v === null ? null : Number(v) })} />
          <Combobox label={t("Str.Warehouse")} value={pos.opening.warehouseId} options={pos.warehouses.data ?? []} onChange={(v) => pos.setOpening({ ...pos.opening, warehouseId: v === null ? null : Number(v) })} />
          <Button size="lg" onPress={() => void pos.openShift()}>
            {t("Str.Pos.OpenShift")}
          </Button>
        </Card>
      </ScrollView>
    );

  return (
    <View className="flex-1 bg-paper">
      <View className="gap-2 border-b border-line bg-card px-3 py-2.5">
        <View className="flex-row items-center gap-2">
          <View className="min-w-0 flex-1 justify-center">
            <View className="absolute start-3 z-10">
              <Icon icon={ScanLine} size={16} tone="brand" />
            </View>
            <TextInput
              value={pos.scan}
              onChangeText={pos.setScan}
              onSubmitEditing={() => void pos.submitScan()}
              returnKeyType="search"
              placeholder={t("Str.Pos.Scan")}
              placeholderTextColor={String(faint)}
              className={`h-10 w-full rounded-lg border border-line bg-paper-soft ps-9 pe-11 font-sans text-sm text-ink focus:border-brand focus:bg-card ${start}`}
            />
            <Pressable hitSlop={6} accessibilityLabel={t("Str.Scan.Camera")} onPress={() => setCamera(true)} className="absolute end-1 size-8 items-center justify-center rounded-md active:bg-brand-soft">
              <Icon icon={Camera} size={18} tone="brand" />
            </Pressable>
          </View>
          <Button variant="ghost" size="sm" icon={<Icon icon={RotateCcw} size={16} tone="ink-soft" />} onPress={() => router.push("/p/PosReturn/form")}>
            {t("Str.Pos.ReturnTab")}
          </Button>
          <Button variant="ghost" size="sm" icon={<Icon icon={LogOut} size={16} tone="ink-soft" />} onPress={() => setClosing("")}>
            {t("Str.Pos.CloseShift")}
          </Button>
        </View>
        <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>{t("Str.Pos.ShiftInfo", shift.data.userName ?? "", new Date(shift.data.openedAt).toLocaleTimeString().slice(0, 5))}</Text>
        {pos.categoryChips.length > 1 ? <TreeChips items={pos.categoryChips} value={pos.categoryId} onChange={pos.setCategoryId} /> : null}
      </View>

      <FlatList
        data={pos.products.data?.items ?? []}
        numColumns={2}
        keyExtractor={(p) => String(p.id)}
        columnWrapperStyle={{ gap: 12 }}
        contentContainerStyle={{ padding: 12, gap: 12, paddingBottom: 96 }}
        keyboardShouldPersistTaps="handled"
        ListEmptyComponent={pos.products.isLoading ? <Skeleton className="h-64" /> : <EmptyState message={t("Str.NoData")} />}
        renderItem={({ item: p }) => {
          const image = imageUrl(p.imageData);
          return (
            <Pressable onPress={() => pos.addProduct(p)} className="flex-1 overflow-hidden rounded-xl border border-line bg-card active:scale-[0.97]">
              <View className="aspect-[4/3] w-full items-center justify-center bg-paper">
                {image ? <Image source={{ uri: image }} className="size-full" resizeMode="cover" /> : <Icon icon={Package} size={28} tone="ink-faint" />}
              </View>
              <View className="flex-1 gap-1 px-3 py-2">
                <Text numberOfLines={2} className={`font-sans text-[13px] font-medium text-ink ${start}`}>{p.name}</Text>
                <Text className={`mt-auto font-sans text-sm font-bold text-brand ${start}`} style={{ fontVariant: ["tabular-nums"] }}>{formatMoney(p.salePrice)}</Text>
              </View>
            </Pressable>
          );
        }}
      />

      <Pressable
        onPress={() => setCartOpen(true)}
        className="absolute inset-x-3 flex-row items-center gap-3 rounded-2xl bg-brand px-4 py-3 shadow-pop active:opacity-90"
        style={{ bottom: insets.bottom + 12 }}
      >
        <View>
          <Icon icon={ShoppingCart} size={22} tone="on-field" />
          {count > 0 ? (
            <View className="absolute -end-2 -top-2 min-w-5 items-center rounded-full bg-chart-2 px-1">
              <Text className="font-sans text-[11px] font-bold text-white">{formatCount(count)}</Text>
            </View>
          ) : null}
        </View>
        <Text className={`flex-1 font-sans text-sm font-medium text-on-field ${start}`}>{t("Str.NetTotal")}</Text>
        <AnimatedNumber className="font-sans text-lg font-bold text-on-field" value={pos.total} format={formatMoney} />
      </Pressable>

      <Sheet
        open={cartOpen}
        title={t("Str.Pos.Pay")}
        onClose={() => setCartOpen(false)}
        footer={
          <>
            <View className="flex-row items-center justify-between">
              <Text className="font-sans text-sm text-ink-tertiary">{t("Str.NetTotal")}</Text>
              <AnimatedNumber className="font-sans text-2xl font-bold text-ink" value={pos.total} format={formatMoney} />
            </View>
            <TextField keyboardType="decimal-pad" placeholder={t("Str.Pos.Paid")} value={pos.paid} onChangeText={pos.setPaid} />
            {pos.change !== null ? <Text className={`font-sans text-sm text-success ${start}`}>{formatMoney(pos.change)}</Text> : null}
            <View className="flex-row gap-2">
              <Button variant="secondary" className="flex-1" icon={<Icon icon={PauseCircle} size={16} tone="ink-soft" />} disabled={!cart.length} loading={hold.isPending} onPress={() => hold.mutate()}>
                {t("Str.Pos.Hold")}
              </Button>
              <Button variant="secondary" className="flex-1" icon={<Icon icon={Undo2} size={16} tone="ink-soft" />} onPress={() => setHeldOpen(true)}>
                {t("Str.Pos.Recall", pos.held.data?.length ?? 0)}
              </Button>
            </View>
            <Button size="lg" icon={<Icon icon={Wallet} size={18} tone="on-field" />} disabled={!cart.length} loading={pay.isPending} onPress={() => pay.mutate()}>
              {t("Str.Pos.Pay")}
            </Button>
          </>
        }
      >
        <View className="border-b border-line bg-card px-3 py-2.5">
          <Combobox value={pos.customerId} disabled={pos.customerLocked} clearable={!pos.customerLocked} options={pos.customers.data ?? []} onChange={(v) => pos.setCustomerId(v === null ? null : Number(v))} placeholder={t("Str.Customer")} />
        </View>
        <FlatList
          data={cart}
          keyExtractor={(l, i) => `${l.productCode}-${l.unitPrice}-${i}`}
          ListEmptyComponent={<EmptyState message={t("Str.NoData")} />}
          renderItem={({ item: line, index: i }) => (
            <Animated.View entering={FadeInRight.duration(duration.medium2)} className={`flex-row items-center gap-2 bg-card px-3 py-2.5 ${i > 0 ? "border-t border-line" : ""}`}>
              <View className="min-w-0 flex-1">
                <Text numberOfLines={1} className={`font-sans text-sm font-medium text-ink ${start}`}>{line.name}</Text>
                <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>{formatMoney(line.unitPrice)}</Text>
              </View>
              <View className="flex-row items-center gap-1 rounded-xl bg-paper p-1">
                <Pressable hitSlop={6} onPress={() => pos.step(i, -1)} className="size-8 items-center justify-center rounded-lg active:bg-card">
                  <Icon icon={Minus} size={14} tone="ink" />
                </Pressable>
                <Text className="w-8 text-center font-sans text-sm font-semibold text-ink">{line.qty}</Text>
                <Pressable hitSlop={6} onPress={() => pos.step(i, 1)} className="size-8 items-center justify-center rounded-lg active:bg-card">
                  <Icon icon={Plus} size={14} tone="ink" />
                </Pressable>
              </View>
              <Text className="w-20 text-right font-sans text-sm font-semibold text-ink">{formatMoney(cartNet(line))}</Text>
              <Pressable hitSlop={8} onPress={() => pos.removeLine(i)}>
                <Icon icon={Trash2} size={16} tone="ink-faint" />
              </Pressable>
            </Animated.View>
          )}
        />
      </Sheet>

      <BarcodeScanner open={camera} onClose={() => setCamera(false)} onScan={(code) => void pos.submitScan(code)} />

      <Sheet open={heldOpen} title={t("Str.Pos.HeldTitle")} onClose={() => setHeldOpen(false)}>
        <FlatList
          data={pos.held.data ?? []}
          keyExtractor={(h) => String(h.id)}
          contentContainerStyle={{ padding: 12, gap: 8 }}
          ListEmptyComponent={<EmptyState message={t("Str.NoData")} />}
          renderItem={({ item: h }) => (
            <Card className="flex-row items-center justify-between gap-3 px-4 py-3">
              <Text className={`flex-1 font-sans text-sm text-ink ${start}`}>
                {h.label || `#${h.id}`} <Text className="text-xs text-ink-faint">{new Date(h.heldAt).toLocaleTimeString()}</Text>
              </Text>
              <Button size="sm" onPress={async () => (await pos.take(h.id)) && setHeldOpen(false)}>
                {t("Str.Pos.NewSale")}
              </Button>
            </Card>
          )}
        />
      </Sheet>

      <Sheet
        open={closing !== null}
        title={t("Str.Pos.CloseShift")}
        onClose={() => setClosing(null)}
        footer={
          <Button size="lg" onPress={async () => (await pos.closeShift(closing ?? "")) && setClosing(null)}>
            {t("Str.Pos.CloseShift")}
          </Button>
        }
      >
        <ScrollView contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
          <View className="flex-row gap-2 rounded-xl bg-paper-soft p-4">
            {(
              [
                ["Str.Pos.Opening", shift.data.openingCash],
                ["Str.Pos.CashIn", shift.data.cashIn],
                ["Str.Pos.Expected", expectedCash(shift.data)],
              ] as const
            ).map(([label, value]) => (
              <View key={label} className="flex-1">
                <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>{t(label)}</Text>
                <Text className={`font-sans text-sm font-semibold text-ink ${start}`}>{formatMoney(value)}</Text>
              </View>
            ))}
          </View>
          <TextField label={t("Str.Pos.Counted")} keyboardType="decimal-pad" value={closing ?? ""} onChangeText={setClosing} />
        </ScrollView>
      </Sheet>
    </View>
  );
}
