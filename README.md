# 距离放假还有… — HolidayCountdown

ClassIsland 插件，在主界面显示距离下一个法定节假日还有多久。

## 功能

- 显示距离下一个法定节假日的倒计时
- 内置 2025–2028 年节假日数据
- 支持通过 GitHub / API 远程更新数据
- 每实例独立设置：自定义显示格式、字体大小、紧凑模式等
- 插件设置页管理节假日和数据源

## 安装

在 ClassIsland 插件管理中导入 `cipx/HolidayCountdown.cipx` 即可。

## 开发

需要 .NET 8 SDK。

```bash
dotnet build -c Release
```

构建产物在 `bin/Release/net8.0/`，插件包在 `cipx/HolidayCountdown.cipx`。
