import { loadFont as loadGeist } from "@remotion/google-fonts/Geist";
import { loadFont as loadGeistMono } from "@remotion/google-fonts/GeistMono";
import { loadFont as loadNotoSC } from "@remotion/google-fonts/NotoSansSC";

export const geist = loadGeist("normal", {
  weights: ["400", "500", "600", "700", "800", "900"],
  subsets: ["latin"],
});

export const geistMono = loadGeistMono("normal", {
  weights: ["400", "500", "600", "700"],
  subsets: ["latin"],
});

export const notoSC = loadNotoSC("normal", {
  weights: ["400", "500", "700", "900"],
  subsets: ["chinese-simplified", "latin"],
});
