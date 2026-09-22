# PDF全能工具箱 (PdfToolbox)

<p align="center">
  <strong>一款功能强大、界面美观的桌面PDF处理工具</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue" alt=".NET 8.0"/>
  <img src="https://img.shields.io/badge/Framework-WPF-purple" alt="WPF"/>
  <img src="https://img.shields.io/badge/Architecture-MVVM-green" alt="MVVM"/>
  <img src="https://img.shields.io/badge/License-MIT-orange" alt="License"/>
</p>

---

## 项目简介

**PDF全能工具箱** 是一款基于 .NET 8 + WPF 开发的桌面应用程序，采用 MVVM 架构设计，提供 **10 大核心 PDF 处理功能**，并内置 OFD 转 PDF 转换引擎。无需安装 GhostScript、Java 等第三方系统组件，开箱即用。

### 核心特性

- 🎨 **现代化UI** — 精心设计的深色侧边栏 + 浅色内容区，圆角卡片、阴影效果、流畅的交互动画
- ️ **MVVM架构** — 使用 CommunityToolkit.Mvvm 实现数据绑定和命令模式，代码清晰可维护
- **零外部依赖** — 无需安装 GhostScript、ImageMagick、Java 等系统组件（OFD 引擎内置打包）
- 📊 **进度反馈** — 所有长时间操作均提供实时进度条和状态提示
- ⚡ **异步处理** — 所有文件操作均为异步执行，UI不会卡顿
- 🔧 **可取消操作** — 支持随时取消正在进行的处理任务
- 📄 **OFD 支持** — 内置 Java 转换引擎（随包部署），OFD 一键转 PDF，批量/拖拽均可

---

## 功能模块

### 1.  PDF 阅读器

专业的PDF阅读体验，支持多标签页浏览。

| 功能 | 说明 |
|------|------|
| **多标签页** | 同时打开多个PDF文件，标签页切换 |
| **全部页面视图** | 一次加载所有页面，垂直滚动浏览（默认模式） |
| **单页导航** | 首页/上一页/下一页/末页 精确翻页 |
| **缩放控制** | 放大/缩小/适应宽度，支持 25%~300% 缩放范围 |
| **页面旋转** | 顺时针/逆时针 90° 旋转 |
| **实际尺寸渲染** | 自动获取PDF页面真实尺寸，精确渲染 |

### 2.  PDF 合并

将多个PDF文件合并为一个。

| 功能 | 说明 |
|------|------|
| **多文件添加** | 支持一次选择多个PDF文件 |
| **拖拽排序** | 上移/下移/置顶/置底，自由调整文件顺序 |
| **删除文件** | 移除不需要的文件 |
| **清空列表** | 一键清空所有文件 |
| **进度显示** | 实时显示合并进度 |

### 3. ️ PDF 拆分

灵活的PDF拆分策略，支持 5 种拆分模式。

| 模式 | 说明 |
|------|------|
| **按范围拆分** | 指定页码范围（如 1-5, 8-10），拆分为独立文件 |
| **拆分为单页** | 每页生成一个独立的PDF文件 |
| **提取指定页** | 输入页码列表（如 1,3,5），提取为单个PDF |
| **提取奇数页** | 自动提取所有奇数页（1,3,5...） |
| **提取偶数页** | 自动提取所有偶数页（2,4,6...） |

### 4. 📐 PDF 裁剪

精确裁剪PDF页面边距。

| 功能 | 说明 |
|------|------|
| **四边裁剪** | 独立设置上/下/左/右边距（单位：点，1点=1/72英寸） |
| **统一边距** | 一键应用相同边距到所有边 |
| **实时预览** | 显示PDF总页数信息 |

### 5. 🖼️ 图片转 PDF

将图片批量转换为PDF文档。

| 功能 | 说明 |
|------|------|
| **多格式支持** | JPG、JPEG、PNG、BMP、GIF、TIFF |
| **批量添加** | 一次选择多张图片 |
| **顺序调整** | 上移/下移调整图片顺序 |
| **页面尺寸** | 支持 A4、A3、Letter 三种页面尺寸 |

### 6. 📝 文档转 PDF

将文本文档转换为PDF格式。

