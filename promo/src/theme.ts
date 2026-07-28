export const FPS = 60;
export const BPM = 120;
export const BEAT = (60 / BPM) * FPS; // 30 frames
export const BAR = BEAT * 4; // 120 frames

// Master timeline (in frames @60fps, all on bar boundaries)
export const T = {
  hook: { from: 0, dur: 8 * FPS },
  brand: { from: 8 * FPS, dur: 6 * FPS },
  crafting: { from: 14 * FPS, dur: 10 * FPS },
  deploy: { from: 24 * FPS, dur: 10 * FPS },
  features: { from: 34 * FPS, dur: 30 * FPS },
  mcp: { from: 64 * FPS, dur: 8 * FPS },
  outro: { from: 72 * FPS, dur: 6 * FPS },
};
export const MASTER_DUR = 78 * FPS;

export const C = {
  bg: "oklch(0.185 0.01 65)",
  card: "oklch(0.225 0.012 65)",
  cardHi: "oklch(0.27 0.014 65)",
  fg: "oklch(0.955 0.008 85)",
  muted: "oklch(0.72 0.02 72)",
  faint: "oklch(0.52 0.02 70)",
  primary: "oklch(0.79 0.14 78)",
  primarySoft: "oklch(0.79 0.14 78 / 0.16)",
  red: "oklch(0.62 0.21 27)",
  green: "#1bd96a",
  orange: "#f16436",
  blue: "oklch(0.72 0.11 255)",
  codeKey: "oklch(0.8 0.13 82)",
  codeString: "oklch(0.76 0.12 155)",
  codePunct: "oklch(0.58 0.015 65)",
  border: "oklch(1 0 0 / 10%)",
  borderHi: "oklch(1 0 0 / 18%)",
};

export const FONT_SANS = "'Geist', 'Noto Sans SC', sans-serif";
export const FONT_MONO = "'Geist Mono', 'Noto Sans SC', monospace";

export const SPRING_SNAP = { damping: 200 };
export const SPRING_SOFT = { damping: 26, stiffness: 160 };
export const SPRING_BOUNCE = { damping: 12, stiffness: 200 };
