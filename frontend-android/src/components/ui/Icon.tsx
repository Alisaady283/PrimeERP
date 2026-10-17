import type { LucideIcon } from "lucide-react-native";
import { useCSSVariable } from "uniwind";
import { toHex } from "@/lib/color";

/** أيقونة lucide — الأسماء نفسها في الويب — ولونها رمزٌ من tokens.css */
export function Icon({ icon: Glyph, size = 18, tone = "ink-soft", strokeWidth = 2 }: { icon: LucideIcon; size?: number; tone?: string; strokeWidth?: number }) {
  const color = useCSSVariable(`--color-${tone}`);
  return <Glyph size={size} color={toHex(String(color ?? "#000000"))} strokeWidth={strokeWidth} />;
}
