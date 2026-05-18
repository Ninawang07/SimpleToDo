# SimpleTodo — Windows 桌面 Todo 小工具产品规划

## Context

用户需要一个 Windows 桌面端极简 To-Do List，核心诉求：**占用内存最小**（目标 <20MB）、**零外部依赖**（不需装运行时或框架）、系统托盘驻留。技术选型已由用户确认：C# WinForms + 系统托盘 + 本地 JSON 存储。

---

## 技术栈

| 项 | 选型 | 理由 |
|---|---|---|
| 框架 | .NET Framework 4.8 / WinForms | Windows 10/11 内置，无需额外安装 |
| UI | WinForms 原生控件 | 无额外 UI 库，零 NuGet 依赖 |
| 序列化 | `System.Web.Extensions` (JavaScriptSerializer) | .NET Framework 内置 |
| 存储 | `%APPDATA%\SimpleTodo\tasks.json` | 单文件，人可读，易备份 |
| 构建 | MSBuild 或 csc.exe 直接编译 | 单 exe 产出，无需发布工具 |

**零 NuGet 依赖。**

---

## 项目结构

```
D:\111\SimpleTodo\
  SimpleTodo.csproj
  Program.cs              # 入口，单实例互斥
  MainForm.cs             # 主窗口 + 布局 + 托盘 + 定时器（逾期检查）
  TaskItem.cs             # 数据模型
  TaskStore.cs            # JSON 持久化
  TaskRow.cs              # 单条任务 UserControl
  TaskListPanel.cs        # 任务列表滚动容器
  NativeMethods.cs        # P/Invoke（仅 SetForegroundWindow）
```

7 个源文件（去掉 ThemeHelper，去掉热键），约 1500 行。

---

## 数据模型

```json
{
  "Id": "guid",
  "Title": "string",
  "Completed": false,
  "ParentId": null,
  "Children": [],
  "CreatedAt": "2026-05-17T14:30:00",
  "Deadline": "2026-05-20T00:00:00",
  "CompletedAt": null,
  "SortOrder": 0
}
```

- `Deadline` 可为 null（无截止日期）
- `Children` 在 JSON 中存储（人可读），加载时重建 `ParentId` 引用
- `Depth` / `IsExpanded` / `HasChildren` / `IsOverdue` 为计算属性，不序列化
- `IsOverdue` = `Deadline != null && Deadline < DateTime.Now && !Completed`

---

## 功能范围（v1）

1. **添加任务** — 顶部输入框，可附带截止日期，Enter 键添加
2. **完成任务** — 复选框勾选，文字加删除线 + 置灰
3. **编辑任务** — 双击标题原地编辑（Label ↔ TextBox）
4. **子任务** — "+" 按钮添加子任务，缩进显示，最多 3 层
5. **折叠/展开** — 有子任务的行显示箭头，点击折叠
6. **删除任务** — X 按钮删除（含子任务先确认）
7. **系统托盘** — 托盘图标，左键切换显示/隐藏，右键菜单（显示/退出）
8. **自动保存** — 任何变更后 500ms 防抖写入 JSON
9. **窗口定位** — 屏幕**左下角**，任务栏上方
10. **创建日期 + DDL** — 每条任务显示创建日期和截止日期（可设可不设）
11. **逾期提醒** — 超过 DDL 且未完成的任务，左侧红色标识 + DDL 文字变红
12. **启动时逾期检查** — 启动时自动扫描逾期任务，托盘气泡提示逾期数量

---

## 窗口设计（左下角版）

- 尺寸：380×540px（略宽以容纳日期信息）
- 位置：屏幕**左下角**，距左边缘 8px，距下边缘（任务栏上方）8px
- 风格：浅色主题，圆角细边框，无标题栏，自定义顶栏

