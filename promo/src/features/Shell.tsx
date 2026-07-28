import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Sub } from "../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_SNAP } from "../theme";

// Shared layout for one feature beat (300 frames): index + title + sub on the
// left, animated visual on the right.
export const FeatureShell: React.FC<{
  index: string;
  title: React.ReactNode;
  sub: string;
  glowX?: number;
  children?: React.ReactNode;
}> = ({ index, title, sub, glowX = 0.72, children }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const enter = (d: number) =>
    spring({ frame: frame - d, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const out = interpolate(frame, [282, 298], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const outY = interpolate(out, [0, 1], [-26, 0]);

  return (
    <Backdrop gridOpacity={0.04} glowX={glowX}>
      <div style={{ position: "absolute", inset: 0, opacity: out, transform: `translateY(${outY}px)` }}>
        <div style={{ position: "absolute", left: 140, top: 300, width: 720 }}>
          <div
            style={{
              fontFamily: FONT_MONO,
              fontSize: 30,
              fontWeight: 500,
              color: C.faint,
              opacity: enter(0),
              letterSpacing: "0.08em",
            }}
          >
            {index}
          </div>
          <div
            style={{
              fontFamily: FONT_SANS,
              fontSize: 88,
              fontWeight: 900,
              color: C.fg,
              letterSpacing: "-0.03em",
              lineHeight: 1.15,
              marginTop: 22,
              opacity: enter(8),
              transform: `translateY(${interpolate(enter(8), [0, 1], [44, 0])}px)`,
            }}
          >
            {title}
          </div>
          <div
            style={{
              marginTop: 30,
              opacity: enter(18),
              transform: `translateY(${interpolate(enter(18), [0, 1], [30, 0])}px)`,
            }}
          >
            <Sub size={32}>{sub}</Sub>
          </div>
        </div>
        {children}
      </div>
    </Backdrop>
  );
};
