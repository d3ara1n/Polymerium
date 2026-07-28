import React from "react";
import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Logo } from "../components/Logo";
import { C, FONT_MONO, FONT_SANS, SPRING_SNAP } from "../theme";

export const OutroMini: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const logo = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 34 });
  const word = spring({ frame: frame - 8, fps, config: SPRING_SNAP, durationInFrames: 34 });
  const url = spring({ frame: frame - 26, fps, config: SPRING_SNAP, durationInFrames: 30 });

  return (
    <Backdrop gridOpacity={0.05}>
      <div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          gap: 40,
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 30 }}>
          <Logo size={104} shift={logo} opacity={logo} />
          <div
            style={{
              fontFamily: FONT_SANS,
              fontSize: 88,
              fontWeight: 800,
              letterSpacing: "-0.035em",
              color: C.fg,
              opacity: word,
            }}
          >
            Polymerium
          </div>
        </div>
        <div style={{ fontFamily: FONT_MONO, fontSize: 32, color: C.muted, opacity: url }}>
          github.com/d3ara1n/Polymerium
        </div>
      </div>
      <AbsoluteFill
        style={{
          background: "black",
          opacity: interpolate(frame, [164, 178], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          }),
        }}
      />
    </Backdrop>
  );
};
