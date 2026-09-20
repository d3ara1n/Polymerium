# 端侧翻译模型调研（2026-09-20）

为「汉化包 / 端侧自动翻译」计划查证的外部事实与出处。查证方式：researcher 实测 HF API（`?blobs=true`）与仓库源码。**数字与版本有时效性**，实施前按出处重新核实。

## 1. OPUS-MT（NMT 路线）

- `Helsinki-NLP/opus-mt-en-zh`：Apache-2.0，Marian 架构，约 78M 参数，fp32 权重 ≈ 298 MiB。https://huggingface.co/Helsinki-NLP/opus-mt-en-zh
- ⚠ 常见印象有误：`opus-mt-tc-big-en-zh` 在 HF 上**不存在**（实测 401，命名空间全量列举亦无）；Helsinki 下 en→zh 只有这一个官方模型。
- `Xenova/opus-mt-en-zh`：ONNX 移植版存在（2025-07 重新导出）。int8 全套 ≈ 113MB（encoder 52.9 + decoder merged 60.2），fp16 ≈ 223MB。tokenizer 文件（`source.spm`/`target.spm`/`tokenizer.json`）为通用格式，不绑定 transformers.js；但自回归解码循环（KV cache）需自行实现。佐证：onnxruntime-genai 已合并 Marian 支持（PR #1482，含 C# API）。https://huggingface.co/Xenova/opus-mt-en-zh 、https://github.com/microsoft/onnxruntime-genai/pull/1482

## 2. Firefox Translations / Bergamot（NMT 路线）

- en→zh 简繁均为 Release 状态（Mozilla 2025-08 官宣 CJK 本地翻译）。模型注册表：https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json
- base-memory 架构单模型 ≈ 41.8MB（43.5M 参数），另带 shortlist + SPM vocab，整套 ~45MB。**MPL-2.0**。
- 官方发布物只有 bergamot 专用格式（intgemm 量化 .bin），无 HF Marian / CTranslate2 导出。.NET 可用 `BergamotTranslatorSharp`（Freeesia，NuGet，覆盖 Win/Linux/Mac 原生构建，但版本号 0.x）。https://github.com/Freeesia/BergamotTranslatorSharp

## 3. CTranslate2 的 .NET 现状

- 官方仅 Python / C++；未发现通用、活跃维护的 .NET 绑定。https://opennmt.net/CTranslate2/installation.html

## 4. 端侧小 LLM 路线

- **LLamaSharp**：活跃维护，NuGet 0.27.0（绑定 llama.cpp，已支持 Qwen3.5），netstandard2.0 + net8.0 双目标。后端包：`Backend.Cpu`（Win/Linux，**Mac 包内含 Metal**）、CUDA11/12、Vulkan，均为现成 NuGet，无需自编译。https://github.com/SciSharp/LLamaSharp
- Qwen3 GGUF int4（bartowski 转换）：**1.7B Q4_K_M = 1.28GB**；**4B Q4_K_M = 2.50GB**。两模型均 **Apache-2.0**。https://huggingface.co/bartowski/Qwen_Qwen3-1.7B-GGUF 、https://huggingface.co/bartowski/Qwen_Qwen3-4B-GGUF

## 5. C# 分词器现状（仅 NMT 路线需要）

- `Microsoft.ML.Tokenizers` 2.0.0 稳定版含 `SentencePieceTokenizer`：从 `.spm` 原生文件构造，BPE 与 Unigram 均支持。
- `CreateFromTokenizerJson`（解析 HF `tokenizer.json` 的 Unigram）只在 **3.0.0-preview**，不在 2.0.0 稳定包。PR：https://github.com/dotnet/machinelearning/pull/7390

## 6. 许可证红线

- **NLLB-200-distilled-600M：CC-BY-NC-4.0**——非商业条款，不可随软件内置分发。fp32 2.46GB / int8 ≈ 600MB。https://huggingface.co/facebook/nllb-200-distilled-600M

## 7. MC 生态现状

- 现有自动翻译工具清一色在线 API（Google 非官方接口 / DeepL / 云端 LLM）：AutoTranslator（https://github.com/Pocky-l/AutoTranslator）、AutoTranslation-Next（https://github.com/pkjsjq/AutoTranslation-Next）、Re: Translator、MinecraftModsLocalizer 等。
- MC 生态内无端侧翻译先例；泛游戏领域有 METranslator（OPUS-MT / MADLAD-400 离线翻译，https://github.com/Marwan087/METranslator）。
