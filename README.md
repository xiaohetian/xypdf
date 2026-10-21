# xyPDF全能工具箱 (PdfToolbox)

<p align="center">
  <strong>一款功能完整、界面美观的 Windows 桌面 PDF 处理工具，内置 OFD 转 PDF 引擎</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue" alt=".NET 8.0"/>
  <img src="https://img.shields.io/badge/Framework-WPF-purple" alt="WPF"/>
  <img src="https://img.shields.io/badge/Architecture-MVVM-green" alt="MVVM"/>
  <img src="https://img.shields.io/badge/Version-v1.3.1-brightgreen" alt="Version"/>
  <img src="https://img.shields.io/badge/License-MIT-orange" alt="License"/>
</p>

---

## 项目简介

**xyPDF全能工具箱** 基于 .NET 8 + WPF + MVVM 开发，提供 **10 大核心功能**：PDF 阅读、合并、拆分、裁剪、水印、加密/解密、图片转 PDF、文档转 PDF、PDF 转图片，以及 **OFD 转 PDF**（内置 Java 转换引擎，随包部署）。

**零外部依赖**：无需安装 GhostScript、ImageMagick、Java 等系统组件，开箱即用。

## 功能总览

| # | 模块 | 要点 |
|---|------|------|
| 1 |  PDF 阅读器 | 多标签页、单页/全部页面视图、25%~300% 缩放、页面旋转 |
| 2 |  PDF 合并 | 多文件添加、拖拽排序、实时进度 |
| 3 | ️ PDF 拆分 | 按范围 / 拆单页 / 提取指定页 / 奇数页 / 偶数页（5 种模式） |
| 4 | 📐 PDF 裁剪 | 四边独立边距、一键统一、实时预览 |
| 5 | 🖼️ 图片转 PDF | JPG/PNG/BMP/GIF/TIFF 批量转换，A4/A3/Letter，横版图片自动横放 |
| 6 | 📝 文档转 PDF | TXT / RTF / HTML 批量转换 |
| 7 |  PDF 转图片 | PNG/JPG/BMP/TIFF，DPI 96~600 可调，逐页输出 |
| 8 | 💧 PDF 水印 | 文字/图片水印添加；基于颜色检测的水印矢量去除 |
| 9 | 🔒 PDF 加密/解密 | 用户/所有者密码、权限控制、解密 |
| 10 | 📄 OFD 转 PDF | 内置引擎批量转换、拖拽添加、自定义输出目录、进度日志、60s 超时保护 |

各模块详细参数表见 [PdfToolbox/README.md](./PdfToolbox/README.md)。

## 技术栈

| 组件 | 版本 | 用途 |
|------|------|------|
| .NET / WPF | 8.0 | 运行时与 UI 框架 |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 数据绑定与命令 |
| PdfSharp | 6.1.1 | PDF 合并/拆分/裁剪/加密 |
| QuestPDF | 2024.10.3 | PDF 生成（水印/文档转换） |
| PdfiumViewer | 2.13.0 | PDF 渲染引擎 |
| ofdrw + jpackage | — | OFD 转 PDF 引擎（Java 17，约 94MB 随包部署） |

## 快速开始

### 系统要求

- Windows 10/11 (x64)
- 无需预装 .NET（自包含发布）
- 磁盘约 250MB（含 OFD 引擎）

### 从源码构建

```powershell
git clone https://github.com/xiaohetian/xypdf.git
cd xypdf

dotnet restore
dotnet build -c Release

# 自包含发布（OFD 引擎会随构建自动复制到输出目录）
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishReadyToRun=true `
  -o ./publish
```

运行 `publish\PdfToolbox.exe` 即可；首次运行如遇 SmartScreen 提示，点击「更多信息」→「仍要运行」。

### 打包微软商店 MSIX

需要 Windows SDK（makeappx）与 PowerShell 7：

```powershell
pwsh -ExecutionPolicy Bypass -File .\build-msix.ps1
```

产物为仓库根目录 `PdfToolbox-YYYYMMDD.msix`（约 148MB），直接上传 Partner Center。脚本自动完成：Release 发布 → 递归打包（含引擎）→ makeappx → 校验。

## 项目结构

```
xypdf/
├── PdfToolbox.sln                 # 解决方案
├── build-msix.ps1                 # MSIX 商店打包脚本（UTF-8 BOM，须用 pwsh 执行）
├── PdfToolbox/
│   ├── App.xaml / MainWindow.xaml # 入口、全局样式、侧边栏导航
│   ├── Services/                  # 业务逻辑（Pdf / Image / Document / OfdConvert / Watermark…）
│   ├── ViewModels/                # MVVM 视图模型（10 个功能模块）
│   ├── Views/                     # 各模块 XAML 界面
│   ├── Helpers/                   # 拖拽、设置、硬件指纹等工具类
│   └── engine/ofd-convert/        # OFD 转换引擎（ofd-convert.exe + JRE + jar，随包部署）
└── PdfToolbox.Package/            # MSIX 打包清单（Package.appxmanifest）
```

### OFD 引擎约定

- 调用方式：子进程 `ofd-convert.exe <输入.ofd> <输出.pdf>`，退出码 0 = 成功
- 输出规则：源目录「原名.pdf」，重名依次追加 `-转换`、`-转换-2`，不覆盖原文件
- 单文件 60 秒超时自动终止；无需用户安装 Java（JRE 随引擎打包）

## 常见问题

<details>
<summary>OFD 转换提示「未找到转换引擎」</summary>

确认程序目录下存在 `engine\ofd-convert\ofd-convert.exe`（约 94MB）。该目录由 csproj 的 `Content CopyToOutputDirectory=PreserveNewest` 随构建自动复制。
</details>

<details>
<summary>水印去除后文字不可选？</summary>

水印去除采用图像处理管线（渲染 → 检测 → 修复 → 重建），输出为图片型 PDF，属该技术路线的固有限制。
</details>

<details>
<summary>PDF 阅读器打开大文件较慢？</summary>

默认为「全部页面视图」一次性渲染；100+ 页的超大文档建议切换到单页模式。
</details>

## 更新日志

### v1.3.1 (2026-10-21)
- ✅ 修复新下载 OFD 文件转换后乱码（同步 OFD 引擎字体字形映射修复版，实测样例 PDF 无乱码）
- ✅ 版本号升级至 1.3.1.0（微软商店要求每次提交的版本号唯一）

### v1.3.0 (2026)
- ✅ 新增 **OFD 转 PDF**：内置 Java 引擎（94MB 随包），批量/拖拽/自定义输出目录/进度日志
- ✅ 修复关闭窗口后进程残留与 CPU 占用
- ✅ 横版图片转 PDF 自动横放
- ✅ MSIX 商店打包修复：引擎递归进包、清单编码修复、版本 1.3.0.0

### v1.2.1 (2026)
- ✅ 全面重构：PDF 服务异步化、统一操作封装、水印矢量去除、补齐 PDF 转图片

### v1.2.0 (2026)
- ✅ PDF 合并/拆分/裁剪/旋转/水印/加密/阅读/转换 + 硬件指纹授权

## 许可证

MIT License

## 联系方式

- **作者**: 小河田
- **微信**: DoAfterThink
