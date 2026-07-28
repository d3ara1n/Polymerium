import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT_MONO, FONT_SANS, SPRING_SNAP } from "../theme";

// Spring-in wrapper: rise + fade, with optional delay (in frames, relative to
// the enclosing Sequence).
export const Rise: React.FC<{
  delay?: number;
  dy?: number;
  durationInFrames?: number;
  children?: React.ReactNode;
  style?: React.CSSProperties;
}> = ({ delay = 0, dy = 36, children, style }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const p = spring({ frame: frame - delay, fps, config: SPRING_SNAP, durationInFrames: 50 });
  return (
    <div
      style={{
        opacity: p,
        transform: `translateY(${interpolate(p, [0, 1], [dy, 0])}px)`,
        ...style,
      }}
    >
      {children}
    </div>
  );
};

export const Chip: React.FC<{
  label: string;
  color?: string;
  textColor?: string;
  mono?: boolean;
  size?: number;
  style?: React.CSSProperties;
}> = ({ label, color = C.cardHi, textColor = C.fg, mono = false, size = 30, style }) => (
  <div
    style={{
      display: "inline-flex",
      alignItems: "center",
      padding: `${size * 0.32}px ${size * 0.75}px`,
      borderRadius: 9999,
      background: color,
      color: textColor,
      fontFamily: mono ? FONT_MONO : FONT_SANS,
      fontWeight: 600,
      fontSize: size,
      letterSpacing: "0.01em",
      ...style,
    }}
  >
    {label}
  </div>
);

// A jar/mod file icon used in the hook scene.
export const FileIcon: React.FC<{
  size?: number;
  label?: string;
  tint?: string;
  opacity?: number;
}> = ({ size = 64, label = ".jar", tint = C.muted, opacity = 1 }) => (
  <div
    style={{
      width: size * 0.82,
      height: size,
      borderRadius: size * 0.1,
      background: C.card,
      border: `2px solid ${C.borderHi}`,
      display: "flex",
      flexDirection: "column",
      alignItems: "center",
      justifyContent: "center",
      gap: size * 0.06,
      opacity,
      boxShadow: "0 6px 18px oklch(0 0 0 / 0.35)",
    }}
  >
    <div
      style={{
        width: size * 0.34,
        height: size * 0.34,
        borderRadius: size * 0.06,
        border: `2.5px solid ${tint}`,
      }}
    />
    <div
      style={{
        fontFamily: FONT_MONO,
        fontSize: size * 0.17,
        color: tint,
        fontWeight: 500,
      }}
    >
      {label}
    </div>
  </div>
);

export const Caption: React.FC<{
  children: React.ReactNode;
  size?: number;
  color?: string;
  weight?: number;
  style?: React.CSSProperties;
}> = ({ children, size = 44, color = C.fg, weight = 700, style }) => (
  <div
    style={{
      fontFamily: FONT_SANS,
      fontSize: size,
      fontWeight: weight,
      color,
      letterSpacing: "-0.02em",
      lineHeight: 1.25,
      ...style,
    }}
  >
    {children}
  </div>
);

export const Sub: React.FC<{
  children: React.ReactNode;
  size?: number;
  color?: string;
  style?: React.CSSProperties;
}> = ({ children, size = 28, color = C.muted, style }) => (
  <div
    style={{
      fontFamily: FONT_SANS,
      fontSize: size,
      fontWeight: 500,
      color,
      letterSpacing: "0",
      lineHeight: 1.5,
      ...style,
    }}
  >
    {children}
  </div>
);
