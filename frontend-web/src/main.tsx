import "@fontsource/ibm-plex-sans-arabic/400.css";
import "@fontsource/ibm-plex-sans-arabic/500.css";
import "@fontsource/ibm-plex-sans-arabic/600.css";
import "@fontsource/ibm-plex-sans-arabic/700.css";
import "@/styles/app.css";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/app/App";

const root = document.getElementById("root");
if (!root) throw new Error("Root element #root is missing.");

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
