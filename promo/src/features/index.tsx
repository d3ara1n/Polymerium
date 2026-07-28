import React from "react";
import { Sequence } from "remotion";
import { FeatureShell } from "./Shell";
import { MarketBeat, UpdateBeat } from "./BeatsA";
import { DependencyBeat, GraphBeat, SnapshotBeat } from "./BeatsB";
import { GitBeat } from "./BeatsC";
import { C } from "../theme";

const BEAT_DUR = 300;

const Accent: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <span style={{ color: C.primary }}>{children}</span>
);

const BEATS: {
  index: string;
  title: React.ReactNode;
  sub: string;
  node: React.ReactNode;
}[] = [
  {
    index: "01",
    title: <>整合包<Accent>市场</Accent></>,
    sub: "Modrinth 与 CurseForge，浏览、安装、更新，不出启动器",
    node: <MarketBeat />,
  },
  {
    index: "02",
    title: <><Accent>秒级</Accent>更新</>,
    sub: "整合包更新只改元数据，不搬文件，存档配置原样保留",
    node: <UpdateBeat />,
  },
  {
    index: "03",
    title: <>依赖<Accent>视图</Accent></>,
    sub: "依赖一目了然，原地添加、管理，不离开当前页面",
    node: <DependencyBeat />,
  },
  {
    index: "04",
    title: <>依赖<Accent>图谱</Accent></>,
    sub: "整个实例的包关系与缺失依赖，一张图看懂",
    node: <GraphBeat />,
  },
  {
    index: "05",
    title: <>快照<Accent>系统</Accent></>,
    sub: "保存、恢复、对比完整状态，放心尝试任何改动",
    node: <SnapshotBeat />,
  },
  {
    index: "06",
    title: <>Git <Accent>友好</Accent></>,
    sub: "实例就是一个 JSON，像代码一样版本管理整合包",
    node: <GitBeat />,
  },
];

export const Features: React.FC = () => {
  return (
    <>
      {BEATS.map((b, i) => (
        <Sequence key={i} from={i * BEAT_DUR} durationInFrames={BEAT_DUR}>
          <FeatureShell index={b.index} title={b.title} sub={b.sub}>
            {b.node}
          </FeatureShell>
        </Sequence>
      ))}
    </>
  );
};
