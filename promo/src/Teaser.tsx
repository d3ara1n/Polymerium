import React from "react";
import { AbsoluteFill, Audio, Sequence, staticFile } from "remotion";
import { FPS } from "./theme";
import { Hook } from "./scenes/Hook";
import { Brand } from "./scenes/Brand";
import { Flash } from "./scenes/Flash";
import { OutroMini } from "./scenes/OutroMini";

// 15s cut: collapse → brand → feature flash cards → mini outro
export const Teaser: React.FC = () => {
  return (
    <AbsoluteFill style={{ backgroundColor: "black" }}>
      <Audio src={staticFile("music-teaser.wav")} volume={0.9} />
      {/* hook, starting right before the collapse (local frame 300) */}
      <Sequence from={0} durationInFrames={2.5 * FPS}>
        <Sequence from={-300}>
          <Hook />
        </Sequence>
      </Sequence>
      <Sequence from={2.5 * FPS} durationInFrames={3 * FPS}>
        <Brand />
      </Sequence>
      <Sequence from={5.5 * FPS} durationInFrames={6.5 * FPS}>
        <Flash />
      </Sequence>
      <Sequence from={12 * FPS} durationInFrames={3 * FPS}>
        <OutroMini />
      </Sequence>
    </AbsoluteFill>
  );
};
