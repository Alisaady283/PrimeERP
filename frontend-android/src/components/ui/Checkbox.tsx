import { Check, Minus } from "lucide-react-native";
import { Pressable } from "react-native";
import { Icon } from "~/components/ui/Icon";

/** خانة تأشير كخانة الويب: فارغة · كاملة · جزئية؛ وهدف اللمس أوسع منها */
export function Checkbox({ state, onPress }: { state: "none" | "all" | "some" | boolean; onPress: () => void }) {
  const on = state === true || state === "all" || state === "some";
  return (
    <Pressable hitSlop={10} onPress={onPress} className={`size-6 items-center justify-center rounded-md border ${on ? "border-brand bg-brand" : "border-line-strong bg-card"}`}>
      {state === "some" ? <Icon icon={Minus} size={14} tone="on-field" /> : on ? <Icon icon={Check} size={14} tone="on-field" /> : null}
    </Pressable>
  );
}
