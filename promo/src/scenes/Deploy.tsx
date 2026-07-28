import React from "react";
import {
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Caption, Sub } from "../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_SNAP } from "../theme";

const FILES = [
  { name: "sodium-0.6.13.jar", targets: [0, 1, 2] },
  { name: "iris-1.8.1.jar", targets: [0, 1] },
  { name: "terralith-2.5.jar", targets: [0] },
  { name: "fabric-api-0.115.jar", targets: [0, 1, 2] },
  { name: "jei-19.8.jar", targets: [0, 2] },
];

const INSTANCES = [
  { name: "生存实况", meta: "87 个包 · Fabric" },
  { name: "创造测试", meta: "42 个包 · NeoForge" },
  { name: "整合开发", meta: "103 个包 · Quilt" },
];

const CACHE_X = 150;
const CACHE_W = 400;
const FILE_Y0 = 400;
const FILE_H = 58;
const FILE_GAP = 14;

const INST_X = 1340;
const INST_W = 420;
const INST_Y0 = 330;
const INST_H = 150;
const INST_GAP = 40;

const fileY = (i: number) => FILE_Y0 + i * (FILE_H + FILE_GAP) + FILE_H / 2;
const instY = (i: number) => INST_Y0 + i * (INST_H + INST_GAP) + INST_H / 2;

type Pt = { x: number; y: number };

const cubic = (p0: Pt, p1: Pt, p2: Pt, p3: Pt, t: number): Pt => {
  const u = 1 - t;
  return {
    x: u * u * u * p0.x + 3 * u * u * t * p1.x + 3 * u * t * t * p2.x + t * t * t * p3.x,
    y: u * u * u * p0.y + 3 * u * u * t * p1.y + 3 * u * t * t * p2.y + t * t * t * p3.y,
  };
};

