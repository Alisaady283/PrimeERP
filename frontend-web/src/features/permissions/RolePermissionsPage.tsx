import { Check } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { toast } from "@/components/ui/Toast";
import { messageOf } from "@/lib/api";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";
import { PermissionScreen } from "@/features/permissions/PermissionTree";
import { useRolePermissions } from "@/features/permissions/permissions";
import type { CustomSpec } from "@/features/workspace/spec";

function Box({ on, onClick }: { on: boolean; onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className={cn("grid size-5 place-items-center rounded-md border transition-colors", on ? "border-brand bg-brand text-on-field" : "border-line-strong bg-card")}>
      {on && <Check className="size-3.5" />}
    </button>
  );
}

export default function RolePermissionsPage(_props: { spec: CustomSpec }) {
  const t = useText();
  const [roleId, setRoleId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const { granted, toggle, allOf, toggleGroup, ...role } = useRolePermissions(roleId);

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
      actions={roleId && <Button onClick={save} loading={saving}>{t("Str.Save")}</Button>}
      renderKey={(key) => <Box on={granted.has(key)} onClick={() => toggle([key], !granted.has(key))} />}
      renderGroup={(group) => <Box on={allOf(group)} onClick={() => toggleGroup(group)} />}
    />
  );
}
