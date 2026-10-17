import { Text, View } from "react-native";

/** نظير EmptyState في الويب */
export function EmptyState({ message }: { message: string }) {
  return (
    <View className="items-center justify-center gap-3 py-20">
      <View className="size-14 items-center justify-center rounded-2xl bg-paper">
        <Text className="text-2xl text-ink-faint">∅</Text>
      </View>
      <Text className="font-sans text-sm text-ink-faint">{message}</Text>
    </View>
  );
}
