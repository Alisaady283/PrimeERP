import type { ReactNode } from "react";
import { Text, View } from "react-native";

/** نظير PageHeader في الويب: صفٌّ واحد — الأدوات · العدد · الأزرار */
export function PageHeader({ children, meta, actions, className = "" }: { children?: ReactNode; meta?: ReactNode; actions?: ReactNode; className?: string }) {
  return (
    <View className={`flex-row flex-wrap items-center gap-2 ${className}`}>
      {children}
      {meta !== undefined || actions ? (
        <View className="ms-auto flex-row flex-wrap items-center gap-2">
          {meta !== undefined ? <Text className="px-1 font-sans text-xs text-ink-tertiary">{meta}</Text> : null}
          {actions}
        </View>
      ) : null}
    </View>
  );
}
