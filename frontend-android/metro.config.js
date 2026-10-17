// Metro: الدليل والنصوص من الويب والخادم بلا نسخ، وحزمها من هنا وحده
const path = require("node:path");
const { getDefaultConfig } = require("expo/metro-config");
const { withUniwindConfig } = require("uniwind/metro");

const root = __dirname;
const own = path.join(root, "src");
const web = path.resolve(root, "../frontend-web/src");
const strings = path.resolve(root, "../backend/1.Platform/Localization/Strings");

const config = getDefaultConfig(root);
config.watchFolders = [web, strings];

/** ما يختلف بين العملاء: ملف الأندرويد بدل ملف الويب */
const aliases = (name) =>
  name === "@/lib/platform" ? path.join(own, "lib/platform.ts")
  : name.startsWith("~/") ? path.join(own, name.slice(2))
  : name.startsWith("@strings/") ? path.join(strings, name.slice("@strings/".length))
  : name.startsWith("@/") ? path.join(web, name.slice(2))
  : null;

const outside = (file) => file.startsWith(web) || file.startsWith(strings);

config.resolver.resolveRequest = (context, name, platform) => {
  const mapped = aliases(name);
  if (mapped) return context.resolveRequest(context, mapped, platform);
  // حزم ملفات الويب من node_modules هنا: نسخةٌ واحدة من React
  if (outside(context.originModulePath) && !name.startsWith(".")) return context.resolveRequest({ ...context, originModulePath: path.join(root, "package.json") }, name, platform);
  return context.resolveRequest(context, name, platform);
};

// Uniwind الغلاف الخارجي كما توصي وثائقه، ورموزه من tokens.css المشترك
module.exports = withUniwindConfig(config, { cssEntryFile: "./src/global.css", dtsFile: "./src/uniwind-types.d.ts" });
