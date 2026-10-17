import { router } from "expo-router";
import { Trash2 } from "lucide-react-native";
import { useEffect, useState } from "react";
import { Alert, KeyboardAvoidingView, ScrollView, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCan, usePagePermission } from "@/features/auth/session";
import { useDeletePage, usePageRecord, useSavePage } from "@/features/workspace/data";
import { initialValues, resolveField, saveBody, validate, visibleIn } from "@/features/workspace/fieldRules";
import type { ListSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Enter } from "~/components/ui/Enter";
import { Icon } from "~/components/ui/Icon";
import { Skeleton } from "~/components/ui/Skeleton";
import { FieldInput } from "~/features/workspace/FieldInput";
import { useRecordActions } from "~/features/workspace/RecordActions";

/** نظير EntityDialog في الويب — على الهاتف شاشةٌ كاملة، والقواعد مشتركة (fieldRules) */
export function EntityDialog({ spec, id }: { spec: ListSpec; id: number | null }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const fields = (spec.form ?? []).filter((f) => !(id && f.createOnly));
  const record = usePageRecord(spec.key, id);
  const save = useSavePage(spec.key);
  const remove = useDeletePage(spec.key);
  const records = useRecordActions(spec, id);
  const canDelete = useCan(`${usePagePermission(spec)}.Delete`);
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!id || record.data) setValues(initialValues(fields, id, record.data));
  }, [id, record.data]);

  const submit = () => {
    const found = validate(fields.filter((f) => visibleIn(f, values)), values, t);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    save.mutate({ id, body: saveBody(id, record.data, values) }, { onSuccess: () => router.back() });
  };

  const confirmDelete = () =>
    Alert.alert(t("Str.Delete"), t("Str.ConfirmDeleteMessage"), [
      { text: t("Str.Cancel"), style: "cancel" },
      { text: t("Str.Delete"), style: "destructive", onPress: () => id && remove.mutate(id, { onSuccess: () => router.back() }) },
    ]);

  const error = save.error ?? remove.error;

  if (id && record.isLoading)
    return (
      <View className="gap-3 p-4">
        <Skeleton className="h-11" />
        <Skeleton className="h-11" />
        <Skeleton className="h-11" />
      </View>
    );

  return (
    <KeyboardAvoidingView behavior="height" className="flex-1 bg-card">
      <ScrollView contentContainerStyle={{ padding: 16, gap: 16, paddingBottom: insets.bottom + 24 }} keyboardShouldPersistTaps="handled">
        {fields
          .filter((f) => visibleIn(f, values))
          .map((field, i) => (
            <Enter key={field.key} index={i}>
              <FieldInput field={resolveField(field, values)} value={values[field.key]} error={errors[field.key]} disabled={!!id && field.lockOnEdit} onChange={(v) => setValues((s) => ({ ...s, [field.key]: v }))} />
            </Enter>
          ))}
        {error ? (
          <View className="rounded-xl bg-danger-soft px-4 py-3">
            <Text className={`font-sans text-sm text-danger ${start}`}>{messageOf(error)}</Text>
          </View>
        ) : null}
        <View className="gap-2">
          <Button size="lg" loading={save.isPending} onPress={submit}>
            {t("Str.Save")}
          </Button>
          {records.actions.map((a) => (
            <Button key={a.key} size="lg" variant="secondary" onPress={() => void records.run(a)}>
              {records.label(a)}
            </Button>
          ))}
          {id !== null && canDelete ? (
            <Button size="lg" variant="ghost" textClassName="text-danger" icon={<Icon icon={Trash2} size={16} tone="danger" />} loading={remove.isPending} onPress={confirmDelete}>
              {t("Str.Delete")}
            </Button>
          ) : null}
        </View>
      </ScrollView>
      {records.sheet}
    </KeyboardAvoidingView>
  );
}
