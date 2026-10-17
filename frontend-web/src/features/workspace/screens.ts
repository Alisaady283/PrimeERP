import { lazy, type ComponentType } from "react";
import type { CustomSpec } from "@/features/workspace/spec";

type Screen = ComponentType<{ spec: CustomSpec }>;

/** شاشات الويب الخاصة بأسمائها في الدليل */
export const screens: Record<string, Screen> = {
  Accounts: lazy(() => import("@/features/accounts/AccountsPage")),
  Pos: lazy(() => import("@/features/pos/PosPage")) as Screen,
  Categories: lazy(() => import("@/features/categories/CategoriesPage")),
  Cheques: lazy(() => import("@/features/cheques/ChequesPage")) as Screen,
  HrLists: lazy(() => import("@/features/hr/HrListsPage")) as Screen,
  Settings: lazy(() => import("@/features/settings/SettingsPage")),
  RolePermissions: lazy(() => import("@/features/permissions/RolePermissionsPage")),
  UserPermissions: lazy(() => import("@/features/permissions/UserPermissionsPage")),
  BuilderExport: lazy(() => import("@/features/builder/BuilderExportPage")) as Screen,
};
