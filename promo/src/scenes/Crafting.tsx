import React from "react";
import {
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Caption, Chip, Sub } from "../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const SLOT = 116;
const GAP = 16;

// grid top-left origin (center-left of screen)
const GX = 560;
const GY = 330;

const slotCenter = (col: number, row: number) => ({
  x: GX + col * (SLOT + GAP) + SLOT / 2,
  y: GY + row * (SLOT + GAP) + SLOT / 2,
});

const RESULT = { x: 1210, y: GY + SLOT + GAP + SLOT / 2 };

// source chips on the right
const MODRINTH = { x: 1640, y: 420 };
const CURSEFORGE = { x: 1640, y: 560 };

type FlyItem = {
  col: number;
  row: number;
  from: { x: number; y: number };
  startAt: number;
  label: string;
  tint: string;
};

const MODS: FlyItem[] = [
  { col: 2, row: 0, from: MODRINTH, startAt: 96, label: "sodium", tint: C.green },
  { col: 0, row: 1, from: MODRINTH, startAt: 114, label: "iris", tint: C.green },
  { col: 2, row: 1, from: CURSEFORGE, startAt: 132, label: "terralith", tint: C.orange },
  { col: 0, row: 2, from: CURSEFORGE, startAt: 150, label: "jei", tint: C.orange },
  { col: 1, row: 2, from: MODRINTH, startAt: 168, label: "fabric-api", tint: C.green },
  { col: 2, row: 2, from: CURSEFORGE, startAt: 186, label: "create", tint: C.orange },
];

const FLIGHT = 30;

const Slot: React.FC<{ x: number; y: number; appear: number; highlight?: boolean }> = ({
  x,
  y,
  appear,
  highlight = false,
}) => (
  <div
    style={{
      position: "absolute",
      left: x - SLOT / 2,
      top: y - SLOT / 2,
      width: SLOT,
      height: SLOT,
      borderRadius: 18,
      background: C.card,
      border: `2px solid ${highlight ? C.primary : C.border}`,
      transform: `scale(${appear})`,
      opacity: appear,
      boxShadow: highlight ? `0 0 40px ${C.primarySoft}` : "none",
    }}
  />
);

const ItemToken: React.FC<{ label: string; tint: string; size?: number }> = ({
  label,
  tint,
  size = 88,
}) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: 16,
      background: C.cardHi,
      border: `2px solid ${tint}`,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontFamily: FONT_MONO,
      fontSize: size > 70 ? 17 : 15,
      fontWeight: 600,
      color: tint,
      boxShadow: "0 10px 30px oklch(0 0 0 / 0.45)",
      textAlign: "center",
      lineHeight: 1.2,
      padding: 4,
    }}
  >
    {label}
  </div>
);

const GrassBlock: React.FC<{ size?: number }> = ({ size = 88 }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: 14,
      overflow: "hidden",
      display: "flex",
      flexDirection: "column",
      border: `2px solid ${C.borderHi}`,
      boxShadow: "0 10px 30px oklch(0 0 0 / 0.45)",
    }}
  >
    <div style={{ flex: 0.32, background: "#5EBB4D" }} />
    <div style={{ flex: 0.68, background: "#8A5A3C" }} />
  </div>
);

