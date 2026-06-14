# 本地皮肤渲染接入计划

## 概述

用自建的 SkiaSharp 3D 皮肤渲染器替换 `AccountHelper` 中硬编码的外部皮肤服务（starlightskins、mineatar.io），让所有账户类型的皮肤预览（头像、全身图、四方向旋转预览）全部在本地离线渲染。

**核心收益**：authlib-injector 账户不再回落到 Steve（第三方服务拿不到外置验证服务器的皮肤），且全部账户类型离线可见、无隐私外泄。

---

## 关键决策（已定）

| 维度 | 决策 | 备注 |
|------|------|------|
| 接管范围 | **全部 4 种账户类型**（Microsoft / Authlib / Trial / Offline） | 当前不实现，仅记录计划 |
| UI 接入方式 | **自定义 `poly://` URI scheme，由 `AppImageLoader` 拦截** | `AccountHelper.Get*Url` 返回 `poly://skin?...` 字符串，渲染按需触发，绑定层零改动 |
| 渲染引擎 | **自建 SkiaSharp**（无三方渲染依赖） | 仅依赖 `SkiaSharp` NuGet，已加入 csproj |

---

## ✅ 已完成：本地皮肤渲染器

位置：`src/Polymerium.Avalonia/Rendering/`（4 文件，456 行，纯 SkiaSharp）

| 文件 | 行 | 职责 |
|------|----|----|
| `SkinFormat.cs` | 59 | 皮肤格式检测：Classic / Slim / Legacy（含 slim 的透明像素判定） |
| `SkinGeometry.cs` | 199 | 6 部位 box 几何 + UV 模板 + 全身/头像网格装配。UV 与几何参数已逐项核对 `Coloryr/MinecraftSkinRender` 源码 |
| `SkinCamera.cs` | 72 | 等距投影：绕 Y/X 旋转 + 自适应缩放居中 + Y 翻转 |
| `SkinRenderer.cs` | 126 | 门面 API + 光栅化（SKVertices 仿射纹理映射 + 背面剔除 + 深度排序 + base/overlay 分层） |

### 公开 API

```csharp
public enum SkinViewType { Face, Body, Cover, Front, Right, Back, Left }

public sealed class SkinRenderer
{
    // 3D 头像（默认 yaw=0, pitch=0，正面对着镜头）
    public SKImage RenderHead(SKBitmap skin, float yawDeg = 0f, float pitchDeg = 0f, int size = 256);

    // 3D 全身（默认等距 yaw=45, pitch=-22；includeLegs=false 时截断双腿为上半身）
    public SKImage RenderBody(SKBitmap skin, float yawDeg = 45f, float pitchDeg = -22f,
                              bool includeLegs = true, int width = 210, int height = 420);

    // 四方向全身正视（Front→Right→Back→Left，pitch=0，yaw=0/90/180/270）
    public IReadOnlyList<SKImage> RenderBodyViews(SKBitmap skin, int width = 210, int height = 420);

    // 按 SkinViewType 调度（SkinRenderService 调用入口）：
    //   Face=正视头、Body=等距全身、Cover=等距上半身、Front/Right/Back/Left=正视全身
    public SKImage Render(SKBitmap skin, SkinViewType view);
}
```

> `Render(SKBitmap, SkinViewType)` 是 SkinRenderService 的调用入口，按视图类型映射到具体 yaw/pitch/includeLegs。详见上「URL Scheme 设计」的 type 参数表。

### 验证结论

用纯色测试皮肤（每部位一种颜色）做过决定性验证：

- ✅ 6 部位 UV 全对（head=红 / body=绿 / 右臂=蓝 / 左臂=黄 / 右腿=紫 / 左腿=青）
- ✅ 左右对称正确（屏幕左 = 模型 x- = Right 部件；屏幕右 = 模型 x+ = Left 部件）
- ✅ Alex 经典配色全对（橙发 #EA9641 / 绿衫 #568553 / 深裤 #2E2E2E）
- ✅ overlay 外层向外放大 1.125× 正确
- ✅ Classic / Slim / Legacy 格式自动识别
- ✅ 视角修正：face 与四方位为 pitch=0 的 3D 正视（保留立体厚度不倾斜），body 为等距完整全身，cover 为等距上半身特写。四方位 yaw={0,90,180,270} 方向验证通过（front 左右臂正确、right 见右身、back 见背、left 见左身，旋转连贯）

### 关键技术参数（已溯源核对，勿轻易改动）

