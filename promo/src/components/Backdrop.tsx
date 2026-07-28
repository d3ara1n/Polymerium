import React from "react";
import { AbsoluteFill, useVideoConfig } from "remotion";
import { C } from "../theme";

export const Backdrop: React.FC<{
  glowColor?: string;
  glowX?: number; // 0..1
  glowY?: number; // 0..1
  glowSize?: number; // px
  gridOpacity?: number;
  children?: React.ReactNode;
}> = ({
  glowColor = C.primary,
  glowX = 0.6,
  glowY = 0.4,
  glowSize = 900,
  gridOpacity = 0.05,
  children,
}) => {
  const { width, height } = useVideoConfig();
  return (
    <AbsoluteFill style={{ backgroundColor: C.bg }}>
      {/* grid */}
      <AbsoluteFill
        style={{
          backgroundImage: `linear-gradient(${glowColor} 1px, transparent 1px), linear-gradient(90deg, ${glowColor} 1px, transparent 1px)`,
          backgroundSize: "72px 72px",
          opacity: gridOpacity,
          maskImage: `radial-gradient(ellipse 75% 65% at ${glowX * 100}% ${glowY * 100}%, black 25%, transparent 72%)`,
          WebkitMaskImage: `radial-gradient(ellipse 75% 65% at ${glowX * 100}% ${glowY * 100}%, black 25%, transparent 72%)`,
        }}
      />
      {/* glow */}
      <div
        style={{
          position: "absolute",
          left: glowX * width - glowSize / 2,
          top: glowY * height - glowSize / 2,
          width: glowSize,
          height: glowSize,
          borderRadius: 9999,
          background: `radial-gradient(circle, ${glowColor} 0%, transparent 70%)`,
          opacity: 0.13,
          filter: "blur(80px)",
        }}
      />
      {children}
      {/* vignette */}
      <AbsoluteFill
        style={{
          background:
            "radial-gradient(ellipse 120% 105% at 50% 45%, transparent 62%, oklch(0 0 0 / 0.42) 100%)",
          pointerEvents: "none",
        }}
      />
    </AbsoluteFill>
  );
};
