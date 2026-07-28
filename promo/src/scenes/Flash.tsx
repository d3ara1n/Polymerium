import React from "react";
import { spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Backdrop } from "../components/Backdrop";
import { Sub } from "../components/bits";
import { C, FONT_SANS, SPRING_BOUNCE } from "../theme";

export type FlashItem = { big: React.ReactNode; sub: string };

const A: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <span style={{ color: C.primary }}>{children}</span>
);

export const FLASH_ITEMS: FlashItem[] = [
  { big: <>整合包<A>市场</A></>, sub: "Modrinth · CurseForge" },
  { big: <><A>秒级</A>更新</>, sub: "整合包更新只改元数据" },
  { big: <>依赖<A>视图</A></>, sub: "原地添加、管理依赖" },
  { big: <>依赖<A>图谱</A></>, sub: "包关系一张图看懂" },
  { big: <>快照<A>系统</A></>, sub: "放心尝试任何改动" },
  { big: <>Git <A>友好</A></>, sub: "整合包即 JSON" },
  { big: <>内置 <A>MCP</A></>, sub: "让 AI 完成整个工作流" },
];

export const Flash: React.FC<{
  items?: FlashItem[];
  every?: number;
  size?: number;
}> = ({ items = FLASH_ITEMS, every = 55, size = 128 }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const idx = Math.min(Math.floor(frame / every), items.length - 1);
  const local = frame - idx * every;
  const p = spring({ frame: local, fps, config: SPRING_BOUNCE, durationInFrames: 22 });

  return (
    <Backdrop gridOpacity={0.05} glowX={0.5 + (idx % 3) * 0.14 - 0.14}>
      <div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          gap: 34,
        }}
      >
        <div
          style={{
            fontFamily: FONT_SANS,
            fontSize: size,
            fontWeight: 900,
            letterSpacing: "-0.03em",
            color: C.fg,
            opacity: p,
            transform: `scale(${0.86 + p * 0.14})`,
          }}
        >
          {items[idx].big}
        </div>
        <div style={{ opacity: p }}>
          <Sub size={size * 0.27}>{items[idx].sub}</Sub>
        </div>
      </div>
    </Backdrop>
  );
};
