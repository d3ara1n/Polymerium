import React from "react";
import { AbsoluteFill, Audio, Sequence, staticFile } from "remotion";
import { FPS } from "./theme";
import { HookV } from "./scenes/vertical/HookV";
import { CraftV } from "./scenes/vertical/CraftV";
import { DeployV } from "./scenes/vertical/DeployV";
import { Flash, FLASH_ITEMS } from "./scenes/Flash";
import { Outro } from "./scenes/Outro";

// 30s vertical cut: collapse → craft → deploy → feature flash → outro
export const Short: React.FC = () => {
  return (
    <AbsoluteFill style={{ backgroundColor: "black" }}>
      <Audio src={staticFile("music-short.wav")} volume={0.9} />
      <Sequence from={0} durationInFrames={4 * FPS}>
        <HookV />
      </Sequence>
      <Sequence from={4 * FPS} durationInFrames={8 * FPS}>
        <CraftV />
      </Sequence>
      <Sequence from={12 * FPS} durationInFrames={6 * FPS}>
        <DeployV />
      </Sequence>
      <Sequence from={18 * FPS} durationInFrames={5.5 * FPS}>
        <Flash items={FLASH_ITEMS.slice(0, 6)} every={55} size={120} />
      </Sequence>
      <Sequence from={23.5 * FPS} durationInFrames={6.5 * FPS}>
        <Outro />
      </Sequence>
    </AbsoluteFill>
  );
};
