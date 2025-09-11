# KEY 命名格式

`<ControlName>_<Region>[ControlType:Tag|Bar|Tab|Menu|Label|Button][Text|Placeholder|Prompt|Title|Subtitle]`

- 特别的，TextBox 筛选框、搜索框等都被叫做 Bar，而 ComboBox 则被叫做 Box
- 一长串的（包含逗号或句号结尾的）文本命名为 Prompt
- 一个控件的不同状态会是 ActiveOnText/ActiveOffText SourceOriginalText/SourceLocalText
- 通知采用 `[Operation(Sth.Doing)][Result]Notification[Title|Prompt]`，也可以在 `Notification` 后面接
  `[Action|Tag][Text|Placeholder]`
