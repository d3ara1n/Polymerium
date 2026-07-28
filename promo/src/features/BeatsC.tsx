import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Chip } from "../components/bits";
import { C, FONT_MONO, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const VIS = { left: 990, top: 210, width: 800, height: 660 };

/* ── 06 Git 友好 ── */

const DIFF: { sign: string; text: string; kind: "ctx" | "del" | "add" }[] = [
  { sign: " ", text: '"name": "我的整合包",', kind: "ctx" },
  { sign: "-", text: '"version": "1.21.1",', kind: "del" },
  { sign: "+", text: '"version": "1.21.4",', kind: "add" },
  { sign: " ", text: '"loader": "fabric",', kind: "ctx" },
  { sign: "+", text: '"packages": [+3 new]', kind: "add" },
];

export const GitBeat: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const cardP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const commitP = spring({ frame: frame - 24, fps, config: SPRING_BOUNCE, durationInFrames: 26 });

  return (
    <div style={{ position: "absolute", ...VIS, top: 250 }}>
      <div style={{ transform: `scale(${commitP})`, opacity: commitP, transformOrigin: "left bottom", marginBottom: 22 }}>
        <Chip label="⎇ main · feat: 升级到 1.21.4" color={C.cardHi} textColor={C.fg} mono size={24} />
      </div>
      <div
        style={{
          background: C.card,
          border: `1.5px solid ${C.border}`,
          borderRadius: 22,
          overflow: "hidden",
          boxShadow: "0 30px 90px oklch(0 0 0 / 0.45)",
          opacity: cardP,
          transform: `translateY(${interpolate(cardP, [0, 1], [40, 0])}px)`,
        }}
      >
        <div
          style={{
            padding: "16px 28px",
            borderBottom: `1px solid ${C.border}`,
            fontFamily: FONT_MONO,
            fontSize: 22,
            color: C.faint,
          }}
        >
          profile.json
        </div>
        {DIFF.map((l, i) => {
          const p = spring({ frame: frame - 40 - i * 9, fps, config: SPRING_SNAP, durationInFrames: 24 });
          const bg =
            l.kind === "del"
              ? "oklch(0.62 0.21 27 / 0.14)"
              : l.kind === "add"
                ? "oklch(0.76 0.12 155 / 0.13)"
                : "transparent";
          const signColor = l.kind === "del" ? C.red : l.kind === "add" ? C.codeString : C.faint;
          return (
            <div
              key={i}
              style={{
                display: "flex",
                gap: 18,
                padding: "13px 28px",
                background: bg,
                fontFamily: FONT_MONO,
                fontSize: 25,
                opacity: p,
                transform: `translateX(${interpolate(p, [0, 1], [-30, 0])}px)`,
              }}
            >
              <span style={{ color: signColor, width: 22 }}>{l.sign}</span>
              <span style={{ color: l.kind === "ctx" ? C.muted : C.fg }}>{l.text}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
};
