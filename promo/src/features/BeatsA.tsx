import React from "react";
import { Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { Chip } from "../components/bits";
import { C, FONT_MONO, FONT_SANS, SPRING_BOUNCE, SPRING_SNAP } from "../theme";

const VIS = { left: 990, top: 210, width: 800, height: 660 };

/* ── 01 整合包市场 ── */

export const MarketBeat: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const shotP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 40 });
  const zoom = interpolate(frame, [0, 300], [1.09, 1.0]);
  const chip1 = spring({ frame: frame - 42, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const chip2 = spring({ frame: frame - 56, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const btn = spring({ frame: frame - 80, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const installed = frame > 130;

  return (
    <div style={{ position: "absolute", ...VIS }}>
      <div
        style={{
          position: "absolute",
          inset: 0,
          opacity: shotP,
          transform: `perspective(1400px) rotateY(-9deg) rotateX(2.5deg) scale(${zoom}) translateY(${interpolate(shotP, [0, 1], [50, 0])}px)`,
          transformOrigin: "center",
          borderRadius: 22,
          overflow: "hidden",
          border: `1.5px solid ${C.borderHi}`,
          boxShadow: "0 40px 110px oklch(0 0 0 / 0.55)",
        }}
      >
        <Img src={staticFile("screenshots/marketplace.webp")} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
      </div>
      <div style={{ position: "absolute", left: -30, top: 40, transform: `scale(${chip1})`, opacity: chip1 }}>
        <Chip label="Modrinth" color={C.green} textColor="oklch(0.2 0.05 155)" size={30} />
      </div>
      <div style={{ position: "absolute", left: -10, top: 116, transform: `scale(${chip2})`, opacity: chip2 }}>
        <Chip label="CurseForge" color={C.orange} textColor="oklch(0.25 0.06 40)" size={30} />
      </div>
      <div style={{ position: "absolute", right: 20, bottom: 46, transform: `scale(${btn})`, opacity: btn }}>
        <Chip
          label={installed ? "✓ 已安装" : "安装"}
          color={installed ? C.primary : C.cardHi}
          textColor={installed ? "oklch(0.26 0.06 75)" : C.fg}
          size={30}
        />
      </div>
    </div>
  );
};

/* ── 02 秒级更新 ── */

export const UpdateBeat: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const cardP = spring({ frame, fps, config: SPRING_SNAP, durationInFrames: 36 });
  const bannerP = spring({ frame: frame - 40, fps, config: SPRING_BOUNCE, durationInFrames: 28 });
  const clicked = frame >= 96;
  const metaP = interpolate(frame, [100, 112], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const barP = interpolate(frame, [108, 138], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const flipP = spring({ frame: frame - 148, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const doneP = spring({ frame: frame - 158, fps, config: SPRING_BOUNCE, durationInFrames: 26 });
  const keepP = spring({ frame: frame - 190, fps, config: SPRING_SNAP, durationInFrames: 28 });

  return (
    <div style={{ position: "absolute", ...VIS, top: 240 }}>
      {/* update banner */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 18,
          marginBottom: 24,
          opacity: bannerP * (clicked ? 0 : 1),
          transform: `translateY(${interpolate(bannerP, [0, 1], [-22, 0])}px)`,
          height: 62,
        }}
      >
        <Chip label="发现新版本 14.1.0" color={C.cardHi} textColor={C.fg} size={26} />
        <Chip label="更新" color={C.primary} textColor="oklch(0.26 0.06 75)" size={26} />
      </div>
      {clicked && <div style={{ marginBottom: 24, height: 62 }} />}

      {/* instance card */}
      <div
        style={{
          borderRadius: 24,
          background: C.card,
          border: `1.5px solid ${C.borderHi}`,
          padding: "36px 40px",
          opacity: cardP,
          transform: `translateY(${interpolate(cardP, [0, 1], [44, 0])}px)`,
          boxShadow: "0 30px 90px oklch(0 0 0 / 0.45)",
        }}
      >
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div>
            <div style={{ fontFamily: FONT_SANS, fontSize: 40, fontWeight: 800, color: C.fg, letterSpacing: "-0.02em" }}>
              Fabulously Optimized
            </div>
            <div style={{ fontFamily: FONT_MONO, fontSize: 22, color: C.muted, marginTop: 10 }}>
              1.21.4 · Fabric · 导入自 Modrinth
            </div>
          </div>
          <div style={{ transform: `scale(${frame < 148 ? 1 : flipP})`, opacity: frame < 148 ? 1 : flipP }}>
            <Chip
              label={frame < 148 ? "14.0.0" : "14.1.0"}
              mono
              size={26}
              color={frame < 148 ? C.cardHi : C.primarySoft}
              textColor={frame < 148 ? C.fg : C.primary}
            />
          </div>
        </div>

        {/* update progress */}
        <div style={{ height: 14, borderRadius: 9999, background: C.cardHi, marginTop: 30, overflow: "hidden", opacity: clicked ? 1 : 0 }}>
          <div style={{ width: `${barP * 100}%`, height: "100%", borderRadius: 9999, background: C.primary }} />
        </div>
        {clicked && (
          <div style={{ marginTop: 14, opacity: metaP, display: "flex", flexDirection: "column", gap: 6 }}>
            <div style={{ fontFamily: FONT_MONO, fontSize: 22, color: C.red }}>
              - "version": "14.0.0"
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ fontFamily: FONT_MONO, fontSize: 22, color: C.codeString }}>
                + "version": "14.1.0"
              </span>
              <span style={{ fontFamily: FONT_MONO, fontSize: 22, color: C.faint }}>仅元数据变更</span>
            </div>
          </div>
        )}
      </div>

      {/* done + preserved */}
      <div style={{ display: "flex", gap: 14, marginTop: 24, alignItems: "center" }}>
        <div style={{ transform: `scale(${doneP})`, opacity: doneP }}>
          <Chip label="✓ 更新完成 · 0.8s" color={C.primary} textColor="oklch(0.26 0.06 75)" size={26} />
        </div>
        <div style={{ opacity: keepP, transform: `translateY(${interpolate(keepP, [0, 1], [16, 0])}px)` }}>
          <Chip label="存档与配置原样保留" color={C.cardHi} textColor={C.muted} size={24} />
        </div>
      </div>
    </div>
  );
};
