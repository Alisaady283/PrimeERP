/** مقاس الصورة المحفوظة وجودة JPEG — مشتركان بين الويب والأندرويد */
export const IMAGE_SIZE = 256;
export const IMAGE_QUALITY = 0.85;

/** صورةٌ بمقاس العرض */
export async function encodeImage(file: File, max = IMAGE_SIZE): Promise<string> {
  const bitmap = await createImageBitmap(file);
  const scale = Math.min(1, max / Math.max(bitmap.width, bitmap.height));
  const canvas = document.createElement("canvas");
  canvas.width = Math.max(1, Math.round(bitmap.width * scale));
  canvas.height = Math.max(1, Math.round(bitmap.height * scale));
  const context = canvas.getContext("2d")!;
  context.fillStyle = "#fff";
  context.fillRect(0, 0, canvas.width, canvas.height);
  context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  bitmap.close();
  return canvas.toDataURL("image/jpeg", IMAGE_QUALITY).split(",")[1] ?? "";
}

const typeOf = (data: string) => (data.startsWith("/9j/") ? "image/jpeg" : data.startsWith("R0lG") ? "image/gif" : data.startsWith("Qk") ? "image/bmp" : "image/png");

/** عنوان الصورة المحفوظة */
export const imageUrl = (data: unknown) => (typeof data === "string" && data ? `data:${typeOf(data)};base64,${data}` : undefined);