- UV 坐标系：`SKShader.CreateBitmap` 使用**像素坐标**而非归一化坐标
- 背面剔除：`SignedArea < 0` = 正面朝向
- 深度排序：painter's algorithm，base 先画再画 overlay，各自按深度升序
- 部位 pivot（全身装配）：Head `T(0,1.25,0)`、Body 单位矩阵、LeftArm classic `T(0.75,0,0)` / slim `T(0.6875,0,0)`、Legs `T(±0.25,-1.5,0)`
- overlay 缩放：1.125×

### 依赖

- NuGet `SkiaSharp` 3.119.4（已在 `src/Polymerium.Avalonia/Polymerium.Avalonia.csproj`，版本对齐 Avalonia 12.0. 的传递依赖）
- 构建有 1 条 `FilterQuality` 过时警告，属预期构建噪音

---

## ⏳ 待完成：全盘接入

### poly:// 拦截方案

#### 设计动机（为何不用临时文件）

临时文件方案存在「谁生成文件」的鸡生蛋死结：`AccountHelper` 是静态工具类无法注入渲染服务、渲染是异步的但 `AccountModel` 构造时同步要 `Uri`、还需自建缓存失效与异步回填机制。

调研发现项目已有**全局图片加载器 `AppImageLoader`**（`src/Polymerium.Avalonia/AppImageLoader.cs`），它是所有 `async:ImageLoader.Source` 绑定的唯一入口（`Program.cs:109` → `ImageLoader.AsyncImageLoader = loader`），且**自带 LRU `MemoryCache`（256 项 / 30 分钟滑动过期）**。在它的 `ProvideImageAsync` 入口拦截 `poly://` 前缀，即可让渲染按需触发、缓存天然复用、异步无回填问题。

#### 数据流

```
AccountModel 构造
  └─ AccountHelper.GetBodyUrl(account) 返回 "poly://skin?type=body&src=..."   ← 纯字符串拼接，同步、无 IO
      └─ XAML: async:ImageLoader.Source="{Binding BodyUrl}"
          └─ AppImageLoader.ProvideImageAsync("poly://skin?...")               ← ImageLoader 按需触发
              ├─ MemoryCache 命中？→ 直接返回 Bitmap
              └─ 未命中 → SkinRenderService:
                  解析 url → 按 src 前缀路由 → 下载/取内置 skin PNG
                  → SKBitmap.Decode → SkinFormat.Detect → SkinRenderer.Render*
                  → SKImage → Avalonia Bitmap
                  → 写入 MemoryCache(key = poly:// url) → 返回
```

#### URL Scheme 设计

```
poly://skin?type={view}&src={数据源}[&format={auto|slim|classic}]
```

**`type`（渲染类型）**：`face` | `body` | `cover` | `front` | `right` | `back` | `left`

| type | yaw | pitch | 含腿 | 效果 |
|------|-----|-------|------|------|
| `face` | 0 | 0 | — | 3D 头部正视 |
| `body` | 45 | -22 | ✅ | 等距完整全身（右上俯视） |
| `cover` | 45 | -22 | ❌ | 等距上半身特写（截断腿，适合方形封面） |
| `front` | 0 | 0 | ✅ | 3D 正面全身 |
| `right` | 90 | 0 | ✅ | 3D 右侧全身 |
| `back` | 180 | 0 | ✅ | 3D 背面全身 |
| `left` | 270 | 0 | ✅ | 3D 左侧全身 |

**`src`（数据源，按前缀路由）**：

| src 形态 | 数据来源 | format 来源 | 适用账户 |
|----------|---------|------------|---------|
| `mojang:{uuid}` | 走 Mojang sessionserver profile API 查 skin | API metadata 自动给出 | Microsoft |
| `asset:Steve` / `asset:Alex` | 内置默认 PNG | 固定（Steve=Classic, Alex=Slim） | Trial / Offline |
| 任何 http(s) URL（无上述前缀） | 直接当裸展开图下载（万能 fallback） | 默认 `auto`（`SkinFormat.Detect` 自动检测，已验证可靠），可 `&format=slim` 强制 | Authlib（用 `AuthlibAccount.SkinUrl`） |

> 设计要点：
> - **uuid 退化为 `mojang:` 路由的子参数**——有 src 后 uuid 不再是主键。
> - **SkinRenderService 完全无状态**——所有信息编进 url，无注册表、无生命周期管理。
> - **缓存 key = 完整 url**——skin 变了（SkinUrl 变）→ url 变 → 自动重渲染，复用 `AppImageLoader` 的 MemoryCache。
> - **format 智能化**——mojang 走 API metadata、url 走自动检测兜底、asset 固定，三者各得其所。

