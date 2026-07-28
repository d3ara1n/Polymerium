# Polymerium Promo Video

Polymerium 宣传片的 "video as code" 工程，基于 [Remotion](https://remotion.dev) 4。
所有画面为代码生成的 motion design（含真实界面截图合成），配乐由 `scripts/make-music.mjs`
程序化合成（PCM 直写 WAV，无版权风险），剪辑点全部落在 120 BPM 网格上。

## 产出

| Composition | 规格 | 时长 | 用途 |
|---|---|---|---|
| `Master` | 1920×1080 @60fps | 78s | B 站 / YouTube 主片 |
| `Teaser` | 1920×1080 @60fps | 15s | 预告 / 动态 |
| `Short` | 1080×1920 @60fps | 30s | 竖屏短视频 |

渲染产物（gitignored，在 `out/`）：`master.mp4`、`teaser.mp4`、`short.mp4`。

## 命令

```bash
npm i                          # 首次
npm run dev                    # Remotion Studio 实时预览
node scripts/make-music.mjs    # 重新生成三条音轨到 public/
npx remotion render Master out/master.mp4 --crf=18
npx remotion render Teaser out/teaser.mp4 --crf=18
npx remotion render Short out/short.mp4 --crf=18
npx remotion still Master out/frame.png --frame=1200   # 单帧检查
```

## 结构

- `src/theme.ts` — 品牌色（oklch 琥珀，与 website 一致）、BPM 网格、Master 时间轴
- `src/fonts.ts` — Geist / Geist Mono / Noto Sans SC（`@remotion/google-fonts`）
- `src/components/` — `Backdrop`（网格+辉光+暗角）、`Logo`、`bits`（Chip/FileIcon/Caption…）
- `src/scenes/` — Master 各幕：Hook（复制灾难→profile.json）、Brand（动词切换）、
  Crafting（合成台隐喻）、Deploy（符号链接芭蕾）、Mcp（终端反转）、Outro
- `src/features/` — 6 个 feature beat（01 市场 / 02 秒级更新 / 03 依赖视图 / 04 依赖图谱 / 05 快照 / 06 Git 友好），
  共用 `FeatureShell` 左文右图版式
- `src/scenes/vertical/` — Short 竖屏重排版（HookV / CraftV / DeployV）；Flash 与 Outro 直接复用
- `src/Teaser.tsx` — 15s 快剪：坍缩 → 品牌 → 特性快闪 → 收尾
  （用负 `from` 的嵌套 Sequence 让 Hook 从坍缩前一刻切入）
- `public/screenshots/` — 从 `website/public/screenshots` 复制的真实界面截图

## 迭代提示

- 改文案/配色基本只动 `theme.ts` 与对应 scene；场景间是节拍硬切，改时长保持
  `theme.ts` 的 `T` 与音轨编排（`scripts/make-music.mjs` 的段落秒数）同步。
- 音轨段落与场景边界一一对应（如 impact 对齐坍缩、72s 对齐片尾），改一边必须改另一边。
- 配乐情绪弧线（D 大调）：hook 蓄势 → brand 释放 → crafting 轻快 → deploy 流动（结尾抽空）→
  features 明快（快照段喘息）→ mcp 克制 → outro 昂扬；改情绪对照 `scripts/make-music.mjs` 顶部注释。
- 中文为默认语言；要做英文版时复制 scene 文案参数化即可，版式已按混排设计。