export const Crafting: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const gridP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 34 });
  const blockP = spring({ frame: frame - 34, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const loaderP = spring({ frame: frame - 62, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const resultP = spring({ frame: frame - 236, fps, config: SPRING_BOUNCE, durationInFrames: 34 });
  const captionP = spring({ frame: frame - 300, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const chipsP = spring({ frame: frame - 80, fps, config: SPRING_SNAP, durationInFrames: 30 });

  const pulse =
    frame > 216 && frame < 246
      ? 1 + Math.sin(((frame - 216) / 30) * Math.PI) * 0.06
      : 1;

  const out = interpolate(frame, [584, 596], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  // loader chip cycles before settling on Fabric
  const LOADERS = ["Fabric", "Forge", "NeoForge", "Quilt"];
  const loaderIdx =
    frame < 84 ? 0 : frame < 110 ? Math.min(Math.floor((frame - 84) / 8), 3) : 0;

  const s01 = slotCenter(0, 0);
  const s11 = slotCenter(1, 1);

  return (
    <Backdrop gridOpacity={0.05} glowX={0.42}>
      <div style={{ position: "absolute", inset: 0, opacity: out }}>
        {/* crafting grid slots */}
        {[0, 1, 2].flatMap((row) =>
          [0, 1, 2].map((col) => {
            const { x, y } = slotCenter(col, row);
            const d = (row * 3 + col) * 2;
            const p = spring({ frame: frame - d, fps, config: SPRING_SNAP, durationInFrames: 30 });
            return <Slot key={`${row}${col}`} x={x} y={y} appear={p} />;
          })
        )}

        {/* result slot */}
        <Slot x={RESULT.x} y={RESULT.y} appear={gridP} highlight={resultP > 0} />

        {/* arrow */}
        {(() => {
          const arrowP = interpolate(frame, [200, 224], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          });
          return (
            <svg
              width={150}
              height={60}
              style={{ position: "absolute", left: 1005, top: RESULT.y - 30, opacity: gridP }}
            >
              <path
                d="M 8 30 L 116 30 M 90 10 L 118 30 L 90 50"
                fill="none"
                stroke={frame > 216 ? C.primary : C.faint}
                strokeWidth={7}
                strokeLinecap="round"
                strokeLinejoin="round"
                pathLength={1}
                strokeDasharray="1"
                strokeDashoffset={1 - arrowP}
              />
            </svg>
          );
        })()}

        {/* grass block = minecraft version */}
        <div
          style={{
            position: "absolute",
            left: s01.x - 44,
            top: s01.y - 44 - interpolate(blockP, [0, 1], [140, 0]),
            opacity: blockP,
            transform: `scale(${interpolate(blockP, [0, 1], [0.6, 1])})`,
          }}
        >
          <GrassBlock />
        </div>
        <div
          style={{
            position: "absolute",
            left: s01.x - 60,
            top: s01.y + 52,
            opacity: blockP,
            fontFamily: FONT_MONO,
            fontSize: 20,
            color: C.muted,
            width: 120,
            textAlign: "center",
          }}
        >
          1.21.4
        </div>

        {/* loader token */}
        <div
          style={{
            position: "absolute",
            left: s11.x - 44,
            top: s11.y - 44 - interpolate(loaderP, [0, 1], [140, 0]),
            opacity: loaderP,
            transform: `scale(${interpolate(loaderP, [0, 1], [0.6, 1])})`,
          }}
        >
          <ItemToken label={LOADERS[loaderIdx]} tint={C.blue} />
        </div>

        {/* flying mods */}
        {MODS.map((m, i) => {
          const target = slotCenter(m.col, m.row);
          const p = interpolate(frame, [m.startAt, m.startAt + FLIGHT], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          });
          if (p <= 0) return null;
          const arc = Math.sin(p * Math.PI) * -150;
          const x = interpolate(p, [0, 1], [m.from.x, target.x]);
          const y = interpolate(p, [0, 1], [m.from.y, target.y]) + arc;
          const scale = interpolate(p, [0, 0.2, 1], [0.4, 1, 1]);
          return (
            <div
              key={i}
              style={{
                position: "absolute",
                left: x - 44,
                top: y - 44,
                transform: `scale(${scale}) rotate(${(1 - p) * 25}deg)`,
                opacity: Math.min(1, p * 3),
              }}
            >
              <ItemToken label={m.label} tint={m.tint} />
            </div>
          );
        })}

        {/* repository chips */}
        <div style={{ position: "absolute", left: MODRINTH.x - 90, top: MODRINTH.y - 26, opacity: chipsP }}>
          <Chip label="Modrinth" color={C.green} textColor="oklch(0.2 0.05 155)" size={30} />
        </div>
        <div style={{ position: "absolute", left: CURSEFORGE.x - 100, top: CURSEFORGE.y - 26, opacity: chipsP }}>
          <Chip label="CurseForge" color={C.orange} textColor="oklch(0.25 0.06 40)" size={30} />
        </div>

        {/* pulse ring on craft */}
        <div
          style={{
            position: "absolute",
            left: GX - 40,
            top: GY - 40,
            width: 3 * SLOT + 2 * GAP + 80,
            height: 3 * SLOT + 2 * GAP + 80,
            borderRadius: 32,
            border: `3px solid ${C.primary}`,
            opacity: interpolate(frame, [216, 246], [0.7, 0], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
            transform: `scale(${pulse})`,
          }}
        />

        {/* result instance card */}
        <div
          style={{
            position: "absolute",
            left: RESULT.x - 190,
            top: RESULT.y - 120,
            width: 380,
            borderRadius: 24,
            background: C.card,
            border: `2px solid ${C.primary}`,
            padding: "28px 30px",
            opacity: resultP,
            transform: `scale(${interpolate(resultP, [0, 1], [0.5, 1])}) translateY(${Math.sin(frame * 0.05) * 4}px)`,
            boxShadow: `0 24px 80px oklch(0 0 0 / 0.5), 0 0 70px ${C.primarySoft}`,
            transformOrigin: "center",
          }}
        >
          <div style={{ fontFamily: FONT_SANS, fontSize: 40, fontWeight: 800, color: C.fg, letterSpacing: "-0.02em" }}>
            我的整合包
          </div>
          <div style={{ display: "flex", gap: 10, marginTop: 18, flexWrap: "wrap" }}>
            <Chip label="1.21.4" mono size={22} />
            <Chip label="Fabric" mono size={22} color={C.primarySoft} textColor={C.primary} />
            <Chip label="8 个包" size={22} />
          </div>
        </div>

        {/* caption */}
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            bottom: 120,
            textAlign: "center",
            opacity: captionP,
            transform: `translateY(${interpolate(captionP, [0, 1], [30, 0])}px)`,
          }}
        >
          <Caption size={54}>
            版本 + 加载器 + 包，<Caption size={54} color={C.primary} style={{ display: "inline" }}>合成</Caption>一个实例
          </Caption>
        </div>

        {/* top-left hint */}
        <div style={{ position: "absolute", left: 120, top: 110, opacity: gridP }}>
          <Sub size={26} color={C.faint}>创建实例</Sub>
        </div>
      </div>
    </Backdrop>
  );
};
