# Trident CLI 入门体验改善

## 背景

对 trident CLI 做了一轮纯调用式摸底（不读源码），逐个命令验证能力与报错行为。结论：作为脚本 / 自动化入口（也是 MCP server 的底座）它结构清晰、退出码语义化、JSON 输出稳定，是合格甚至优秀的；但以「命令行用户第一次接触能否上手」衡量则不成立，存在多处劝退点。决定按调研发现逐个改善。

## 做什么

改善 trident CLI 对人类新手的使用体验，覆盖下方调研发现的问题点，使其能够独立作为用户入口，而不只是脚本接口。

## 期望效果

- 新用户不读文档，仅靠 `trident --help` 及各命令 `-h` 即可完成实例创建、搜索装包、构建启动、删除的完整闭环。
- 帮助与错误信息一致、完整、不泄漏实现细节。
- 需要机器可读输出的场景（脚本、agent、MCP）不因人类友好化而受损。

## 调研所得的注意事项

以下均经实际调用验证（2026-10-29，本机 trident）：

1. **默认输出即纯 JSON**：`trident list`、`inspect`、`search` 等全部命令直接输出大段 JSON，无表格 / 彩色等人类可读模式；`--json` 与默认输出无区别。
2. **帮助中英混杂**：帮助框架部分（用法 / 选项 / 命令列表）是中文，命令描述、`Required` 标记是英文，本地化半成品。
3. **`package` 无 remove 子命令**：实测 `trident package remove` 报 `Unknown command 'remove'`——能 add / enable / disable，却不能移除包，是实例管理的功能性缺口。
4. **惯用入口缺失**：没有 `trident help` 命令（只能 `-h` / `--help`），也没有 `--version`。
5. **`patch` 的 `[ACTION]` 位置参数帮助不列合法值**：帮助里只有一个裸 `[ACTION]`，要打错一次才能从报错得知合法值是 `list / add / enable / disable / move / remove`。
6. **错误信息泄漏 .NET 内部类型名**：如 `trident run --mode bogus` 报 `Failed to convert 'bogus' to Nullable\`1. Valid values are ...`——虽附了合法值，但类型名对用户是噪音。
7. **删除语义对新手有困惑**：`instance delete --yes` 成功后返回 `deletedImmediately: false`，实例目录仍在（落 `_bomb_has_been_planted_` 标记延迟清除，列表已立即消失），用户无法判断到底删没删。

另有一处已确认的可取设计，改善时不要破坏：非交互模式下危险操作强制 `--yes` 并给出可操作提示（`Re-run with --yes in non-interactive mode`）；错误退出码有语义（2 用法错、3 实例不存在、5 需确认）；实例目录内自动识别上下文免传 `--instance`。