#### 各账户类型生成的实际 url

```
Microsoft:    poly://skin?type=body&src=mojang:{uuid}
              poly://skin?type=front&src=mojang:{uuid}   （四方向各一个）

Authlib:      poly://skin?type=body&src={UrlEncode(SkinUrl)}
              （SkinUrl 来自 AuthlibAccount，如 https://cdn.xxx/skin.png）

Trial/Offline: poly://skin?type=body&src=asset:Steve
```

#### 三个实现落点

1. **新增 `SkinRenderService`**（`src/Polymerium.Avalonia/Services/` 或 `Facilities/`）：解析 `poly://` url → 按 `src` 前缀路由 → 下载/取内置 PNG → `SKBitmap.Decode` → `SkinFormat.Detect` → `SkinRenderer.Render*` → `SKImage` → `Avalonia.Media.Imaging.Bitmap`。需注入 `HttpClient`（已有）与 `SkinRenderer`（新建实例）。Mojang 路由需查询能力（见数据源表）。
2. **改 `AppImageLoader`**（`src/Polymerium.Avalonia/AppImageLoader.cs`）：构造函数注入 `SkinRenderService`，`ProvideImageAsync` 入口加分支——`if (url.StartsWith("poly://skin")) return await _skinService.RenderAsync(url);`。MemoryCache 在外层（基类 / 现有逻辑），poly:// 与 http:// 共享同一缓存实例，无需额外接线。
3. **改 `AccountHelper`**（`src/Polymerium.Avalonia/Utilities/AccountHelper.cs`）：`Get*Url` 改为按账户类型拼接 `poly://` url（同步纯字符串，无 IO），删除 starlightskins/mineatar 常量与 URL 拼接。

---

### 取代点地图（完整文件清单）

> 以下行号基于接入调查时的代码状态（2026-06-13），实施前请重新核对。

#### 🔴 核心源头 — `src/Polymerium.Avalonia/Utilities/AccountHelper.cs`

| 位置 | 现状 | 取代动作 |
|------|------|----------|
| L15-21 | `STEVE_FACE_URL` / `STEVE_BODY_URL` 常量（starlightskins） | 删除，改用 `asset:Steve` 走 poly:// |
| L65-67 | `GetFaceUrl(uuid)` → starlightskins pixel face | 拼接 `poly://skin?type=face&src=...`（按账户类型路由 src） |
| L68-70 | `GetBodyUrl(uuid)` → starlightskins default face | 拼接 `poly://skin?type=body&src=...` |
| L72-90 | `GetBodyViewUrl[s]` → mineatar.io body front/right/back/left | 拼接 4 个 `poly://skin?type={front,right,back,left}&src=...` |
| L92-99 | `enum SkinView { Front, Right, Back, Left }` | 保留，供四方向语义 |

> ⚠️ 注意：现有 `GetBodyUrl` 注释写「body」但实际调的是 starlightskins 的 `render/default/.../face`（头像级全身）。接入后语义会真正变成「全身图」。另外 `Get*Url` 的入参要从 `uuidOrUsername` 改为能携带数据源信息的形态（`IAccount` 或 `(src, format?)` 元组）。

#### 🟠 消费方（数据流自上而下）— poly:// 方案下全部零改动

| 文件 | 位置 | 现状 | 接入影响 |
|------|------|------|----------|
| `Models/AccountModel.cs` | L35-37, 44-46, 53-55, 62-64 | 4 个账户分支**完全相同**地调 `GetFaceUrl/GetBodyUrl/GetBodyViewUrls` | ⚠️ **authlib 分支（L39-46）忽略 `AuthlibAccount.SkinUrl`**，是回落 Steve 的 bug 根源。接入后按账户类型传不同 src 给 `Get*Url` |
| `Models/AccountModel.cs` | L80, L82 | `FaceUrl`/`BodyUrl : Uri` 属性 | **零改动**（poly:// 仍是合法 Uri 字符串） |
| `Controls/AccountEntryButton.axaml` | L50 | `async:ImageLoader.Source="{Binding BodyUrl}"` | **零改动** |
| `Modals/AccountEntryModal.axaml` | L30 | `Fallback="{Binding BodyUrl}"` | **零改动** |
| `Components/RotatingSkinView.axaml.cs` | L20 | `Sources : IReadOnlyList<Uri>?` | **零改动** |
| `Components/RotatingSkinView.axaml` | L51 | `async:ImageLoader.Source="{Binding Url}"` | **零改动** |
| `Models/SkinFrameModel.cs` | L13 | `Url : Uri` | **零改动** |

