# 内存占用检测方法论

> 定位：Polymerium（Avalonia + Trident.Core 桌面端）内存 profile 的操作规程与判读手册。面向"用户反馈/直觉认为内存偏高，如何定位到底高在哪"。随依赖演进维护：更换图片加载、缓存或渲染方案时同步更新嫌疑点清单。

## 0. 前置认知：内存的三个去向

Avalonia 应用的内存不都在托管堆上，先分清三个去向，工具才选得对：

| 去向 | 典型内容 | 能否被 gcdump 看到 |
|------|----------|--------------------|
| 托管堆 | ViewModel、集合、委托、STJ 产物 | ✅ |
| Native 堆（.NET 侧） | GC 自身结构、JIT 代码 | ❌ |
| Native 堆（三方库） | Skia 位图/表面、BlurBackdrop、sqlite page cache、libgit2 | ❌ |

判读总开关：**Working Set − GC Heap Size ≈ native 侧**。若 working set 很高而托管堆很小，堆快照无罪，问题在渲染面或 native 库，别在 gcdump 里白费功夫。

## 1. 工具准备

```bash
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-gcdump
```

被测对象必须用 **Release** 构建（Debug 带 console logging 和诊断包，轮廓不同）。可直接跑 Release 产物；若同时要验证 trim 产物，发布后跑 `Publish/<rid>/` 下的可执行文件，一举两得：

```bash
dotnet publish src/Polymerium.Avalonia/Polymerium.Avalonia.csproj -c Release -r <rid> --self-contained
```

## 2. dotnet-counters：实时趋势

```bash
dotnet-counters ps                                        # 拿 PID
dotnet-counters monitor -p <PID> --counters System.Runtime
```

留档改用 `collect -o run.csv --format csv`。

关键指标与判读：

| 指标 | 判读 |
|------|------|
| GC Heap Size | 托管堆总量，是 gcdump 能覆盖的部分 |
| Gen 2 Size | 闲置后不回落 = 对象被长期持有（缓存或订阅泄漏） |
| LOH Size | >85KB 对象；图片解码的 `byte[]` 全在这 |
| Allocation Rate | 稳态应趋近 0；持续高位 = 热路径疯狂分配 |
| % Time in GC | 长期 >10% = GC 压力大 |
| Working Set | 与 GC Heap 的差值即 native 侧（见 §0） |

## 3. dotnet-gcdump：堆快照

```bash
dotnet-gcdump collect -p <PID>                            # 生成 *.gcdump
dotnet-gcdump report <file>.gcdump | head -50             # 按类型聚合的大小/数量排行
```

- gcdump 会触发一次完整 GC（进程短暂停顿），**别在部署进行中抓**。
- 可视化用 Visual Studio（Windows）或 Rider 直接打开 `.gcdump`。
- 两份快照 diff：`dotnet-gcdump report a.gcdump > a.txt` 后 diff 文本。

## 4. 分阶段协议

单点数字没意义，增长曲线才有。每阶段末尾记一行 counters + 抓一个 gcdump：

| 阶段 | 操作 | 目的 |
|------|------|------|
| A 空载 | 启动 → 首页闲置 5 分钟 | 基线（等更新检查/快照扫描跑完） |
| B 浏览 | 市场页滚动图片列表几分钟、开关几个详情 | 压 AsyncImageLoader + FusionCache + DynamicData |
| C 回落 | 回首页再闲置 5 分钟 | gen2/LOH 回不回落；不回落 = 被持有 |
| D 部署 | 部署并启动一个实例 | 压 Trident 部署管线（文件流、zip、事件） |

判读：**B−A 看泄漏速度，C vs B 看回收能力，D−C 看重操作残留。**

## 5. 本应用嫌疑点（按可能性排序）

1. **图片解码** — AsyncImageLoader 对市场缩略图若按原始尺寸解码，LOH 直接爆炸。先查有没有限制解码尺寸。
2. **Native 渲染面** — working set 高、heap 低 → Skia 表面、BlurBackdrop（带 P/Invoke）。优化方向在渲染面，不在堆。
3. **事件/订阅泄漏** — dump 里 ViewModel/委托链持续增长 → `OnDeinitializeAsync` 漏退订（纪律对照 `LandingPageModel`）。
4. **FusionCache 内存层** — `Startup` 里 `AddMemoryCache` 设了 `SizeLimit=256`（entries，且每个 entry 必须设 Size 才生效），但 FusionCache 自身 L1 上限需单独确认。

## 6. 测出结果后的调参入口

- `System.GC.HeapHardLimitPercent`（runtimeconfig）：压堆上限换更激进回收。
- 保持 workstation GC，不要开 server GC（桌面单进程无收益）。
- 按 profile 结果给 FusionCache L1 定条目/内存上限，给图片解码定尺寸上限。
