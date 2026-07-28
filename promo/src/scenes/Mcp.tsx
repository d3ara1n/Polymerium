import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Caption, Sub } from "../components/bits";
import { C, FONT_MONO, SPRING_SNAP } from "../theme";

const CMD = "trident --mcp";

const LINES: { at: number; node: React.ReactNode }[] = [
  { at: 78, node: <span style={{ color: C.codeString }}>✓ MCP server 已就绪 · stdio</span> },
  {
    at: 116,
    node: (
      <span>
        <span style={{ color: C.primary }}>▸ </span>
        <span style={{ color: C.muted }}>instance.create</span>
        <span style={{ color: C.codePunct }}>(</span>
        <span style={{ color: C.codeString }}>"ATM9 魔改"</span>
        <span style={{ color: C.codePunct }}>)</span>
      </span>
    ),
  },
  {
    at: 158,
    node: (
      <span>
        <span style={{ color: C.primary }}>▸ </span>
        <span style={{ color: C.muted }}>package.add</span>
        <span style={{ color: C.codePunct }}>(</span>
        <span style={{ color: C.codeString }}>"modrinth:sodium"</span>
        <span style={{ color: C.codePunct }}>)</span>
      </span>
    ),
  },
  {
    at: 200,
    node: (
      <span>
        <span style={{ color: C.primary }}>▸ </span>
        <span style={{ color: C.muted }}>deploy.build</span>
        <span style={{ color: C.codePunct }}>(</span>
        <span style={{ color: C.codeString }}>"ATM9 魔改"</span>
        <span style={{ color: C.codePunct }}>)</span>
      </span>
    ),
  },
  { at: 246, node: <span style={{ color: C.codeString }}>✓ 部署完成 · 启动游戏</span> },
];

export const Mcp: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const titleP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const termP = spring({ frame: frame - 10, fps, config: SPRING_SNAP, durationInFrames: 40 });

  const typed = Math.floor(Math.max(0, frame - 26) / 1.6);
  const shownCmd = CMD.slice(0, typed);
  const cmdDone = typed >= CMD.length;

  const out = interpolate(frame, [464, 476], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <Backdrop gridOpacity={0.045} glowX={0.7}>
      <div style={{ position: "absolute", inset: 0, opacity: out }}>
        {/* left copy */}
        <div style={{ position: "absolute", left: 140, top: 300, width: 700 }}>
          <div style={{ opacity: titleP, transform: `translateY(${interpolate(titleP, [0, 1], [40, 0])}px)` }}>
            <Caption size={80} weight={900} style={{ letterSpacing: "-0.03em", lineHeight: 1.2 }}>
              唯一内置 <span style={{ color: C.primary }}>MCP</span> 的
              <br />
              Minecraft 启动器
            </Caption>
          </div>
          <div
            style={{
              marginTop: 34,
              opacity: spring({ frame: frame - 14, fps, config: SPRING_SNAP, durationInFrames: 36 }),
            }}
          >
            <Sub size={32}>30+ 工具，让 AI 完成整个实例工作流</Sub>
          </div>
        </div>

        {/* terminal */}
        <div
          style={{
            position: "absolute",
            left: 930,
            top: 250,
            width: 850,
            borderRadius: 18,
            overflow: "hidden",
            boxShadow: `0 0 0 1px ${C.border}, 0 40px 110px oklch(0 0 0 / 0.6)`,
            opacity: termP,
            transform: `translateY(${interpolate(termP, [0, 1], [46, 0])}px)`,
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 9,
              padding: "15px 20px",
              background: "oklch(0.22 0.012 62)",
              borderBottom: `1px solid ${C.border}`,
            }}
          >
            {[0, 1, 2].map((i) => (
              <div key={i} style={{ width: 14, height: 14, borderRadius: 9999, background: C.cardHi }} />
            ))}
            <div style={{ marginLeft: 14, fontFamily: FONT_MONO, fontSize: 20, color: C.faint }}>
              trident — MCP
            </div>
          </div>
          <div
            style={{
              background: "oklch(0.175 0.012 62)",
              padding: "30px 32px 40px",
              fontFamily: FONT_MONO,
              fontSize: 26,
              lineHeight: 2.05,
              minHeight: 420,
            }}
          >
            <div>
              <span style={{ color: C.primary }}>$ </span>
              <span style={{ color: C.fg }}>{shownCmd}</span>
              {!cmdDone && <span style={{ color: C.primary }}>▌</span>}
            </div>
            {LINES.map((l, i) => {
              const p = spring({ frame: frame - l.at, fps, config: SPRING_SNAP, durationInFrames: 22 });
              if (p <= 0) return null;
              return (
                <div key={i} style={{ opacity: p, transform: `translateY(${interpolate(p, [0, 1], [14, 0])}px)` }}>
                  {l.node}
                </div>
              );
            })}
            {cmdDone && frame > 262 && (
              <span style={{ color: C.primary, opacity: Math.floor(frame / 16) % 2 === 0 ? 1 : 0 }}>▌</span>
            )}
          </div>
        </div>
      </div>
    </Backdrop>
  );
};
