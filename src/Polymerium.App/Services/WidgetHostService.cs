namespace Polymerium.App.Services;

// 这不是什么插件化的模块，所有小工具都是一次性集中封装的，因此后端共用同一个服务，后缀 Host 表示一对多
public class WidgetHostService(PersistenceService _) { }