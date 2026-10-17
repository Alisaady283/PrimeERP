import { useState } from "react";
import { stateItems, useUserPermissions } from "@/features/permissions/permissions";
import type { CustomSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { Button } from "~/components/ui/Button";
import { Segmented } from "~/components/ui/SearchBox";
import { toast } from "~/components/ui/Snackbar";
import { PermissionScreen } from "~/features/permissions/PermissionTree";

/** نظير UserPermissionsPage في الويب: موروثة · ممنوحة · مسحوبة لكل مفتاح */
export default function UserPermissionsPage(_props: { spec: CustomSpec }) {
  const t = useText();
  const [userId, setUserId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const user = useUserPermissions(userId);

  const save = async () => {
    setSaving(true);
    try {
      await user.save();
      toast.success(t("Str.Success"));
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  return (
    <PermissionScreen
      sourceLabel="Str.User"
      sourcePage="Users"
      sourceId={userId}
      onSource={setUserId}
      stacked
      footer={
        <>
          <Button size="lg" variant="secondary" className="flex-1" onPress={user.resetInherited}>
            {t("Str.Permissions.ResetInherited")}
          </Button>
          <Button size="lg" className="flex-1" loading={saving} onPress={() => void save()}>
            {t("Str.Save")}
          </Button>
        </>
      }
      renderKey={(key) => {
        const p = user.states[key];
        return p ? <Segmented items={stateItems(t, p.fromRole)} value={p.state} onChange={(state) => user.set(key, state)} /> : null;
      }}
    />
  );
}