#### 🟢 附带清理（取代后失去意义）

| 文件 | 位置 | 内容 | 动作 |
|------|------|------|------|
| `Widgets/NetworkCheckerWidget.axaml.cs` | L23 | `("Starlight Skins", "https://starlightskins.lunareclipse.studio")` | 删除 |
| `Widgets/NetworkCheckerWidget.axaml.cs` | L24 | `("Mineatar Skin", "https://api.mineatar.io")` | 删除 |
| `Pages/SettingsPage.axaml` | L679-680 | "Mineatar API" 外链卡片（NavigateUri） | 删除（致谢页性质） |

---

### 数据源（每种账户如何拿到 skin PNG）

| 账户类型 | poly:// src | 状态 |
|----------|-------------|------|
| **Authlib** | `SkinUrl`（裸 URL） | ✅ **已就绪**。`AuthlibAccount.SkinUrl`（`submodules/Trident.Net/src/TridentCore.Core/Accounts/AuthlibAccount.cs:13`），认证时由 `YggdrasilService.AuthenticateAsync` → `GetSkinUrlAsync` 填充，format 走自动检测 |
| **Microsoft** | `mojang:{uuid}` | ⚠️ **待确认**。Mojang sessionserver profile API（`sessionserver.minecraft.net/session/minecraft/profile/{uuid}`）返回 base64 textures，含 `SKIN.url` + `metadata.model`（slim/classic）。需确认 Trident 是否已有对应获取方法；若无，在 app 层补一个查询（或参照 `YggdrasilService.GetSkinUrlAsync` 实现） |
| **Trial** | `asset:Steve` / `asset:Alex` | ⚠️ **待确认**。需确认 Assets 内是否已有默认皮肤图；若无随渲染器附带一张 64×64 PNG |
| **Offline** | `asset:Steve` | ✅ 离线账户无皮肤，回落默认 Steve |

> `YggdrasilService.GetSkinUrlAsync(serverUrl, uuid, token)` 返回 `Task<Uri?>`，在 `submodules/Trident.Net/src/TridentCore.Core/Services/YggdrasilService.cs:111`，解析 profile textures 的逻辑在 L130（`Textures.TryGetValue("SKIN", ...)`）。Microsoft 分支的 Mojang sessionserver 查询可参照此实现。

---

### `AccountHelper` 重构后形态预览

```csharp
public static class AccountHelper
{
    // src 由调用方（AccountModel）按账户类型提供：
    //   Microsoft  -> $"mojang:{uuid}"
    //   Authlib    -> account.SkinUrl（裸 URL）
    //   Trial/Offline -> "asset:Steve" / "asset:Alex"
    public static Uri GetBodyUrl(string src) =>
        new($"poly://skin?type=body&src={Uri.EscapeDataString(src)}");

    public static Uri GetFaceUrl(string src) =>
        new($"poly://skin?type=face&src={Uri.EscapeDataString(src)}");

    public static IReadOnlyList<Uri> GetBodyViewUrls(string src) =>
    [
        new($"poly://skin?type=front&src={Uri.EscapeDataString(src)}"),
        new($"poly://skin?type=right&src={Uri.EscapeDataString(src)}"),
        new($"poly://skin?type=back&src={Uri.EscapeDataString(src)}"),
        new($"poly://skin?type=left&src={Uri.EscapeDataString(src)}"),
    ];

    public static Uri GetSteveBodyUrl() => GetBodyUrl("asset:Steve");
}
```

> `AccountHelper` 保持静态、无服务注入、无 IO。数据源路由（src 字符串的构造）由 `AccountModel` 在构造时完成——它持有 `IAccount`，能拿到 `AuthlibAccount.SkinUrl`、Microsoft 的 uuid 等。

---

## 关键技术决策记录

