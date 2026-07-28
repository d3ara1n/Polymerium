import "./index.css";
import "./fonts";
import React from "react";
import { Composition } from "remotion";
import { FPS, MASTER_DUR } from "./theme";
import { Master } from "./Master";
import { Teaser } from "./Teaser";
import { Short } from "./Short";

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="Master"
        component={Master}
        durationInFrames={MASTER_DUR}
        fps={FPS}
        width={1920}
        height={1080}
      />
      <Composition
        id="Teaser"
        component={Teaser}
        durationInFrames={15 * FPS}
        fps={FPS}
        width={1920}
        height={1080}
      />
      <Composition
        id="Short"
        component={Short}
        durationInFrames={30 * FPS}
        fps={FPS}
        width={1080}
        height={1920}
      />
    </>
  );
};
