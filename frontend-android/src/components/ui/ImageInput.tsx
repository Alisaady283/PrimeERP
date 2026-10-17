import { ImageManipulator, SaveFormat } from "expo-image-manipulator";
import * as ImagePicker from "expo-image-picker";
import { Camera, ImageIcon, Images, Trash2 } from "lucide-react-native";
import { useState } from "react";
import { Image, Text, View } from "react-native";
import { useText } from "@/lib/i18n";
import { IMAGE_QUALITY, IMAGE_SIZE, imageUrl } from "@/lib/image";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Icon } from "~/components/ui/Icon";

/** نظير ImageInput في الويب: الكاميرا أو المعرض بدل اختيار ملف، مقصوصةً مربّعاً ثم مصغَّرةً بمقاس الويب وجودته (JPEG) */
export function ImageInput({ label, value, onChange, disabled }: { label?: string; value: unknown; onChange: (value: string) => void; disabled?: boolean }) {
  const t = useText();
  const start = useStart();
  const [error, setError] = useState<string | null>(null);
  const url = imageUrl(value);

  const pick = async (camera: boolean) => {
    const options: ImagePicker.ImagePickerOptions = { mediaTypes: ["images"], allowsEditing: true, aspect: [1, 1], quality: 1 };
    if (camera && !(await ImagePicker.requestCameraPermissionsAsync()).granted) return;
    const result = camera ? await ImagePicker.launchCameraAsync(options) : await ImagePicker.launchImageLibraryAsync(options);
    const asset = result.canceled ? undefined : result.assets[0];
    if (!asset) return;
    try {
      const wide = asset.width >= asset.height;
      const context = ImageManipulator.manipulate(asset.uri);
      if (Math.max(asset.width, asset.height) > IMAGE_SIZE) context.resize(wide ? { width: IMAGE_SIZE } : { height: IMAGE_SIZE });
      const saved = await (await context.renderAsync()).saveAsync({ format: SaveFormat.JPEG, compress: IMAGE_QUALITY, base64: true });
      onChange(saved.base64 ?? "");
      setError(null);
    } catch {
      setError(t("Str.Image.ReadFailed", asset.fileName ?? ""));
    }
  };

  return (
    <View className="gap-1.5">
      {label ? <Text className={`font-sans text-xs font-medium text-ink-soft ${start}`}>{label}</Text> : null}
      <View className="flex-row items-center gap-4 rounded-xl border border-dashed border-line-strong bg-paper-soft p-3">
        <View className="size-20 items-center justify-center overflow-hidden rounded-lg border border-line bg-card">
          {url ? <Image source={{ uri: url }} className="size-full" resizeMode="cover" /> : <Icon icon={ImageIcon} size={24} tone="ink-faint" />}
        </View>
        <View className="flex-1 gap-2">
          <Button size="sm" variant="secondary" disabled={disabled} icon={<Icon icon={Camera} size={16} tone="ink-soft" />} onPress={() => void pick(true)}>
            {t("Str.Image.Camera")}
          </Button>
          <Button size="sm" variant="secondary" disabled={disabled} icon={<Icon icon={Images} size={16} tone="ink-soft" />} onPress={() => void pick(false)}>
            {t("Str.Image.Pick")}
          </Button>
          {url && !disabled ? (
            <Button size="sm" variant="ghost" textClassName="text-danger" icon={<Icon icon={Trash2} size={16} tone="danger" />} onPress={() => onChange("")}>
              {t("Str.Image.Remove")}
            </Button>
          ) : null}
        </View>
      </View>
      {error ? <Text className={`font-sans text-xs text-danger ${start}`}>{error}</Text> : null}
    </View>
  );
}