| 功能 | 说明 |
|------|------|
| **多格式支持** | TXT、RTF、HTML 文档转换 |
| **批量处理** | 支持同时转换多个文件 |
| **进度显示** | 实时显示转换进度 |

### 7. 🖼️ PDF 转图片

将PDF页面转换为图片文件。

| 功能 | 说明 |
|------|------|
| **多格式支持** | PNG、JPG、BMP、TIFF 四种图片格式 |
| **批量转换** | 支持一次转换多个PDF文件 |
| **页面选择** | 自动转换所有页面，每页生成一张图片 |
| **质量控制** | 支持调节 DPI（96、150、200、300、600） |
| **文件命名** | 自动命名格式：`文件名_page页码.扩展名` |
| **进度显示** | 实时显示转换进度和已处理页数 |

### 8. 💧 PDF 水印

添加和去除PDF水印。

#### 添加水印
| 参数 | 说明 |
|------|------|
| **文字水印** | 自定义水印文字、字体大小、颜色 |
| **图片水印** | 支持 PNG/JPG 图片作为水印 |
| **透明度** | 0.0~1.0 可调透明度 |
| **旋转角度** | 自定义水印旋转角度（默认 -45°） |
| **平铺模式** | 全屏平铺或单处放置 |

#### 去除水印
| 参数 | 说明 |
|------|------|
| **目标颜色** | 指定要去除的水印颜色 |
| **颜色容差** | 0~255 容差范围，默认 100 |
| **去除强度** | 0.0~1.0 强度控制，默认 0.7 |
| **智能检测** | 基于RGB距离的水印像素检测 |
| **空间去噪** | 自动过滤孤立噪点，避免误伤 |
| **多轮修复** | 两轮渐进式像素修复，确保彻底去除 |

### 8. 🔒 PDF 加密/解密

PDF文档安全保护。

| 功能 | 说明 |
|------|------|
| **用户密码** | 打开PDF时需要输入的密码 |
| **所有者密码** | 修改PDF权限时需要输入的密码 |
| **权限控制** | 允许打印/允许复制/允许修改 |
| **解密** | 输入密码移除PDF加密保护 |

### 9. 📄 OFD 转 PDF

调用内置转换引擎（ofd-convert.exe，Java 打包）批量将 OFD 文件转换为 PDF。

| 功能 | 说明 |
|------|------|
| **批量转换** | 一次添加多个 OFD 文件，逐个转换、互不影响 |
| **拖拽添加** | 支持将 .ofd 文件拖入界面，自动过滤无效文件 |
| **输出目录** | 自定义输出目录（留空 = 输出到源文件所在目录） |
| **防覆盖** | 已存在同名 PDF 时自动追加 `-转换`、`-转换-2` 后缀 |
| **实时进度** | 进度条 + 日志区实时显示每一步转换结果 |
| **超时保护** | 单个文件超过 60 秒自动终止，避免卡死 |

---

## 技术架构

### 技术栈

| 组件 | 版本 | 用途 |
|------|------|------|
| **.NET** | 8.0 | 运行时框架 |
| **WPF** | Windows | UI框架 |
| **CommunityToolkit.Mvvm** | 8.2.2 | MVVM框架 |
| **PdfSharp** | 6.1.1 | PDF操作（合并/拆分/裁剪/加密） |
| **PdfiumViewer** | 2.13.0 | PDF渲染引擎 |
| **QuestPDF** | 2024.10.3 | PDF生成（水印/文档转换） |
| **System.Drawing.Common** | 8.0.0 | 图像处理 |
| **Newtonsoft.Json** | 13.0.3 | JSON序列化 |
| **System.Management** | 8.0.0 | 硬件信息获取 |
| **ofdrw + jpackage** | — | OFD 转 PDF 引擎（Java 17，94MB 随包部署） |

### 项目结构

