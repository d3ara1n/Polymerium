import React from "react";
import { interpolate, random, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../../components/Backdrop";
import { Caption, FileIcon } from "../../components/bits";
import { C, SPRING_BOUNCE, SPRING_SNAP } from "../../theme";

const COUNT = 44;
const COLLAPSE = 132;

const spawns = new Array(COUNT).fill(0).map((_, i) => {
  const angle = random(`va${i}`) * Math.PI * 2;
  const radius = 120 + random(`vr${i}`) * 560;
  return {
    x: Math.cos(angle) * radius * 0.78,
    y: Math.sin(angle) * radius * 1.28,
    rot: (random(`vrot${i}`) - 0.5) * 36,
    spawnAt: 8 + Math.floor(Math.pow(random(`vs${i}`), 1.5) * 80),
    size: 46 + random(`vsz${i}`) * 24,
    tint: random(`vt${i}`) > 0.75 ? C.primary : C.muted,
    label: random(`vl${i}`) > 0.5 ? ".jar" : ".zip",
  };
});

export const HookV: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();
  const cx = width / 2;
  const cy = height * 0.4;

  const statementP = spring({ frame: frame - 190, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const out = interpolate(frame, [228, 238], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <Backdrop gridOpacity={0.045} glowY={0.38}>
      <div style={{ position: "absolute", inset: 0, opacity: out }}>
        {spawns.map((s, i) => {
          const born = spring({ frame: frame - s.spawnAt, fps, config: SPRING_BOUNCE, durationInFrames: 24 });
          if (born <= 0) return null;
          const cp = interpolate(frame, [COLLAPSE + (i % 10), COLLAPSE + 24 + (i % 10)], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          });
          const x = interpolate(cp, [0, 1], [s.x, 0]);
          const y = interpolate(cp, [0, 1], [s.y, 0]);
          return (
            <div
              key={i}
              style={{
                position: "absolute",
                left: cx + x,
                top: cy + y,
                transform: `translate(-50%, -50%) rotate(${s.rot * (1 - cp)}deg) scale(${born * (1 - cp)})`,
              }}
            >
              <FileIcon size={s.size} label={s.label} tint={s.tint} opacity={0.92} />
            </div>
          );
        })}
        {/* flash */}
        <div
          style={{
            position: "absolute",
            left: cx - 260,
            top: cy - 260,
            width: 520,
            height: 520,
            borderRadius: 9999,
            background: `radial-gradient(circle, ${C.primary} 0%, transparent 65%)`,
            opacity: interpolate(frame, [COLLAPSE + 24, COLLAPSE + 38, COLLAPSE + 60], [0, 0.5, 0], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
            filter: "blur(40px)",
          }}
        />
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            top: height * 0.62,
            textAlign: "center",
            opacity: statementP,
            transform: `translateY(${interpolate(statementP, [0, 1], [34, 0])}px)`,
          }}
        >
          <Caption size={76}>
            实例不是文件夹，
            <br />
            是<span style={{ color: C.primary }}>一份描述</span>。
          </Caption>
        </div>
      </div>
    </Backdrop>
  );
};
