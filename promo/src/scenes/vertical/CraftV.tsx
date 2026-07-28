import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../../components/Backdrop";
import { Caption, Chip } from "../../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_BOUNCE, SPRING_SNAP } from "../../theme";

const SLOT = 100;
const GAP = 14;
const GX = 350; // grid left
const GY = 330;

const center = (col: number, row: number) => ({
  x: GX + col * (SLOT + GAP) + SLOT / 2,
  y: GY + row * (SLOT + GAP) + SLOT / 2,
});

const MODRINTH = { x: 880, y: 210 };
const CURSEFORGE = { x: 880, y: 330 };

const MODS = [
  { col: 2, row: 0, from: MODRINTH, startAt: 60, label: "sodium", tint: C.green },
  { col: 0, row: 1, from: MODRINTH, startAt: 76, label: "iris", tint: C.green },
  { col: 2, row: 1, from: CURSEFORGE, startAt: 92, label: "terralith", tint: C.orange },
  { col: 0, row: 2, from: CURSEFORGE, startAt: 108, label: "jei", tint: C.orange },
  { col: 1, row: 2, from: MODRINTH, startAt: 124, label: "fabric-api", tint: C.green },
  { col: 2, row: 2, from: CURSEFORGE, startAt: 140, label: "create", tint: C.orange },
];

const RESULT = { x: 540, y: 1010 };

const Item: React.FC<{ label: string; tint: string }> = ({ label, tint }) => (
  <div
    style={{
      width: 80,
      height: 80,
      borderRadius: 14,
      background: C.cardHi,
      border: `2px solid ${tint}`,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontFamily: FONT_MONO,
      fontSize: 15,
      fontWeight: 600,
      color: tint,
      boxShadow: "0 10px 30px oklch(0 0 0 / 0.45)",
      textAlign: "center",
      lineHeight: 1.2,
      padding: 3,
    }}
  >
    {label}
  </div>
);

export const CraftV: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const gridP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 30 });
  const blockP = spring({ frame: frame - 22, fps, config: SPRING_BOUNCE, durationInFrames: 24 });
  const loaderP = spring({ frame: frame - 42, fps, config: SPRING_BOUNCE, durationInFrames: 24 });
  const resultP = spring({ frame: frame - 190, fps, config: SPRING_BOUNCE, durationInFrames: 32 });
  const captionP = spring({ frame: frame - 250, fps, config: SPRING_SNAP, durationInFrames: 34 });
  const chipsP = spring({ frame: frame - 50, fps, config: SPRING_SNAP, durationInFrames: 28 });

  const LOADERS = ["Fabric", "Forge", "NeoForge", "Quilt"];
  const loaderIdx = frame < 60 ? 0 : frame < 88 ? Math.min(Math.floor((frame - 60) / 8), 3) : 0;

  const out = interpolate(frame, [466, 478], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const arrowP = interpolate(frame, [166, 188], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const s00 = center(0, 0);
  const s11 = center(1, 1);

  return (
    <Backdrop gridOpacity={0.05} glowY={0.3}>
      <div style={{ position: "absolute", inset: 0, opacity: out }}>
        {[0, 1, 2].flatMap((row) =>
          [0, 1, 2].map((col) => {
            const { x, y } = center(col, row);
            const p = spring({ frame: frame - (row * 3 + col) * 2, fps, config: SPRING_SNAP, durationInFrames: 26 });
            return (
              <div
                key={`${row}${col}`}
                style={{
                  position: "absolute",
                  left: x - SLOT / 2,
                  top: y - SLOT / 2,
                  width: SLOT,
                  height: SLOT,
                  borderRadius: 16,
                  background: C.card,
                  border: `2px solid ${C.border}`,
                  transform: `scale(${p})`,
                  opacity: p,
                }}
              />
            );
          })
        )}

        {/* grass block */}
        <div
          style={{
            position: "absolute",
            left: s00.x - 40,
            top: s00.y - 40 - interpolate(blockP, [0, 1], [120, 0]),
            opacity: blockP,
          }}
        >
          <div style={{ width: 80, height: 80, borderRadius: 12, overflow: "hidden", display: "flex", flexDirection: "column", border: `2px solid ${C.borderHi}` }}>
            <div style={{ flex: 0.32, background: "#5EBB4D" }} />
            <div style={{ flex: 0.68, background: "#8A5A3C" }} />
          </div>
        </div>

        {/* loader */}
        <div
          style={{
            position: "absolute",
            left: s11.x - 40,
            top: s11.y - 40 - interpolate(loaderP, [0, 1], [120, 0]),
            opacity: loaderP,
          }}
        >
          <Item label={LOADERS[loaderIdx]} tint={C.blue} />
        </div>

        {/* mods */}
        {MODS.map((m, i) => {
          const target = center(m.col, m.row);
          const p = interpolate(frame, [m.startAt, m.startAt + 28], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          });
          if (p <= 0) return null;
          const arc = Math.sin(p * Math.PI) * -110;
          const x = interpolate(p, [0, 1], [m.from.x, target.x]);
          const y = interpolate(p, [0, 1], [m.from.y, target.y]) + arc;
          return (
            <div key={i} style={{ position: "absolute", left: x - 40, top: y - 40, opacity: Math.min(1, p * 3) }}>
              <Item label={m.label} tint={m.tint} />
            </div>
          );
        })}

        {/* chips */}
        <div style={{ position: "absolute", left: MODRINTH.x - 80, top: MODRINTH.y - 24, opacity: chipsP }}>
          <Chip label="Modrinth" color={C.green} textColor="oklch(0.2 0.05 155)" size={26} />
        </div>
        <div style={{ position: "absolute", left: CURSEFORGE.x - 90, top: CURSEFORGE.y - 24, opacity: chipsP }}>
          <Chip label="CurseForge" color={C.orange} textColor="oklch(0.25 0.06 40)" size={26} />
        </div>

        {/* down arrow */}
        <svg width={80} height={130} style={{ position: "absolute", left: 500, top: 790, opacity: gridP }}>
          <path
            d="M 40 8 L 40 100 M 14 74 L 40 102 L 66 74"
            fill="none"
            stroke={frame > 170 ? C.primary : C.faint}
            strokeWidth={8}
            strokeLinecap="round"
            strokeLinejoin="round"
            pathLength={1}
            strokeDasharray="1"
            strokeDashoffset={1 - arrowP}
          />
        </svg>

        {/* result */}
        <div
          style={{
            position: "absolute",
            left: RESULT.x - 220,
            top: RESULT.y,
            width: 440,
            borderRadius: 24,
            background: C.card,
            border: `2px solid ${C.primary}`,
            padding: "30px 34px",
            opacity: resultP,
            transform: `scale(${interpolate(resultP, [0, 1], [0.5, 1])})`,
            boxShadow: `0 24px 80px oklch(0 0 0 / 0.5), 0 0 70px ${C.primarySoft}`,
          }}
        >
          <div style={{ fontFamily: FONT_SANS, fontSize: 44, fontWeight: 800, color: C.fg, letterSpacing: "-0.02em" }}>
            我的整合包
          </div>
          <div style={{ display: "flex", gap: 10, marginTop: 18 }}>
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
            top: 1420,
            textAlign: "center",
            opacity: captionP,
            transform: `translateY(${interpolate(captionP, [0, 1], [28, 0])}px)`,
          }}
        >
          <Caption size={56}>
            版本 + 加载器 + 包，
            <br />
            <span style={{ color: C.primary }}>合成</span>一个实例
          </Caption>
        </div>
      </div>
    </Backdrop>
  );
};