```
┌────────────────────────────────────┐
│  ≡ SimpleTodo              —  □  × │  ← 自定义标题栏（浅灰底）
├────────────────────────────────────┤
│  任务标题: [              ]        │
│  DDL:     [____/__/__] [选填]     │  ← 日期输入（yyyy/MM/dd）
│  [添加任务]                        │
├────────────────────────────────────┤
│                                    │
│ ▸ ☐ 买牛奶               [+]  [×]  │  ← 任务行 36px
│    📅 05/17   ⏰ 05/20             │  ← 日期行 20px（小字灰色）
│                                    │
│   ☐ 全脂牛奶             [+]  [×]  │  ← 子任务（缩进 24px）
│    📅 05/17   ⏰ 05/19             │
│                                    │
│ ▌☐ 写周报                 [+]  [×]  │  ← ▌红色左条 = 已逾期
│   📅 05/15   ⏰ 05/16 ⚠逾期        │  ← 红色逾期标记
│                                    │
│ ☐ 健身                   [+]  [×]  │
│  📅 05/17   ⏰ ──                  │  ← 无 DDL 显示 "──"
│                                    │
└────────────────────────────────────┘
```

**任务行布局（每行 36px 高）：**
```
[左缩进] [▸] [☐] [任务标题文字...fill] [+] [×]
```

**日期行布局（每行 18px 高，浅灰色小字）：**
```
[左缩进+24px] 📅 MM/dd   ⏰ MM/dd [逾期标记]
```

- 逾期任务：左侧加 3px 红色竖条（`BorderLeft`），日期行 DDL 文字变红加粗
- 已完成任务：全部置灰 + 删除线，日期行也置灰，不显示逾期标记
- 子任务叠加缩进：第 1 层 +24px，第 2 层 +48px，第 3 层 +72px

---

## 输入区设计

```
┌────────────────────────────────────┐
│  任务标题: [________________]      │  ← TextBox
│  DDL:     [____/__/__] (选填)     │  ← MaskedTextBox 或 TextBox
│  [添加任务]                        │  ← Button
└────────────────────────────────────┘
```

- 标题输入框自动聚焦
- DDL 输入框可选填，格式 `yyyy/MM/dd`，留空即无截止日期
- 点击"添加任务"或按 Enter（在标题框）创建任务
- 创建后清空输入框，焦点回到标题框

---

## 配色方案（固定浅色主题）

| 元素 | 颜色 |
|---|---|
| 窗口背景 | `#FAFAFA` |
| 标题栏背景 | `#F0F0F0` |
| 文字主色 | `#1E1E1E` |
| 文字次级（日期） | `#A0A0A0` |
| 复选框边框 | `#CCCCCC` |
| 按钮悬停 | `#E8E8E8` |
| 逾期红色 | `#D32F2F` |
| 已完成文字 | `#C0C0C0` |
| 分割线 | `#E8E8E8` |
| 输入框背景 | `#FFFFFF` |
| 输入框边框 | `#DDDDDD` |

---

## 内存优化

- 零 NuGet 程序集加载
- TaskRow 控件复用（`Bind()` 重新绑定）
- 隐藏到托盘时不销毁窗口
- 字体/画刷静态缓存
- 同步 I/O（JSON <100KB）
- 正确释放托盘图标、定时器

**预期内存**：冷启动 8-12MB，200 条任务 14-18MB。

---

## 关键实现细节

1. **单实例互斥** — `Mutex`，第二个实例激活已有窗口
2. **关闭即隐藏** — `OnFormClosing` 拦截 `UserClosing`，Hide 不退出
3. **编辑模式** — Label/TextBox 叠加，双击切换，Enter 提交 / Escape 取消
4. **原子保存** — 写 `.tmp` → `File.Replace` 原子替换
5. **逾期检测** — `System.Windows.Forms.Timer` 每 60 秒扫描一次，发现逾期刷新列表 + 托盘气泡
6. **DDL 输入** — 简单 TextBox，解析 `yyyy/MM/dd` 格式，非法输入忽略

---

## 构建方式

```batch
MSBuild.exe D:\111\SimpleTodo\SimpleTodo.csproj /p:Configuration=Release
```

产出：单个 `SimpleTodo.exe`。

---

## 验证计划

1. 编译零错误零警告
2. 运行 → 托盘图标 + 左下角定位
3. 添加任务（无 DDL / 有 DDL）
4. 编辑 → 完成 → 删除
5. 子任务 → 折叠展开 → 删除父任务确认
6. 关闭窗口 → 隐藏到托盘
7. 手动改 tasks.json 设一个过去的 DDL → 重启 → 确认逾期红标
8. 检查 `%APPDATA%\SimpleTodo\tasks.json` 数据完整性
9. 任务管理器内存 <20MB
