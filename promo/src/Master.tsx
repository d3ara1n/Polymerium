import React from "react";
import { AbsoluteFill, Audio, Sequence, staticFile } from "remotion";
import { T } from "./theme";
import { Hook } from "./scenes/Hook";
import { Brand } from "./scenes/Brand";
import { Crafting } from "./scenes/Crafting";
import { Deploy } from "./scenes/Deploy";
import { Features } from "./features";
import { Mcp } from "./scenes/Mcp";
import { Outro } from "./scenes/Outro";

export const Master: React.FC = () => {
  return (
    <AbsoluteFill style={{ backgroundColor: "black" }}>
      <Audio src={staticFile("music-master.wav")} volume={0.9} />
      <Sequence from={T.hook.from} durationInFrames={T.hook.dur}>
        <Hook />
      </Sequence>
      <Sequence from={T.brand.from} durationInFrames={T.brand.dur}>
        <Brand />
      </Sequence>
      <Sequence from={T.crafting.from} durationInFrames={T.crafting.dur}>
        <Crafting />
      </Sequence>
      <Sequence from={T.deploy.from} durationInFrames={T.deploy.dur}>
        <Deploy />
      </Sequence>
      <Sequence from={T.features.from} durationInFrames={T.features.dur}>
        <Features />
      </Sequence>
      <Sequence from={T.mcp.from} durationInFrames={T.mcp.dur}>
        <Mcp />
      </Sequence>
      <Sequence from={T.outro.from} durationInFrames={T.outro.dur}>
        <Outro />
      </Sequence>
    </AbsoluteFill>
  );
};
