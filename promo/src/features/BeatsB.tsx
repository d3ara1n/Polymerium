import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Chip } from "../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const VIS = { left: 990, top: 210, width: 800, height: 660 };

/* ── 03 依赖视图（原地管理） ── */

const DEP_ROWS = [
  { name: "Fabric API", icon: "⚙", required: true, refs: 17 },
  { name: "Mod Menu", icon: "▤", required: false, refs: 10 },
];

export const DependencyBeat: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const modalP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const pick = spring({ frame: frame - 136, fps, config: SPRING_SNAP, durationInFrames: 22 });
  const added = spring({ frame: frame - 162, fps, config: SPRING_BOUNCE, durationInFrames: 24 });

  return (
    <div style={{ position: "absolute", ...VIS, top: 220 }}>
      <div
        style={{
          borderRadius: 24,
          background: C.card,
          border: `1.5px solid ${C.borderHi}`,
          overflow: "hidden",
          opacity: modalP,
          transform: `translateY(${interpolate(modalP, [0, 1], [44, 0])}px)`,
          boxShadow: "0 30px 90px oklch(0 0 0 / 0.5)",
        }}
      >
        {/* modal header */}
        <div style={{ display: "flex", alignItems: "center", gap: 18, padding: "26px 32px 0" }}>
          <div
            style={{
              width: 56,
              height: 56,
              borderRadius: 14,
              background: C.primarySoft,
              border: `2px solid ${C.primary}`,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 26,
            }}
          >
            ⚡
          </div>
          <div>
            <div style={{ fontFamily: FONT_SANS, fontSize: 34, fontWeight: 800, color: C.fg }}>Sodium</div>
            <div style={{ fontFamily: FONT_MONO, fontSize: 19, color: C.primary, marginTop: 2 }}>MODRINTH</div>
          </div>
        </div>
        {/* tabs */}
        <div style={{ display: "flex", gap: 30, padding: "20px 32px 0", borderBottom: `1px solid ${C.border}` }}>
          {["基础", "标签", "版本", "依赖", "历史"].map((t) => (
            <div
              key={t}
              style={{
                fontFamily: FONT_SANS,
                fontSize: 24,
                fontWeight: t === "依赖" ? 700 : 500,
                color: t === "依赖" ? C.fg : C.faint,
                paddingBottom: 14,
                borderBottom: t === "依赖" ? `3px solid ${C.primary}` : "3px solid transparent",
              }}
            >
              {t}
            </div>
          ))}
        </div>
        {/* dependency rows */}
        <div style={{ padding: "20px 24px 28px", display: "flex", flexDirection: "column", gap: 14 }}>
          {DEP_ROWS.map((r, i) => {
            const p = spring({ frame: frame - 56 - i * 12, fps, config: SPRING_SNAP, durationInFrames: 26 });
            const isPicked = i === 1 && pick > 0;
            return (
              <div key={r.name}>
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 16,
                    padding: "18px 22px",
                    borderRadius: 14,
                    background: isPicked ? C.primarySoft : C.cardHi,
                    borderLeft: `4px solid ${isPicked ? C.primary : "transparent"}`,
                    opacity: p,
                    transform: `translateX(${interpolate(p, [0, 1], [-26, 0])}px)`,
                  }}
                >
                  <span style={{ fontSize: 26, color: C.muted }}>{r.icon}</span>
                  <span style={{ fontFamily: FONT_SANS, fontSize: 27, fontWeight: 700, color: C.fg, flex: 1 }}>
                    {r.name}
                  </span>
                  {r.required && <Chip label="必要" color={C.card} textColor={C.muted} size={20} />}
                  <Chip label={`Refs: ${r.refs}`} color={C.card} textColor={C.faint} size={20} mono />
                </div>
                {/* in-place add result */}
                {i === 1 && (
                  <div
                    style={{
                      overflow: "hidden",
                      height: interpolate(added, [0, 1], [0, 62]),
                      opacity: added,
                    }}
                  >
                    <div style={{ padding: "12px 22px 0 46px" }}>
                      <Chip label="✓ 已原地添加到实例" color={C.primary} textColor="oklch(0.26 0.06 75)" size={22} />
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

/* ── 04 依赖图谱 ── */

type Node = { id: string; x: number; y: number };
const NODES: Node[] = [
  { id: "sodium", x: 400, y: 200 },
  { id: "iris", x: 180, y: 130 },
  { id: "fabric-api", x: 380, y: 380 },
  { id: "lithium", x: 610, y: 140 },
  { id: "modmenu", x: 150, y: 330 },
  { id: "create", x: 620, y: 360 },
  { id: "patchouli", x: 660, y: 520 },
  { id: "jei", x: 220, y: 500 },
];
// edges: [from, to]; sodium's subtree = sodium→iris, sodium→fabric-api, iris→fabric-api
const EDGES: [number, number][] = [
  [0, 1], [0, 2], [1, 2], [3, 2], [4, 2], [5, 6], [7, 2], [0, 3],
];
const SUBTREE = new Set([0, 1, 2]);
const MISSING = 6; // patchouli starts as missing

export const GraphBeat: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const panelP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const selected = frame >= 96 && frame < 170;
  const resolveP = spring({ frame: frame - 196, fps, config: SPRING_SNAP, durationInFrames: 26 });
  const missingVisible = frame >= 150 && resolveP < 1;
  const missingGone = resolveP >= 1;

  return (
    <div style={{ position: "absolute", ...VIS, top: 200 }}>
      <div
        style={{
          borderRadius: 24,
          background: C.card,
          border: `1.5px solid ${C.border}`,
          height: 640,
          position: "relative",
          overflow: "hidden",
          opacity: panelP,
          transform: `translateY(${interpolate(panelP, [0, 1], [44, 0])}px)`,
          boxShadow: "0 30px 90px oklch(0 0 0 / 0.45)",
        }}
      >
        {/* edges */}
        <svg width={800} height={640} style={{ position: "absolute", inset: 0 }}>
          {EDGES.map(([a, b], i) => {
            const draw = interpolate(frame, [26 + i * 5, 52 + i * 5], [0, 1], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            });
            if (draw <= 0) return null;
            const na = NODES[a];
            const nb = NODES[b];
            const hot = selected && SUBTREE.has(a) && SUBTREE.has(b);
            return (
              <line
                key={i}
                x1={na.x}
                y1={na.y}
                x2={nb.x}
                y2={nb.y}
                stroke={hot ? C.primary : C.faint}
                strokeWidth={hot ? 3.5 : 2}
                opacity={selected ? (hot ? 0.95 : 0.18) : 0.4}
                pathLength={1}
                strokeDasharray="1"
                strokeDashoffset={1 - draw}
              />
            );
          })}
        </svg>
        {/* nodes */}
        {NODES.map((n, i) => {
          const p = spring({ frame: frame - 8 - i * 6, fps, config: SPRING_BOUNCE, durationInFrames: 24 });
          const hot = selected && SUBTREE.has(i);
          const isMissing = i === MISSING && missingVisible && !missingGone;
          return (
            <div
              key={n.id}
              style={{
                position: "absolute",
                left: n.x - 62,
                top: n.y - 30,
                display: "flex",
                alignItems: "center",
                gap: 10,
                padding: "10px 16px",
                borderRadius: 9999,
                background: C.cardHi,
                border: `2.5px ${isMissing ? "dashed" : "solid"} ${hot ? C.primary : isMissing ? C.red : C.borderHi}`,
                transform: `scale(${p * (hot ? 1.12 : 1)})`,
                opacity: p * (selected && !SUBTREE.has(i) ? 0.35 : 1),
                boxShadow: hot ? `0 0 40px ${C.primarySoft}` : "none",
                zIndex: 2,
              }}
            >
              <div style={{ width: 12, height: 12, borderRadius: 4, background: hot ? C.primary : isMissing ? C.red : C.faint }} />
              <span style={{ fontFamily: FONT_MONO, fontSize: 21, color: C.fg }}>{n.id}</span>
            </div>
          );
        })}
        {/* missing / resolved badges */}
        {missingVisible && (
          <div style={{ position: "absolute", left: NODES[MISSING].x - 40, top: NODES[MISSING].y - 76, zIndex: 3 }}>
            <Chip label="⚠ 缺失" color={C.red} textColor="white" size={20} />
          </div>
        )}
        <div
          style={{
            position: "absolute",
            left: NODES[MISSING].x - 52,
            top: NODES[MISSING].y - 80,
            transform: `scale(${resolveP})`,
            opacity: resolveP,
            zIndex: 3,
          }}
        >
          <Chip label="✓ 已补齐" color={C.primary} textColor="oklch(0.26 0.06 75)" size={20} />
        </div>
      </div>
    </div>
  );
};

/* ── 05 快照系统 ── */

const NODES_TL = [
  { x: 60, label: "周一" },
  { x: 290, label: "周三" },
  { x: 520, label: "周五" },
  { x: 700, label: "昨天" },
];

export const SnapshotBeat: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const lineP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 30 });
  const fwd = interpolate(frame, [30, 90], [NODES_TL[0].x, NODES_TL[3].x], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const back = interpolate(frame, [112, 162], [NODES_TL[3].x, NODES_TL[1].x], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const headX = frame < 100 ? fwd : back;
  const restored = spring({ frame: frame - 172, fps, config: SPRING_BOUNCE, durationInFrames: 28 });

  return (
    <div style={{ position: "absolute", ...VIS, top: 330 }}>
      <div
        style={{
          background: C.card,
          border: `1.5px solid ${C.border}`,
          borderRadius: 24,
          padding: "70px 50px 90px",
          boxShadow: "0 30px 90px oklch(0 0 0 / 0.45)",
          position: "relative",
          opacity: lineP,
        }}
      >
        <div style={{ position: "relative", height: 4, background: C.cardHi, borderRadius: 9999 }}>
          <div
            style={{
              position: "absolute",
              left: 0,
              width: headX,
              height: "100%",
              background: C.primary,
              borderRadius: 9999,
              opacity: 0.85,
            }}
          />
          {NODES_TL.map((n, i) => {
            const p = spring({ frame: frame - 12 - i * 7, fps, config: SPRING_BOUNCE, durationInFrames: 24 });
            const near = Math.abs(headX - n.x) < 26;
            return (
              <div
                key={i}
                style={{
                  position: "absolute",
                  left: n.x - 14,
                  top: -12,
                  width: 28,
                  height: 28,
                  borderRadius: 9999,
                  background: near ? C.primary : C.cardHi,
                  border: `3px solid ${near ? C.primary : C.faint}`,
                  transform: `scale(${p * (near ? 1.25 : 1)})`,
                  opacity: p,
                }}
              />
            );
          })}
          {NODES_TL.map((n, i) => (
            <div
              key={`l${i}`}
              style={{
                position: "absolute",
                left: n.x - 60,
                top: 34,
                width: 120,
                textAlign: "center",
                fontFamily: FONT_SANS,
                fontSize: 22,
                color: C.faint,
                opacity: lineP,
              }}
            >
              {n.label}
            </div>
          ))}
        </div>
        <div
          style={{
            position: "absolute",
            left: NODES_TL[1].x - 30,
            top: -34,
            transform: `scale(${restored})`,
            opacity: restored,
          }}
        >
          <Chip label="⟲ 已恢复" color={C.primary} textColor="oklch(0.26 0.06 75)" size={26} />
        </div>
      </div>
    </div>
  );
};
