<p align="center">
  <a href="README.md">English</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.ru.md">Русский</a> |
  <a href="README.pt-BR.md">Português (BR)</a>
</p>

> **机器翻译。** 这份说明由机器翻译，欢迎校正。[提交翻译修正](https://github.com/johnstonstu/ror2-ah64/issues/new?template=translation.yml)

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/banner.jpg" alt="AH-64，雨中冒险 2 的阿帕奇幸存者" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=23fc79&style=for-the-badge" alt="Thunderstore 版本"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.downloads&label=downloads&color=ff8a28&style=for-the-badge" alt="Thunderstore 下载量"></a>
  <a href="https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT 许可证"></a>
</p>

<p align="center"><b>雨中冒险 2 的 AH-64 阿帕奇武装直升机幸存者。</b></p>

<p align="center">喜欢的话，请在 <a href="https://thunderstore.io/package/JohnstonStu/AH64/">Thunderstore 给 AH64 点赞</a>，方便其他玩家找到它。</p>

> **抢先体验：** AH-64 仍在调整。错误报告、平衡设置和本局细节都会影响下一次更新。
>
> **[报告错误](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml)** · **[分享平衡反馈](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml)** · **[讨论](https://github.com/johnstonstu/ror2-ah64/discussions)**

<h3 align="center">武装直升机。一个从不着陆的幸存者。</h3>

**AH-64** 把拥挤的战场变成武装直升机的突击航线。贴近地形悬停，像地面幸存者一样横向移动，同时让机头炮塔跟踪雷达目标。向敌群连续发射 Hydra 火箭，再选择特殊技能：长弓锁定、激光制导地狱火，或沿航线投下炸弹。

按住跳跃键爬升，用横滚、后空翻或倾侧转弯重新占位。三种主武器、三种机动技能、三种特殊技能和五种涂装，让你配置自己的武装直升机。

[安装](#install) · [控制](#flight-controls) · [技能](#the-kit) · [组合](#put-it-together) · [反馈](#help-balance-ah-64)

**1.3 新内容**

激光制导地狱火、倾侧急转和沿航线投弹加入武器配置。横滚与后空翻保留进入机动时的动量；特殊技能和机动技能各有可见挂件。M230 弹鼓现在为20发。完整记录见[更新日志](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md)。

<a id="flight-controls"></a>

## 飞行控制

按住**跳跃**爬升，按住**下降**（默认手柄 **B**、键盘 **C**）降低高度。松开两者即可保持高度。高于静止悬停高度时，滞空时间会按飞行方式消耗；回到静止悬停高度即可补充。额外跳跃增加滞空时间和爬升高度。准星下的小刻度显示剩余时间。

在原地转向和射击消耗较慢。快速飞行、爬升和远离起飞点消耗更快；击杀会短暂暂停消耗。回到静止悬停高度补充时间并拾取地面物品。

可选的**经典高度控制**恢复松开跳跃键即下降的方式，并取消滞空时间限制。

<a id="the-kit"></a>

## 武器配置

以下数值为默认设置。视频展示当前1.3版本，在 Titanic Plains、Distant Roost 和 Siphoned Forest 拍摄。这些是游戏内的受控展示，采用脚本输入和专门的镜头构图。

### 被动 — 火控雷达

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="被动 — 火控雷达">

标记附近最强的威胁。对标记目标增加12%伤害，面向目标时增加15%移动速度，靠近目标时获得30护甲。机头炮塔独立于视角跟踪目标。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/fire-control-radar.webp" width="640" alt="被动 — 火控雷达">

### 主武器 — M230 链式机炮

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Chaingun.png" width="64" alt="主武器 — M230 链式机炮">

20发弹鼓，一次全部装填。弹丸和高爆爆炸在10米内造成75%伤害，30米起达到全额伤害。远距离点射可保持集中。攻击速度同时改善射击和装填。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m230-chain-gun.webp" width="640" alt="主武器 — M230 链式机炮">

### 主武器变体 — XM301 转管炮

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Gatling.png" width="64" alt="主武器变体 — XM301 转管炮">

六管转管炮，从60发弹鼓中逐渐加速到每秒约18发。弹丸和爆炸比 M230 轻，同样在10米内造成75%伤害，30米起达到全额。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/xm301-rotary-cannon.webp" width="640" alt="主武器变体 — XM301 转管炮">

### 主武器变体 — M789 重型加农炮

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Cannon.png" width="64" alt="主武器变体 — M789 重型加农炮">

每秒2.5发的缓慢重型炮弹，每发带大范围爆炸。弹鼓8发，每次射击都会使机身后坐。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m789-heavy-cannon.webp" width="640" alt="主武器变体 — M789 重型加农炮">

### 副武器 — Hydra-70 火箭巢

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64RocketPods.png" width="64" alt="副武器 — Hydra-70 火箭巢">

在接近一秒内连续发射火箭：整个齐射期间保持瞄准。火箭飞行8米内造成75%伤害，25米起达到全额。超过基础6发的每枚火箭增加0.8秒装填时间。主武器装填时，火箭巢仍可使用。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/hydra-70-pods.webp" width="640" alt="副武器 — Hydra-70 火箭巢">

### 机动技能 — 规避横滚

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64EvasiveJink.png" width="64" alt="机动技能 — 规避横滚">

按移动方向选择向前斜上方的桶滚。前半段无敌，整个横滚约0.95秒获得200护甲。保留进入时的动量，平滑加速后衔接回正常飞行。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/evasive-roll.webp" width="640" alt="机动技能 — 规避横滚">

### 机动技能变体 — 烟幕后空翻

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64SmokeBackflip.png" width="64" alt="机动技能变体 — 烟幕后空翻">

向后冲刺并爬升完成俯仰翻转，同时释放烟幕并短暂隐形。保留进入时的动量，平滑恢复飞行。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/smoke-backflip.webp" width="640" alt="机动技能变体 — 烟幕后空翻">

### 机动技能变体 — 倾侧急转

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BrakingTurn.png" width="64" alt="机动技能变体 — 倾侧急转">

保持飞行并完成大幅度90度倾侧转弯。移动输入选择左右；无方向输入时向右转。全程可以瞄准和开火。冷却4秒。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/banked-break.webp" width="640" alt="机动技能变体 — 倾侧急转">

### 特殊技能 — AGM-114L 长弓

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Longbow.png" width="64" alt="特殊技能 — AGM-114L 长弓">

按住特殊技能标记雷达锁定，同时继续使用机炮和 Hydra；松开后发射。基础挂架容纳6枚导弹，每层溶解电池增加1枚。第6次锁定之后，单枚导弹伤害不再增加。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/agm-114l-longbow.webp" width="640" alt="特殊技能 — AGM-114L 长弓">

### 特殊技能变体 — AGM-114 地狱火

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Hellfire.png" width="64" alt="特殊技能变体 — AGM-114 地狱火">

按下特殊技能发射慢速主导弹。按住显示激光，并使该导弹加速飞向瞄准位置；松开以减速，再次按住可恢复对同一导弹的制导，不消耗弹药。前一枚主导弹结束后才能发射新的。主、副武器始终可用。便携式 I.C.B.M. 增加两枚无制导扇形导弹；只有主导弹跟踪激光。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/guided-hellfire.webp" width="640" alt="特殊技能变体 — AGM-114 地狱火">

### 特殊技能变体 — 轰炸航线

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BombingRun.png" width="64" alt="特殊技能变体 — 轰炸航线">

沿飞行路线以0.3秒间隔投下6枚炸弹。每枚造成300%伤害，爆炸半径6米，每个敌人每轮最多承受3次炸弹命中。继续飞行、瞄准，并用其他武器或机动技能改变投弹轨迹。被中断后的剩余投弹丢失。冷却10秒。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/bombing-run.webp" width="640" alt="特殊技能变体 — 轰炸航线">

<a id="put-it-together"></a>

## 组合使用

- **装填时保持火力。** 主武器和 Hydra 独立装填。机炮装填时发射火箭，并保持瞄准到齐射结束。M230、XM301 和 Hydra 在远处伤害更高。
- **射击时标记。** 装备长弓后，按住特殊技能积累锁定，同时继续使用机炮和 Hydra；松开后把导弹送入战场。
- **制导、松开、再次制导。** 使用地狱火时，按住特殊技能控制主导弹。松开以减速并调整角度，再次按住控制同一枚导弹。机炮和 Hydra 全程可用。
- **画出轰炸轨迹。** 爬升接近敌群，在敌群上方启动轰炸航线，并在投弹过程中继续移动。倾侧急转可使航线弯曲，同时保留瞄准和射击。每个敌人每轮最多受3次炸弹命中，因此应把投弹分散到敌群。

## 选择你的武装直升机

每种主武器都有独立的机头组件。特殊技能可添加开放式长弓导轨、封闭式地狱火发射架或炸弹挂架；机动技能添加推进器、烟幕罐或倾侧鳍片。更改配置会同时更新角色选择模型和游戏中的机体，每个主动技能都有独立的绘制图标。

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/modular-loadouts.webp" width="640" alt="Three special and utility attachment combinations">

五种涂装可以用于游戏：橄榄绿、沙漠黄、极地、军绿，以及通过 AH-64 精通成就解锁的夜行者（在季风难度通关或自我抹除）。

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="AH-64 1.2 模型的五套涂装：橄榄、沙漠黄、极地、军绿和夜行者" width="100%">
</p>

*Blender 模型预览；游戏内光照会有差别。标题横幅是更早机身版本的宣传图。*

## 语言

AH-64 跟随你在雨中冒险 2 里选择的语言（**设置 → 语言**）。已包含简体中文、俄文和巴西葡萄牙语。其他语言回退到英文。游戏内的调整菜单保持英文。

这些翻译是机器翻译。欢迎校正，见[翻译说明](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/TRANSLATING.md)。

<a id="install"></a>

## 安装

**模组管理器（推荐）：** 用 [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) 或 Thunderstore Mod Manager 安装。所需依赖会自动装上。

**手动：** 把压缩包里的 `plugins` 内容复制到 `BepInEx/plugins/JohnstonStu-AH64/`。保持这样的布局：

```text
JohnstonStu-AH64/
  AH64.dll
  AH64.language
  AssetBundles/ah64
  SoundBanks/AH64Rotor.bnk
```

`AH64.language` 必须放在 `BepInEx/plugins` 下面的某处。本包把它放在 DLL 旁边。没有这个文件时，游戏会回退到英文。

大厅里的每个人都需要**相同的模组版本**。玩法调整只在本地生效，多人开局前先统一设置。

### 所需依赖

- `bbepis-BepInExPack-5.4.1905`
- `RiskofThunder-R2API_Core-5.0.3`
- `RiskofThunder-R2API_Prefab-1.0.1`
- `RiskofThunder-R2API_RecalculateStats-1.0.0`
- `RiskofThunder-R2API_Language-1.0.1`
- `RiskofThunder-R2API_Sound-1.0.2`

<h2 id="help-balance-ah-64">帮助平衡 AH-64</h2>

我希望 AH-64 手感对，最快的办法就是看到你实际在用的设置。你的设置能告诉我玩家在平衡上哪里意见一致，也是提出还没有的滑条的地方。

调整菜单需要 [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/)。它是可选的：没有它，AH-64 会用默认值运行。

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/feedback/balance-flow.jpg" alt="如何分享 AH-64 设置：调滑条，按复制并打开 GitHub 把设置复制到剪贴板，如果表单是空的就粘贴并写上备注，然后提交" width="100%">
</p>

1. 打开 **设置 → 模组选项 → AH-64** 并调整滑条。分页是 Movement、M230、XM301 Gatling、M789 Cannon、Utility、Presentation、Audio 和 Feedback。这些菜单保持英文。基础速度、加速度、主武器装填时间和规避横滚冷却在重启后生效。
2. 可选：在 **Feedback → Comments** 里写备注。
3. 在任一页末尾按 **Share settings → Copy & open GitHub**。设置报告（每一项 AH-64 设置加上你的备注）会进剪贴板，反馈表单会在 GitHub 打开。
4. **AH-64 settings and comments** 栏通常已经填好。如果是空的，粘贴进去（**Ctrl+V**）。报告已经在剪贴板上。然后填写 **What kind of feedback** 和 **Your idea**，包括哪里太强或太弱。
5. 点击 **Create**。

**Feedback → Copy all settings** 会复制同一份报告，但不打开浏览器。

**隐私：** 不会自动发送任何内容。按钮只复制到剪贴板并打开表单；预填表单把报告放在链接里，但在你点击 **Create** 之前什么都不会提交。提交需要 GitHub 账号。

### 错误、想法和问题

- **错误：** [打开错误报告](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml)。写上版本、复现步骤、关卡、主机或客户端，以及 `BepInEx/LogOutput.log` 或配置代码。
- **平衡或具体功能请求：** [打开反馈表单](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml)。讨论平衡时贴上设置报告。
- **一般讨论：** [提问](https://github.com/johnstonstu/ror2-ah64/discussions/categories/q-a)、[说说手感](https://github.com/johnstonstu/ror2-ah64/discussions/categories/general)，或 [提出想法](https://github.com/johnstonstu/ror2-ah64/discussions/categories/ideas)。

## 已知限制

- 导弹挂架的消耗反映的是本地特殊技能存量，包括上色锁定时长弓占用的数量。远程玩家和中途加入的表现仍需要专门验证。
- 玩法调整不会在玩家之间同步。
- 超出地面传感器的深坑使用普通下落物理。跳板和升降处理已经改进；如果某个关卡仍有问题，请报告。

## JohnstonStu 的其他模组

**[Hollow Saint](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/)**: 风暴圣者幸存者，使用连锁闪电、可附着的风暴长矛，以及响应命中的雷霆。

## 致谢与许可

- 使用 R2API 和雨中冒险 2 模组社区的幸存者框架制作。
- 机身和武器几何为本模组在 Blender 中从零建模。
- 当前旋翼循环：**aquinn 的 “Helicopter Sounds”**，CC0。完整来源和处理说明见 [Art/Audio](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Audio/README.md) 和包内的 `LICENSE_SOURCE.txt`。
- qubodup 的旧旋翼录音为 CC0，来源仍有记录。其他游戏音效使用原版雨中冒险 2 的 Wwise 事件。

[MIT](https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE) © 2026 Stu Johnston。第三方音频保留其 CC0 许可。

版本历史见[更新日志](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md)。

---

<details>
<summary><b>从源码构建</b></summary>

使用 Unity **2021.3.33f1**、内置渲染管线、.NET SDK 和 Wwise **2023.1.4.8496**（bank 格式 150）。改模型或 bundle 输入之前先读 [AGENTS.md](https://github.com/johnstonstu/ror2-ah64/blob/main/AGENTS.md)。[开发笔记](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/development/README.md) 涵盖音频、模型约束、工具和发布检查。

### 1. 构建 Unity assetbundle

用钉死的项目路径打开 `AH64UnityProject`。独立安装的 Windows 编辑器：

```powershell
& "C:/Program Files/Unity 2021.3.33f1/Editor/Unity.exe" -projectPath "<repo>/AH64UnityProject"
```

运行 **AH64 → Build AssetBundle**（`Ctrl+Alt+B`）。输出：`AH64UnityProject/AssetBundles/ah64`。只用这个 Unity 版本，以免资源被迁移。写入配置是可选的。

### 2. 构建音效 bank 和插件

在仓库根目录：

```powershell
powershell -ExecutionPolicy Bypass -File tools/build-rotor-bank.ps1
dotnet build AH64Mod/AH64.csproj -c Release /p:AH64DeployToProfiles=false
```

bank 脚本可以用 `-WwiseConsole` 指定别的安装路径。只发布 `AH64Rotor.bnk`，不要发布创作工程的 `Init.bnk`。见 [Art/Wwise/README.md](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Wwise/README.md)。

插件依赖来自 NuGet。构建会把 DLL 放到 `Build/plugins/`；自动部署到配置默认关闭。

### 3. 检查并打包

```powershell
powershell -ExecutionPolicy Bypass -File tools/check-language.ps1
powershell -ExecutionPolicy Bypass -File tools/check-feedback.ps1
powershell -ExecutionPolicy Bypass -File tools/check-weapon-previews.ps1
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -SkipBuild
```

打包脚本会检查版本一致、资源是否过期、音效 bank、图标尺寸和 ZIP 布局。它写出 `dist/AH64-<version>.zip`，不会上传。发布前在一个全新的模组管理器配置里测试这个 ZIP。

`AH64Plugin.MODVERSION` 和 `Build/manifest.json` 必须一致，并且使用纯 `major.minor.patch`。生成的 DLL、bundle、soundbank 和 ZIP 不入库；新克隆的仓库必须重新构建它们。

| 路径 | 内容 |
| --- | --- |
| `AH64Mod/` | C# BepInEx 插件 |
| `AH64UnityProject/` | Unity 工程和 bundle 源 |
| `Art/Blender/` | 程序化机身和武器源 |
| `Art/Audio/`、`Art/Wwise/` | 旋翼来源和 bank 创作工程 |
| `Build/` | Thunderstore 说明、清单和图标 |
| `tools/` | 构建、打包和检查工具 |

</details>
