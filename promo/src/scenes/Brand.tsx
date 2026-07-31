import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Logo } from "../components/Logo";
import { Sub } from "../components/bits";
import { C, FONT_SANS, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const VERBS = ["管理", "打包", "分享"];
const VERB_EVERY = 72; // frames per verb
const EXIT_AT = 348;

const Verb: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const idx = Math.min(Math.floor(frame / VERB_EVERY), VERBS.length - 1);
  const local = frame - idx * VERB_EVERY;
  const enter = spring({ frame: local, fps, config: SPRING_BOUNCE, durationInFrames: 30 });
  const exit = interpolate(local, [VERB_EVERY - 12, VERB_EVERY], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const settled = idx === VERBS.length - 1 ? 1 : exit;
  return (
    <span
      style={{
        display: "inline-block",
        color: C.primary,
        opacity: enter * settled,
        transform: `translateY(${interpolate(enter, [0, 1], [52, 0]) - (1 - settled) * 40}px)`,
        minWidth: "3.2em",
        textAlign: "right",
      }}
    >
      {VERBS[idx]}
    </span>
  );
};

export const Brand: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const assemble = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 44 });
  const wordP = spring({ frame: frame - 18, fps, config: SPRING_SNAP, durationInFrames: 40 });
  const lineP = spring({ frame: frame - 44, fps, config: SPRING_SNAP, durationInFrames: 40 });
  const subP = spring({ frame: frame - 200, fps, config: SPRING_SNAP, durationInFrames: 40 });

  const out = interpolate(frame, [EXIT_AT, EXIT_AT + 12], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

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
          gap: 54,
          opacity: out,
          transform: `scale(${interpolate(out, [0, 1], [1.04, 1])})`,
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 42 }}>
          <Logo size={150} shift={assemble} opacity={assemble} />
          <div
            style={{
              fontFamily: FONT_SANS,
              fontSize: 118,
              fontWeight: 800,
              letterSpacing: "-0.035em",
              color: C.fg,
              opacity: wordP,
              transform: `translateY(${interpolate(wordP, [0, 1], [34, 0])}px)`,
            }}
          >
            Polymerium
          </div>
        </div>
        <div
          style={{
            fontFamily: FONT_SANS,
            fontSize: 72,
            fontWeight: 700,
            letterSpacing: "-0.02em",
            color: C.fg,
            opacity: lineP,
            display: "flex",
            gap: "0.35em",
          }}
        >
          <Verb />
          <span>你的 Minecraft 体验</span>
        </div>
        <div style={{ opacity: subP, transform: `translateY(${interpolate(subP, [0, 1], [24, 0])}px)` }}>
          <Sub size={32}>元数据驱动的实例管理 · 跨平台 · MIT 开源</Sub>
        </div>
      </div>
    </Backdrop>
  );
};
