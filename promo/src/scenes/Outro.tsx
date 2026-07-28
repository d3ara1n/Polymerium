import React from "react";
import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Logo } from "../components/Logo";
import { Chip, Sub } from "../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const PLATFORMS = ["Windows", "macOS", "Linux"];

export const Outro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const logo = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 40 });
  const word = spring({ frame: frame - 10, fps, config: SPRING_SNAP, durationInFrames: 40 });
  const url = spring({ frame: frame - 120, fps, config: SPRING_SNAP, durationInFrames: 36 });

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
          gap: 44,
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 36 }}>
          <Logo size={120} shift={logo} opacity={logo} />
          <div
            style={{
              fontFamily: FONT_SANS,
              fontSize: 96,
              fontWeight: 800,
              letterSpacing: "-0.035em",
              color: C.fg,
              opacity: word,
              transform: `translateY(${interpolate(word, [0, 1], [30, 0])}px)`,
            }}
          >
            Polymerium
          </div>
        </div>

        <div style={{ display: "flex", gap: 18, alignItems: "center" }}>
          {PLATFORMS.map((p, i) => {
            const s = spring({ frame: frame - 52 - i * 9, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
            return (
              <div key={p} style={{ transform: `scale(${s})`, opacity: s }}>
                <Chip label={p} size={28} />
              </div>
            );
          })}
          <div style={{ transform: `scale(${spring({ frame: frame - 80, fps, config: SPRING_BOUNCE, durationInFrames: 26 })})`, opacity: spring({ frame: frame - 80, fps, config: SPRING_BOUNCE, durationInFrames: 26 }) }}>
            <Chip label="MIT 开源" color={C.primary} textColor="oklch(0.26 0.06 75)" size={28} />
          </div>
        </div>

        <div
          style={{
            fontFamily: FONT_MONO,
            fontSize: 34,
            color: C.fg,
            opacity: url,
            transform: `translateY(${interpolate(url, [0, 1], [22, 0])}px)`,
            letterSpacing: "0.01em",
          }}
        >
          github.com/d3ara1n/Polymerium
        </div>
        <div style={{ opacity: url, marginTop: -18 }}>
          <Sub size={26} color={C.faint}>polymerium.dearain.dev</Sub>
        </div>
      </div>

      {/* final fade to black */}
      <AbsoluteFill
        style={{
          background: "black",
          opacity: interpolate(frame, [342, 358], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          }),
        }}
      />
    </Backdrop>
  );
};