1. **为何不用外部库（Coloryr/MinecraftSkinRender.Image）**：它的纯 Skia 变体只做 3D 头像，全身 3D 需要 OpenGL/Vulkan 变体（不符合启动器纯 CPU 渲染诉求）；自建可完全控制且零运行时三方依赖。
2. **为何选 poly:// 拦截而非临时文件 / 内存 Bitmap**：
   - **临时文件**：存在「谁生成文件」死结——`AccountHelper` 静态无法注入服务、渲染异步但构造同步要 Uri、需自建缓存失效与异步回填。
   - **内存 Bitmap（改属性类型）**：要动 `RotatingSkinView`/`SkinFrameModel`/`AccountModel` 的属性类型 + XAML 绑定，改动面大。
   - **poly:// 拦截**：`AppImageLoader.ProvideImageAsync` 已是所有图片绑定的唯一入口且自带 MemoryCache，在其入口拦截 poly:// 前缀即可让渲染按需触发、缓存天然复用、异步无回填问题。改动收敛在 `AppImageLoader` + `AccountHelper`，绑定层与 `AccountModel` 属性类型零改动。
3. **左右对称的模型约定**：渲染器中模型 x- = 角色右半身 = 屏幕左；模型 x+ = 角色左半身 = 屏幕右。接入四方向时保持此约定。

---

## 实现顺序建议

1. **确认 Microsoft 数据源**：核查 Trident 是否已有 Mojang sessionserver profile 获取；没有则在 app 层补一个查询（参照 `YggdrasilService.GetSkinUrlAsync`）。
2. **内置默认 PNG**：确认/补充 Steve、Alex 的 64×64 皮肤 PNG 进 Assets（供 `asset:` 路由）。
3. **新增 `SkinRenderService`**：实现 url 解析 + src 前缀路由 + 下载/取内置 PNG + `SKBitmap.Decode` + `SkinFormat.Detect` + `SkinRenderer.Render*` + `SKImage`→`Bitmap`。
4. **改 `AppImageLoader`**：注入 `SkinRenderService`，`ProvideImageAsync` 入口加 `poly://` 分支转发。
5. **改 `AccountHelper`**：`Get*Url` 改为拼接 `poly://` url（入参改为 src 字符串），删除 starlightskins/mineatar。
6. **改 `AccountModel`**：4 个账户分支按类型构造 src 传给 `Get*Url`（authlib 用 `SkinUrl`、microsoft 用 `mojang:{uuid}`、trial/offline 用 `asset:`）。
7. **四方向 yaw 调参**：确定 `{front, right, back, left}` 对应的 `yawDeg`（等距视角下需视觉确认旋转连续性，建议从 `{0, 90, 180, 270}` 或 `{−45, 45, 135, 225}` 试起）。
8. **附带清理**：删 `NetworkCheckerWidget` 与 `SettingsPage` 的 starlightskins/mineatar 条目。
9. **构建 + 手测**：各账户类型的头像、全身、四方向旋转预览。

---

## 风险 / 待确认项

- [ ] **Microsoft 数据源**：Trident 是否已有 Mojang sessionserver profile → SKIN.url + model 获取？（影响「全部接管」可行性；authlib 路由已就绪，可先实现非 Microsoft 部分）
- [ ] **四方向等距 yaw 值**：需视觉调参，确保 RotatingSkinView 旋转过渡自然（原本是 mineatar 的 flat 2D 视角，换 3D 等距后过渡观感会变）。
- [ ] **`AppImageLoader` 构造注入 `SkinRenderService`**：当前 `AppImageLoader` 在 `Program.cs:109` 手动 new（非 DI），需评估注入方式——可能要在 `Startup.ConfigureServices` 注册 `SkinRenderService`，并在 `Program.cs` 构造 `AppImageLoader` 时从 `Services` 取。
- [ ] **`SKImage` → `Avalonia Bitmap` 转换**：`SkinRenderer` 产出 `SKImage`，`ImageLoader` 期望 `Avalonia.Media.Imaging.Bitmap`。需确认两者桥接（`SKImage.Encode()` → stream → `new Bitmap(stream)`，或用 SkiaSharp 的互操作 API）。注意 Bitmap 生命周期与 `AppImageLoader` 现有缓存驱逐策略一致（缓存驱逐不释放 Bitmap，靠 GC finalizer）。
- [ ] **皮肤更新感知**：poly:// url 含 src，skin 变了 src 变 → url 变 → 新缓存条目。但 30 分钟滑动过期前旧条目仍占缓存槽，问题不大（LRU 会驱逐）。若需即时更新可在 AccountModel 刷新时主动失效对应缓存。
- [ ] **`FilterQuality` 过时警告**：接入后视情况迁移到 `SamplingOptions`，非阻塞。
