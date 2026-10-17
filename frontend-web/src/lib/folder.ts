import { download } from "@/lib/platform";
import { readZip } from "@/lib/zip";

type Picker = { showDirectoryPicker?: (options?: { mode?: "readwrite" }) => Promise<FileSystemDirectoryHandle> };

/** اختيار مجلد */
export const pickFolder = async (): Promise<FileSystemDirectoryHandle | null> => {
  const picker = (window as unknown as Picker).showDirectoryPicker;
  if (!picker) return null;
  try {
    return await picker({ mode: "readwrite" });
  } catch {
    return null;
  }
};

export const canPickFolder = () => !!(window as unknown as Picker).showDirectoryPicker;

/** الملف المضغوط في المجلد */
export async function unpackInto(folder: FileSystemDirectoryHandle | null, zip: ArrayBuffer, name: string, progress: (percent: number) => void) {
  if (!folder) {
    download(new Blob([zip], { type: "application/zip" }), name);
    progress(100);
    return;
  }
  const entries = await readZip(zip);
  for (const [i, entry] of entries.entries()) {
    const parts = entry.name.split("/");
    const name = parts.pop() ?? entry.name;
    let dir = folder;
    for (const part of parts) dir = await dir.getDirectoryHandle(part, { create: true });
    const file = await dir.getFileHandle(name, { create: true });
    const writable = await file.createWritable();
    await writable.write(entry.data as BlobPart);
    await writable.close();
    progress(Math.round(((i + 1) * 100) / entries.length));
  }
}