```
PdfToolbox/
├── App.xaml / App.xaml.cs          # 应用程序入口，全局样式
├── MainWindow.xaml / .cs           # 主窗口，侧边栏导航
├── PdfToolbox.csproj               # 项目配置
│
├── Helpers/                        # 辅助工具类
│   ├── BoolToVisibilityConverter.cs
│   ├── FolderBrowserHelper.cs
│   ├── HardwareIdHelper.cs         # 硬件指纹生成
│   ├── SecuritySettings.cs         # 安全设置
│   └── SettingsService.cs          # 用户偏好设置
│
├── Models/                         # 数据模型
│   └── ...
│
├── Services/                       # 业务逻辑层
│   ├── DocumentService.cs          # 文档转换服务
│   ├── ImageService.cs             # 图片处理服务
│   ├── OfdConvertService.cs        # OFD转PDF服务（调用内置引擎）
│   ├── PdfRenderService.cs         # PDF渲染服务
│   ├── PdfService.cs               # PDF操作服务
│   ├── PdfToolService.cs           # PDF工具服务
│   ├── SettingsService.cs          # 设置服务
│   └── WatermarkRemovalService.cs  # 水印去除服务
│
├── ViewModels/                     # 视图模型层 (MVVM)
│   ├── ViewModelBase.cs            # 基类
│   ├── MainViewModel.cs            # 主窗口ViewModel
│   ├── PdfReaderViewModel.cs       # 阅读器
│   ├── PdfMergeViewModel.cs        # 合并
│   ├── PdfSplitViewModel.cs        # 拆分
│   ├── PdfCropViewModel.cs         # 裁剪
│   ├── ImageToPdfViewModel.cs      # 图片转PDF
│   ├── DocumentToPdfViewModel.cs   # 文档转PDF
│   ├── PdfWatermarkViewModel.cs    # 水印
│   ├── PdfSecurityViewModel.cs     # 加密/解密
│   └── OfdConvertViewModel.cs      # OFD转PDF
│
├── Views/                          # 视图层 (XAML)
│   ├── PdfReaderView.xaml          # 阅读器界面
│   ├── PdfMergeView.xaml           # 合并界面
│   ├── PdfSplitView.xaml           # 拆分界面
│   ├── PdfCropView.xaml            # 裁剪界面
│   ├── ImageToPdfView.xaml         # 图片转PDF界面
│   ├── DocumentToPdfView.xaml      # 文档转PDF界面
│   ├── PdfWatermarkView.xaml       # 水印界面
│   ├── PdfSecurityView.xaml        # 加密/解密界面
│   └── OfdConvertView.xaml         # OFD转PDF界面
│
├── engine/                         # OFD转PDF引擎（Java 打包，随程序部署）
│   └── ofd-convert/                # ofd-convert.exe + JRE 运行时 + ofd-convert-0.1.0.jar
│
└── Resources/                      # 资源文件
    └── pdftoolbox.ico              # 应用图标
```

---

## 快速开始

### 系统要求

- **操作系统**: Windows 10/11 (x64)
- **运行时**: 无需安装 .NET（自包含发布）
- **磁盘空间**: 约 250MB（含约 94MB OFD 转换引擎）

### 安装使用

1. 从发布目录获取 `PdfToolbox.exe`
2. 双击运行即可
3. 首次运行 Windows SmartScreen 可能提示"未知发布者"，点击"更多信息" → "仍要运行"

### 从源码构建

```powershell
# 克隆项目
git clone <repository-url>
cd PdfToolbox

# 还原依赖
dotnet restore

# 编译
dotnet build -c Release

# 发布自包含单文件
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishReadyToRun=true `
  -o ./publish