const threadPath = (x1: number, y1: number, x2: number, y2: number) => {
  const dx = (x2 - x1) * 0.5;
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`;
};

// flatten: every (file, target) pair
const THREADS = FILES.flatMap((f, fi) =>
  f.targets.map((ti) => ({ fi, ti }))
);

export const Deploy: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();

  const cacheP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 34 });
  const counterP = spring({ frame: frame - 250, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const captionP = spring({ frame: frame - 310, fps, config: SPRING_SNAP, durationInFrames: 36 });

  // dedup countdown while threads connect
  const dup = interpolate(frame, [90, 210], [12.4, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const out = interpolate(frame, [584, 596], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <Backdrop gridOpacity={0.04} glowX={0.3} glowY={0.5}>
      <div style={{ position: "absolute", inset: 0, opacity: out }}>
        {/* threads (behind cards) */}
        <svg
          width={width}
          height={height}
          style={{ position: "absolute", inset: 0 }}
        >
          {THREADS.map(({ fi, ti }, k) => {
            const startAt = 84 + k * 5;
            const draw = interpolate(frame, [startAt, startAt + 34], [0, 1], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            });
            if (draw <= 0) return null;
            const x1 = CACHE_X + CACHE_W;
            const y1 = fileY(fi);
            const x2 = INST_X;
            const y2 = instY(ti);
            const d = threadPath(x1, y1, x2, y2);
            // traveling pulse
            const t = ((frame * 0.006 + k * 0.13) % 1);
            const pulsePos = cubic(
              { x: x1, y: y1 },
              { x: x1 + (x2 - x1) * 0.5, y: y1 },
              { x: x2 - (x2 - x1) * 0.5, y: y2 },
              { x: x2, y: y2 },
              t
            );
            return (
              <g key={k}>
                <path
                  d={d}
                  fill="none"
                  stroke={C.primary}
                  strokeWidth={2.5}
                  opacity={0.34}
                  pathLength={1}
                  strokeDasharray="1"
                  strokeDashoffset={1 - draw}
                />
                {draw >= 1 && (
                  <circle cx={pulsePos.x} cy={pulsePos.y} r={5} fill={C.primary} opacity={0.9} />
                )}
              </g>
            );
          })}
        </svg>

        {/* cache card */}
        <div
          style={{
            position: "absolute",
            left: CACHE_X,
            top: FILE_Y0 - 96,
            width: CACHE_W,
            opacity: cacheP,
            transform: `translateY(${interpolate(cacheP, [0, 1], [30, 0])}px)`,
          }}
        >
          <Sub size={26} color={C.faint} style={{ marginBottom: 16 }}>
            共享缓存
          </Sub>
          <div style={{ display: "flex", flexDirection: "column", gap: FILE_GAP }}>
            {FILES.map((f, i) => {
              const p = spring({ frame: frame - 10 - i * 7, fps, config: SPRING_SNAP, durationInFrames: 30 });
              return (
                <div
                  key={i}
                  style={{
                    height: FILE_H,
                    borderRadius: 14,
                    background: C.card,
                    border: `1.5px solid ${C.border}`,
                    display: "flex",
                    alignItems: "center",
                    padding: "0 22px",
                    fontFamily: FONT_MONO,
                    fontSize: 22,
                    color: C.fg,
                    opacity: p,
                    transform: `translateX(${interpolate(p, [0, 1], [-24, 0])}px)`,
                  }}
                >
                  {f.name}
                </div>
              );
            })}
          </div>
        </div>

        {/* instance cards */}
        <div style={{ position: "absolute", left: INST_X, top: INST_Y0 - 96, opacity: 1 }}>
          <Sub size={26} color={C.faint} style={{ marginBottom: 16, opacity: cacheP }}>
            实例
          </Sub>
        </div>
        {INSTANCES.map((inst, i) => {
          const p = spring({ frame: frame - 36 - i * 12, fps, config: SPRING_SNAP, durationInFrames: 34 });
          return (
            <div
              key={i}
              style={{
                position: "absolute",
                left: INST_X,
                top: instY(i) - INST_H / 2,
                width: INST_W,
                height: INST_H,
                borderRadius: 22,
                background: C.card,
                border: `1.5px solid ${C.borderHi}`,
                padding: "30px 32px",
                opacity: p,
                transform: `translateX(${interpolate(p, [0, 1], [40, 0])}px)`,
                boxShadow: "0 18px 50px oklch(0 0 0 / 0.4)",
              }}
            >
              <div style={{ fontFamily: FONT_SANS, fontSize: 34, fontWeight: 800, color: C.fg, letterSpacing: "-0.01em" }}>
                {inst.name}
              </div>
              <div style={{ fontFamily: FONT_MONO, fontSize: 21, color: C.muted, marginTop: 12 }}>
                {inst.meta}
              </div>
            </div>
          );
        })}

        {/* dedup counter */}
        <div
          style={{
            position: "absolute",
            left: CACHE_X,
            top: FILE_Y0 + 5 * (FILE_H + FILE_GAP) + 40,
            opacity: counterP,
            transform: `translateY(${interpolate(counterP, [0, 1], [26, 0])}px)`,
          }}
        >
          <div style={{ fontFamily: FONT_SANS, fontSize: 26, color: C.muted, fontWeight: 500 }}>
            重复文件占用
          </div>
          <div
            style={{
              fontFamily: FONT_MONO,
              fontSize: 88,
              fontWeight: 700,
              color: dup < 0.05 ? C.primary : C.fg,
              letterSpacing: "-0.03em",
              marginTop: 6,
            }}
          >
            {dup.toFixed(1)} GB
          </div>
        </div>

        {/* caption */}
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            bottom: 110,
            textAlign: "center",
            opacity: captionP,
            transform: `translateY(${interpolate(captionP, [0, 1], [30, 0])}px)`,
          }}
        >
          <Caption size={52}>
            每个模组只存一份，符号链接<Caption size={52} color={C.primary} style={{ display: "inline" }}>按需构建</Caption>
          </Caption>
        </div>
      </div>
    </Backdrop>
  );
};
