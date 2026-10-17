import { useState } from "react";
import { useRolePermissions } from "@/features/permissions/permissions";
import type { CustomSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { Button } from "~/components/ui/Button";
import { Checkbox } from "~/components/ui/Checkbox";
import { toast } from "~/components/ui/Snackbar";
import { PermissionScreen } from "~/features/permissions/PermissionTree";

/** نظير RolePermissionsPage في الويب: تأشيرٌ لكل مفتاح ولكل مجموعة */
export default function RolePermissionsPage(_props: { spec: CustomSpec }) {
  const t = useText();
  const [roleId, setRoleId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const role = useRolePermissions(roleId);

  const save = async () => {
    setSaving(true);
    try {
      await role.save();
      toast.success(t("Str.Success"));
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  return (
    <PermissionScreen
      sourceLabel="Str.Role"
      sourcePage="Roles"
      sourceId={roleId}
      onSource={setRoleId}
      footer={
        <Button size="lg" className="flex-1" loading={saving} onPress={() => void save()}>
          {t("Str.Save")}
        </Button>
      }
      renderKey={(key) => <Checkbox state={role.granted.has(key)} onPress={() => role.toggle([key], !role.granted.has(key))} />}
      renderGroup={(group) => <Checkbox state={role.allOf(group)} onPress={() => role.toggleGroup(group)} />}
    />
  );
}
