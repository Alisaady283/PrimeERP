import type { ComponentType } from "react";
import type { CustomSpec } from "@/features/workspace/spec";
import AccountsPage from "~/features/accounts/AccountsPage";
import CategoriesPage from "~/features/categories/CategoriesPage";
import ChequesPage from "~/features/cheques/ChequesPage";
import HrListsPage from "~/features/hr/HrListsPage";
import PosPage from "~/features/pos/PosPage";
import RolePermissionsPage from "~/features/permissions/RolePermissionsPage";
import UserPermissionsPage from "~/features/permissions/UserPermissionsPage";
import SettingsPage from "~/features/settings/SettingsPage";

type Screen = ComponentType<{ spec: CustomSpec }>;

/** نظير screens في الويب: الشاشات الخاصة بأسمائها في الدليل — ما لم يُنقل بعد يعرض «متاحة على الويب» */
export const screens: Record<string, Screen> = {
  Accounts: AccountsPage,
  Pos: PosPage,
  Categories: CategoriesPage,
  Cheques: ChequesPage,
  HrLists: HrListsPage,
  Settings: SettingsPage,
  RolePermissions: RolePermissionsPage,
  UserPermissions: UserPermissionsPage,
};
