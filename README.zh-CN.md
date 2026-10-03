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

**AH-64** 在地形上方悬停，并像地面角色一样平移。总距输入和规避动作提供短暂的高度。机头炮塔会跟踪雷达目标，方便你一边换位一边开火。

**1.2 的新内容**

- **选定高度并停在那里。** 按住跳跃爬升，下降（手柄 B，键盘 C）则下落。松开后飞机会保持该高度，在起伏地面上保持水平，直到滞空时间耗尽才缓缓回落。
- **战斗会奖励滞空。** 在一处悬停、转身射击几乎不消耗滞空；猛飞和远离则会更快耗尽。击杀会短暂暂停消耗，落回静息高度大约两秒就能回满。准星下方的白色小刻度显示剩余量。
- **你的道具会生效。** 霍普之羽、蜡鹌鹑、繁茂真菌、H3AD-5T v2、裂解细胞、备用弹匣、Eclipse Lite 和袖珍洲际导弹现在会配合 AH-64 的悬停和技能，表现与原版幸存者一致。悬停经过地面道具时会捡起它们。
- **它飞得像直升机，坠落时也像。** 武器会踢动机身，重击会把它打偏，转弯会侧倾，重伤时拖出引擎烟，死亡时旋转坠落并爆炸，而不是直接消失。
- **两套新涂装和一次模型整理。** 军绿，以及作为精通奖励的夜行者黑。沙漠黄取代了原来的沙漠。多面座舱玻璃、TADS 传感器转塔、剪刀式尾桨、会加速和减速的旋翼、色块皮肤图标和新的角色肖像。

**1.1：** 三种主武器各自的模型、角色选择界面里即时的武器预览、橄榄 / 沙漠 / 极地涂装、会响应操作的旋翼音效，以及可分享设置报告的可选平衡选项。

[Thunderstore](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/) · [更新日志](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) · [帮助平衡 AH-64](#help-balance-ah-64)

## 武器配置

| 槽位 | 技能 | 作用 | 游戏内 |
| :---: | --- | --- | :---: |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="火控雷达"><br>**被动** | **火控雷达** | 标记附近最强的威胁，靠近时提供 30 点护甲。机头炮塔的朝向与你的视线无关，所以换位时机炮仍对着目标。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/fire-control-radar.webp" width="320" alt="游戏中的火控雷达"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PrimaryIcon.png" width="64" alt="M230 链式机炮"><br>**主武器** | **M230 链式机炮** | 固定弹鼓，一次全部装填，而不是一发一发回弹。子弹和高爆破片在 10 米内造成 75% 伤害，从 30 米起为全额，所以远距离点射才能让短点射保持集中。无论有没有打空都会装填，投入交火前先补满。攻击速度也会加快装填，不只是射速。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m230-chain-gun.webp" width="320" alt="游戏中的 M230 链式机炮"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64GatlingIcon.png" width="64" alt="XM301 转管炮"><br>*主武器变体* | **XM301 转管炮** | 六管转管炮。炮管加速后射速升高，大约每秒 18 发，弹鼓 60 发。子弹和爆炸都比 M230 轻，同样是 10 米内 75%、30 米全额。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/xm301-rotary-cannon.webp" width="320" alt="游戏中的 XM301 转管炮"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64CannonIcon.png" width="64" alt="M789 重型加农炮"><br>*主武器变体* | **M789 重型加农炮** | 缓慢的重型炮弹，每秒 2.5 发，每发都有大范围爆炸。弹鼓 8 发，每一发都会踢动机身。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m789-heavy-cannon.webp" width="320" alt="游戏中的 M789 重型加农炮"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SecondaryIcon.png" width="64" alt="Hydra-70 火箭巢"><br>**副武器** | **Hydra-70 火箭巢** | 将近一秒的连射，齐射期间要稳住准星。火箭在飞出 8 米内造成 75% 伤害，从 25 米起为全额。基础六枚之后每多一枚，装填增加 0.8 秒。有自己的冷却，主武器装填时仍可使用。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/hydra-70-pods.webp" width="320" alt="游戏中的 Hydra-70 火箭巢"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64UtilityIcon.png" width="64" alt="规避横滚"><br>**辅助** | **规避横滚** | 沿爬升斜线向前的桶滚，前半段有无敌帧，整段翻滚约 0.95 秒内有 200 点护甲。按住跳跃爬升，下降（手柄 B，键盘 C）下落；两者都松开则保持高度。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/evasive-roll.webp" width="320" alt="游戏中的规避横滚"> |
| *辅助变体* | **烟雾后空翻** | 向后猛冲并做一个爬升的俯仰筋斗，重新加入战斗时短暂隐匿。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/smoke-backflip.webp" width="320" alt="游戏中的烟雾后空翻"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SpecialIcon.png" width="64" alt="AGM-114L 长弓"><br>**特殊** | **AGM-114L 长弓** | 按住可为雷达锁定上色，同时继续射击机炮和 Hydra，松开即发射。单枚伤害在第六个锁定之后不再提高。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114l-longbow.webp" width="320" alt="游戏中的 AGM-114L 长弓"> |
| *特殊变体* | **AGM-114 地狱火** | 从翼下导轨发射的瞄准、无制导导弹。主武器继续射击时也可以发射。 | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114-hellfire.webp" width="320" alt="游戏中的 AGM-114 地狱火"> |

## 选择你的武装直升机

每种主武器都有自己的机头组件。更换配装会立刻更新大厅模型。五套涂装会带进对局：橄榄、沙漠黄、极地、军绿，以及由 AH-64 精通成就解锁的夜行者（在季风难度下通关或湮灭）。

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="AH-64 1.2 模型的五套涂装：橄榄、沙漠黄、极地、军绿和夜行者" width="100%">
</p>

*Blender 模型预览；游戏内光照会有差别。标题横幅是更早机身版本的宣传图。*

## 语言

AH-64 跟随你在雨中冒险 2 里选择的语言（**设置 → 语言**）。已包含简体中文、俄文和巴西葡萄牙语。其他语言回退到英文。游戏内的调整菜单保持英文。

这些翻译是机器翻译。欢迎校正，见[翻译说明](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/TRANSLATING.md)。

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
- 激光制导的地狱火还没做。当前的地狱火变体无制导；雷达制导由长弓提供。

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