```

> 注：OFD 转换引擎默认随程序目录部署（`engine\ofd-convert\`），无需单独安装 Java。

### 打包微软商店 MSIX

需要 Windows SDK（makeappx/signtool）与 PowerShell 7（pwsh）：

```powershell
# 在解决方案根目录执行（仓库根：D:\DELL\PDF工具）
pwsh -ExecutionPolicy Bypass -File .\build-msix.ps1     # 默认路径，或
pwsh -ExecutionPolicy Bypass -File .\build-msix.ps1 -ProjectRoot <路径>
```

产物为仓库根目录下的 `PdfToolbox-YYYYMMDD.msix`，直接上传至 **Partner Center** 即可。脚本会自动：Release 自包含发布 → 递归打包（含引擎）→ makeappx 生成 MSIX → 签名校验（未签名属正常，商店提交不需要）。

---

## 用户指南

### PDF 阅读器使用技巧

1. **打开文件**: 点击"打开 PDF"按钮，支持多选
2. **切换视图**: 底部"切换视图"按钮可在单页/全部页面模式间切换
3. **缩放**: 使用放大/缩小按钮或"适应宽度"快速调整
4. **旋转**: 顶部 ↺ ↻ 按钮旋转当前页面

### 水印去除最佳实践

1. **选择目标颜色**: 使用颜色选择器指定水印颜色
2. **调整容差**: 
   - 半透明水印 → 容差 100~150
   - 纯色水印 → 容差 60~80
3. **调整强度**: 
   - 残留痕迹 → 强度 0.8~0.9
   - 避免误伤 → 强度 0.5~0.6
4. **注意**: 水印去除为有损处理，输出为图片型PDF（文字不可选）

### PDF 拆分模式选择

| 场景 | 推荐模式 |
|------|----------|
| 提取特定章节 | 按范围拆分 |
| 每页独立保存 | 拆分为单页 |
| 只要某几页 | 提取指定页 |
| 只要奇数/偶数页 | 提取奇数页/偶数页 |

---

## 常见问题

### Q: 水印去除后文字不可选了？
A: 水印去除采用图像处理方式，将PDF页面渲染为图片后修复再重建，因此输出为图片型PDF。这是目前去除水印的技术限制。

### Q: PDF阅读器打开大文件很慢？
A: 阅读器会一次性渲染所有页面。对于超大PDF（100+页），建议使用单页模式。

### Q: 合并PDF时顺序不对？
A: 使用合并界面的上移/下移/置顶/置底按钮调整文件顺序，顺序从上到下即为合并后的页码顺序。

### Q: 加密后的PDF无法用其他软件打开？
A: 请确保使用正确的用户密码。所有者密码用于修改权限，用户密码用于打开文档。

### Q: OFD 转换提示"未找到转换引擎"？
A: 请确认程序目录下存在 `engine\ofd-convert\ofd-convert.exe`（约 94MB）。直接双击运行源码构建产物时需确保该目录随构建复制到位。

---

## 开发说明

### 添加新功能模块

1. 在 `ViewModels/` 创建新的 ViewModel（继承 `ViewModelBase`）
2. 在 `Views/` 创建对应的 XAML 视图
3. 在 `MainWindow.xaml` 的 `ContentControl.Resources` 中添加 DataTemplate
4. 在 `MainViewModel.cs` 的菜单项列表中添加新菜单

### OFD 转换引擎约定

- 引擎路径：`AppContext.BaseDirectory\engine\ofd-convert\ofd-convert.exe`，子进程方式调用，参数 1 = 输入 OFD，参数 2 = 输出 PDF
- 转换规则：输出到源文件同目录「原名.pdf」，已存在依次追加 `-转换`、`-转换-2` 后缀，不覆盖原文件
- 退出码 0 = 成功；非 0 读取 stderr 作为错误信息；单文件 60 秒超时自动终止
- 引擎通过 `PdfToolbox.csproj` 的 `Content CopyToOutputDirectory=PreserveNewest` 随构建输出

### 样式规范

- 主色: `#4A90D9` (PrimaryBrush)
- 侧边栏: 渐变 `#2E3E50` → `#232F3D`
- 背景色: `#F0F2F5`
- 卡片圆角: 8px
- 按钮圆角: 6px

---

## 许可证

MIT License

---

## 联系方式

- **作者**: 小河田
- **微信**: DoAfterThink
- **版本**: v1.3.0
- **发布日期**: 2026年

---

## 更新日志

### v1.3.0 (2026)
- ✅ 新增 **OFD 转 PDF** 模块：内置 Java 转换引擎（94MB 随包部署，支持批量/拖拽/自定义输出目录/进度日志）
- ✅ 修复关闭窗口后进程残留与 CPU 占用（OnMainWindowClose + 渲染循环取消）
- ✅ 横版图片转 PDF 自动横放（交换页面宽高）优化
- ✅ MSIX 商店打包修复：引擎递归进包、清单编码/文案修复、版本 1.3.0.0

### v1.2.1 (2026)
- ✅ 全面重构：PDF 服务异步化、统一操作封装
- ✅ 新增水印矢量去除、补齐 PDF 转图片

### v1.2.0 (2026)
- ✅ PDF 合并/拆分/裁剪/旋转/水印/加密/阅读/转换等功能
- ✅ 硬件指纹授权系统
