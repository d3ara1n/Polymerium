import React from "react";
import {
  AbsoluteFill,
  interpolate,
  random,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Caption, FileIcon, Sub } from "../components/bits";
import { C, FONT_MONO, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const ICON_COUNT = 72;
const COLLAPSE_START = 336; // 5.6s
const QUESTION_OUT = COLLAPSE_START + 8;
const JSON_IN = 402; // 6.7s

type Spawn = {
  x: number; // final position, px relative to center
  y: number;
  rot: number;
  spawnAt: number;
  size: number;
  tint: string;
  label: string;
};

const spawns: Spawn[] = new Array(ICON_COUNT).fill(0).map((_, i) => {
  const angle = random(`a${i}`) * Math.PI * 2;
  const radius = 90 + random(`r${i}`) * 620;
  return {
    x: Math.cos(angle) * radius * 1.35,
    y: Math.sin(angle) * radius * 0.62,
    rot: (random(`rot${i}`) - 0.5) * 40,
    // exponential-feel waves: a few early, then floods
    spawnAt: 24 + Math.floor(Math.pow(random(`s${i}`), 1.6) * 180),
    size: 44 + random(`sz${i}`) * 26,
    tint: random(`t${i}`) > 0.75 ? C.primary : C.muted,
    label: random(`l${i}`) > 0.5 ? ".jar" : ".zip",
  };
});

const JsonCard: React.FC<{ progress: number }> = ({ progress }) => {
  const scale = interpolate(progress, [0, 1], [0.7, 1]);
  const lines: React.ReactNode[] = [
    <span key="0" style={{ color: C.codePunct }}>{"{"}</span>,
    <span key="1">
      <span style={{ color: C.codeKey }}>  "version"</span>
      <span style={{ color: C.codePunct }}>: </span>
      <span style={{ color: C.codeString }}>"1.21.4"</span>
      <span style={{ color: C.codePunct }}>,</span>
    </span>,
    <span key="2">
      <span style={{ color: C.codeKey }}>  "loader"</span>
      <span style={{ color: C.codePunct }}>: </span>
      <span style={{ color: C.codeString }}>"fabric"</span>
      <span style={{ color: C.codePunct }}>,</span>
    </span>,
    <span key="3">
      <span style={{ color: C.codeKey }}>  "packages"</span>
      <span style={{ color: C.codePunct }}>: [</span>
      <span style={{ color: C.codeString }}>"pref://modrinth/sodium"</span>
      <span style={{ color: C.codePunct }}>, …]</span>
    </span>,
    <span key="4" style={{ color: C.codePunct }}>{"}"}</span>,
  ];
  return (
    <div
      style={{
        transform: `scale(${scale})`,
        opacity: progress,
        background: C.card,
        border: `1.5px solid ${C.borderHi}`,
        borderRadius: 20,
        padding: "26px 34px",
        fontFamily: FONT_MONO,
        fontSize: 27,
        lineHeight: 1.75,
        boxShadow: `0 0 0 1px oklch(0 0 0 / 0.3), 0 24px 80px oklch(0 0 0 / 0.5), 0 0 90px ${C.primarySoft}`,
        display: "flex",
        flexDirection: "column",
      }}
    >
      <div
        style={{
          fontSize: 20,
          color: C.faint,
          marginBottom: 10,
          letterSpacing: "0.04em",
        }}
      >
        profile.json
      </div>
      {lines.map((l, i) => (
        <div key={i}>{l}</div>
      ))}
    </div>
  );
};

export const Hook: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();
  const cx = width / 2;
  const cy = height * 0.46;

  // Disk usage climbs as icons multiply, freezes at collapse
  const diskT = Math.min(frame, COLLAPSE_START);
  const gb = interpolate(diskT, [24, COLLAPSE_START], [2.1, 47.8], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const diskPct = interpolate(gb, [0, 64], [0, 100]);
  const diskRed = gb > 38;

  const jsonP = spring({ frame: frame - JSON_IN, fps, config: SPRING_SNAP, durationInFrames: 40 });

  const questionP = spring({ frame: frame - 200, fps, config: SPRING_SNAP, durationInFrames: 40 });
  const questionOut = interpolate(frame, [QUESTION_OUT, QUESTION_OUT + 14], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const statementP = spring({ frame: frame - (JSON_IN + 22), fps, config: SPRING_SNAP, durationInFrames: 40 });

  return (
    <Backdrop glowColor={diskRed && frame < COLLAPSE_START ? C.red : C.primary} gridOpacity={0.045}>
      {/* multiplying files */}
      {spawns.map((s, i) => {
        const born = spring({ frame: frame - s.spawnAt, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
        if (born <= 0) return null;
        // chaos jitter before collapse
        const jitter =
          frame > 240 && frame < COLLAPSE_START
            ? Math.sin(frame * 0.35 + i) * 4
            : 0;
        // collapse: converge to center
        const cp = interpolate(
          frame,
          [COLLAPSE_START + (i % 12), COLLAPSE_START + 26 + (i % 12)],
          [0, 1],
          { extrapolateLeft: "clamp", extrapolateRight: "clamp" }
        );
        const x = interpolate(cp, [0, 1], [s.x, 0]);
        const y = interpolate(cp, [0, 1], [s.y + jitter, 0]);
        const scale = born * (1 - cp);
        return (
          <div
            key={i}
            style={{
              position: "absolute",
              left: cx + x,
              top: cy + y,
              transform: `translate(-50%, -50%) rotate(${s.rot * (1 - cp)}deg) scale(${scale})`,
            }}
          >
            <FileIcon size={s.size} label={s.label} tint={s.tint} opacity={0.92} />
          </div>
        );
      })}

      {/* flash on collapse */}
      <div
        style={{
          position: "absolute",
          left: cx - 300,
          top: cy - 300,
          width: 600,
          height: 600,
          borderRadius: 9999,
          background: `radial-gradient(circle, ${C.primary} 0%, transparent 65%)`,
          opacity: interpolate(frame, [COLLAPSE_START + 26, COLLAPSE_START + 40, JSON_IN + 6], [0, 0.5, 0], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          }),
          filter: "blur(40px)",
        }}
      />

      {/* question caption */}
      <AbsoluteFill style={{ alignItems: "center" }}>
        <div style={{ marginTop: 110, opacity: questionP * questionOut }}>
          <Caption size={62}>
            每个实例，都是一堆<Caption size={62} color={C.faint} style={{ display: "inline" }}>文件副本</Caption>？
          </Caption>
        </div>
      </AbsoluteFill>

      {/* profile.json card + statement */}
      <AbsoluteFill style={{ alignItems: "center", justifyContent: "center" }}>
        <div style={{ transform: "translateY(-24px)" }}>
          <JsonCard progress={jsonP} />
        </div>
      </AbsoluteFill>
      <AbsoluteFill style={{ alignItems: "center", justifyContent: "flex-end" }}>
        <div style={{ marginBottom: 210, opacity: statementP, transform: `translateY(${interpolate(statementP, [0, 1], [28, 0])}px)` }}>
          <Caption size={58}>
            实例不是文件夹，是<Caption size={58} color={C.primary} style={{ display: "inline" }}>一份描述</Caption>。
          </Caption>
        </div>
      </AbsoluteFill>

      {/* disk bar */}
      <AbsoluteFill style={{ justifyContent: "flex-end", alignItems: "center" }}>
        <div
          style={{
            marginBottom: 90,
            width: 760,
            opacity: interpolate(frame, [36, 60], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }) *
              interpolate(frame, [JSON_IN - 10, JSON_IN + 8], [1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }),
          }}
        >
          <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 14 }}>
            <Sub size={26} color={C.muted}>磁盘占用</Sub>
            <div style={{ fontFamily: FONT_MONO, fontSize: 30, fontWeight: 600, color: diskRed ? C.red : C.fg }}>
              {gb.toFixed(1)} GB
            </div>
          </div>
          <div style={{ height: 14, borderRadius: 9999, background: C.cardHi, overflow: "hidden" }}>
            <div
              style={{
                width: `${diskPct}%`,
                height: "100%",
                borderRadius: 9999,
                background: diskRed ? C.red : C.muted,
              }}
            />
          </div>
        </div>
      </AbsoluteFill>
    </Backdrop>
  );
};
