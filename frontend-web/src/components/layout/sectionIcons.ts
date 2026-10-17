import { BarChart3, Boxes, Briefcase, Calculator, Factory, Landmark, Settings, ShoppingBag, ShoppingCart, Store, Users, Wallet, Wrench, type LucideIcon } from "lucide-react";
import type { SectionIcon } from "@/catalog/navigation";

/** أيقونات الأقسام في الويب */
export const sectionIcons: Record<SectionIcon, LucideIcon> = {
  calculator: Calculator, "shopping-bag": ShoppingBag, store: Store, "shopping-cart": ShoppingCart, boxes: Boxes, factory: Factory,
  wallet: Wallet, landmark: Landmark, users: Users, "bar-chart": BarChart3, settings: Settings, wrench: Wrench, briefcase: Briefcase,
};
