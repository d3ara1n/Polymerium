# Trident V3

V3 将 V2 的单个 `profile.json` 拆分为「元数据 + 每包一个文件」：`profile.json` 只保留实例元数据，`packages/` 目录承载全部包引用。

## 目录结构

`*.json` 类型的文件为可用户编辑的配置文件。
`data.*.json` 类型的文件为程序记录信息，该文件缺失或构建后删除并不致命；不建议用户出于纠错目的外的修改。
`packages/` 为包引用目录，每包一个文件，是包集合的唯一权威来源。

### `profile.json` 元数据信息

该文件储存一个能被用户编辑并作为整合包元数据导出的最小实例信息：实例名、构建层（版本、加载器、源）、规则、运行覆盖项。

该文件不再包含包清单。文件顶部声明格式版本：

```json
{ "format": 3 }
```

### `packages/` 包目录

每个包引用一个文件，字段与 V2 的包条目一致：

```json
{
  "pref": "pref://modrinth/aC3cM3Vq@9I21YYxf",
  "enabled": true,
  "source": null,
  "tags": ["optimization"]
}
```

目录即唯一真源，无 manifest 与之同步：加载时读取目录内全部文件，保存时把包集合写回逐个文件。新增包 = 新建文件，删除包 = 删除文件。

### 包文件命名

文件名是包的项目身份，**绝不包含版本**：

- `{仓库}.{身份}.json`，例如 `modrinth.sodium.json`
- pref 带命名空间时：`{仓库}.{命名空间}.{身份}.json`
- 无法解析为项目身份的 pref 退化为 pref 的稳定哈希

版本更新只改文件内容，git 报 `~ packages/modrinth.sodium.json`（修改）而非 delete + add 一对文件；文件名跨更新稳定是单包修改可见的前提。

## 设计动机

V2 单文件在 git 工作流中不可读：任何包变更都重写整个文件，`git status` 只见一个 profile.json 的 ±N 行，无法分辨改了哪些包。

V3 的 git 粒度：新增 = `+ packages/xx.json`，更新 = `~ packages/xx.json`，删除 = `- packages/xx.json`。进一步获得单包历史（`git log -p -- packages/...`）、单包回滚（`git checkout -- packages/...`）和收敛到单文件的 merge 冲突。

## 语义

- 包列表无部署语义：部署仲裁按 source tier 与 SourceOrders 排名，不依赖列表顺序，因此目录枚举（按文件名排序）即为合法展示顺序。
- 写 N 个包文件不是单步原子操作，这是无 manifest 布局的刻意取舍；profile 单写者、逐文件幂等写入。
