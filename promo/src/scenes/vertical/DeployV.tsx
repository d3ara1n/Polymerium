import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../../components/Backdrop";
import { Caption, Sub } from "../../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_SNAP } from "../../theme";

const FILES = ["sodium-0.6.13.jar", "iris-1.8.1.jar", "terralith-2.5.jar", "fabric-api-0.115.jar"];
const TARGETS: number[][] = [[0, 1, 2], [0, 1], [0], [0, 2]];
const INSTANCES = [
  { name: "生存实况", meta: "87 个包 · Fabric" },
  { name: "创造测试", meta: "42 个包 · NeoForge" },
  { name: "整合开发", meta: "103 个包 · Quilt" },
];

const CACHE_X = 90;
const CACHE_W = 900;
const FILE_Y0 = 260;
const FILE_H = 54;
const FILE_GAP = 12;

const INST_X = 90;
const INST_W = 900;
const INST_Y0 = 1080;
const INST_H = 128;
const INST_GAP = 34;

const THREADS = TARGETS.flatMap((targets, fi) => targets.map((ti) => ({ fi, ti })));

const bezierY = (y1: number, y2: number, x1: number, x2: number, t: number) => {
  const u = 1 - t;
  const cy1 = y1 + (y2 - y1) * 0.5;
  const cy2 = y2 - (y2 - y1) * 0.5;
  return {
    x: u * u * u * x1 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x2,
    y: u * u * u * y1 + 3 * u * u * t * cy1 + 3 * u * t * t * cy2 + t * t * t * y2,
  };
};

export const DeployV: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();

  const cacheP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 30 });
  const captionP = spring({ frame: frame - 240, fps, config: SPRING_SNAP, durationInFrames: 34 });
  const dup = interpolate(frame, [70, 170], [12.4, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const out = interpolate(frame, [346, 358], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const cacheBottom = FILE_Y0 + 4 * (FILE_H + FILE_GAP) + 30;

  return (
    <Backdrop gridOpacity={0.04} glowY={0.3}>
      <div style={{ position: "absolute", inset: 0, opacity: out }}>
        {/* threads */}
        <svg width={width} height={height} style={{ position: "absolute", inset: 0 }}>
          {THREADS.map(({ fi, ti }, k) => {
            const startAt = 60 + k * 6;
            const draw = interpolate(frame, [startAt, startAt + 30], [0, 1], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            });
            if (draw <= 0) return null;
            const x1 = CACHE_X + 140 + fi * 200;
            const y1 = cacheBottom;
            const x2 = INST_X + 170 + ti * 280;
            const y2 = INST_Y0 + ti * (INST_H + INST_GAP) - 14;
            const t = (frame * 0.008 + k * 0.17) % 1;
            const pos = bezierY(y1, y2, x1, x2, t);
            return (
              <g key={k}>
                <path
                  d={`M ${x1} ${y1} C ${x1} ${y1 + (y2 - y1) * 0.5}, ${x2} ${y2 - (y2 - y1) * 0.5}, ${x2} ${y2}`}
                  fill="none"
                  stroke={C.primary}
                  strokeWidth={2.5}
                  opacity={0.34}
                  pathLength={1}
                  strokeDasharray="1"
                  strokeDashoffset={1 - draw}
                />
                {draw >= 1 && <circle cx={pos.x} cy={pos.y} r={5} fill={C.primary} opacity={0.9} />}
              </g>
            );
          })}
        </svg>

        {/* cache */}
        <div style={{ position: "absolute", left: CACHE_X, top: FILE_Y0 - 64, opacity: cacheP }}>
          <Sub size={26} color={C.faint}>共享缓存</Sub>
        </div>
        {FILES.map((f, i) => {
          const p = spring({ frame: frame - 8 - i * 6, fps, config: SPRING_SNAP, durationInFrames: 26 });
          return (
            <div
              key={i}
              style={{
                position: "absolute",
                left: CACHE_X,
                top: FILE_Y0 + i * (FILE_H + FILE_GAP),
                width: CACHE_W,
                height: FILE_H,
                borderRadius: 14,
                background: C.card,
                border: `1.5px solid ${C.border}`,
                display: "flex",
                alignItems: "center",
                padding: "0 24px",
                fontFamily: FONT_MONO,
                fontSize: 23,
                color: C.fg,
                opacity: p,
                transform: `translateY(${interpolate(p, [0, 1], [-20, 0])}px)`,
              }}
            >
              {f}
            </div>
          );
        })}

        {/* instances */}
        {INSTANCES.map((inst, i) => {
          const p = spring({ frame: frame - 30 - i * 10, fps, config: SPRING_SNAP, durationInFrames: 30 });
          return (
            <div
              key={i}
              style={{
                position: "absolute",
                left: INST_X,
                top: INST_Y0 + i * (INST_H + INST_GAP),
                width: INST_W,
                height: INST_H,
                borderRadius: 20,
                background: C.card,
                border: `1.5px solid ${C.borderHi}`,
                padding: "28px 34px",
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                opacity: p,
                transform: `translateY(${interpolate(p, [0, 1], [30, 0])}px)`,
                boxShadow: "0 16px 44px oklch(0 0 0 / 0.35)",
              }}
            >
              <div style={{ fontFamily: FONT_SANS, fontSize: 34, fontWeight: 800, color: C.fg }}>{inst.name}</div>
              <div style={{ fontFamily: FONT_MONO, fontSize: 21, color: C.muted }}>{inst.meta}</div>
            </div>
          );
        })}

        {/* counter + caption */}
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            top: 1630,
            textAlign: "center",
            opacity: captionP,
          }}
        >
          <div style={{ fontFamily: FONT_MONO, fontSize: 30, color: C.muted, marginBottom: 10 }}>
            重复文件占用 <span style={{ color: C.primary, fontWeight: 700 }}>{dup.toFixed(1)} GB</span>
          </div>
          <Caption size={54}>
            每个模组只存一份，符号链接<span style={{ color: C.primary }}>按需构建</span>
          </Caption>
        </div>
      </div>
    </Backdrop>
  );
};
