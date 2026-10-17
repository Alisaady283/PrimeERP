import type { FieldSpec } from "@/features/workspace/spec";
import { api } from "@/lib/api";

/** تغيير كلمة المرور — الحقول والطلب مشتركان بين الويب والأندرويد */
export const passwordFields: FieldSpec[] = [
  { key: "currentPassword", label: "Str.User.CurrentPassword", kind: "password", required: true },
  { key: "newPassword", label: "Str.User.NewPassword", kind: "password", required: true },
  { key: "confirmPassword", label: "Str.User.ConfirmPassword", kind: "password", required: true },
];

export const changePassword = (values: Record<string, unknown>) => api.put("/auth/password", values);
