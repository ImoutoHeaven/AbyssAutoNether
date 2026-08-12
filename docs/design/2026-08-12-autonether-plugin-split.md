# AutoNether 独立插件拆分设计

日期：2026-08-12

迁移来源：`AbyssModMod/autonether-testing@4dd2f879af962a663fe163127da64f95f2e9daad`

F11 基线：`AbyssModMod/master@f4b79c1314cb93b9a7c56be16027a983f93ce397`

## 1. 目标

将当前 `autonether-testing` 中的 F12 深渊自动爬塔从 `AbyssMod` 拆成可以独立安装、独立配置和独立发布的 BepInEx IL2CPP 插件。

- `AbyssMod.dll` 继续由 `AbyssModMod/master` 维护翻译、LLM 机翻、F6 掉落 ID 检视和 F11 Auto-SL。
- `AutoNether.dll` 由新仓库 `Abyss-AutoNether` 维护 F12 自动爬塔。
- `AutoNether.dll` 不安装 `AbyssMod.dll` 时仍能完成自动选路、战斗、结算、事件、商店、代码选择和 checkpoint。
- 两个插件同时安装时，F11 可以在 F12 发起的每场深渊战斗前重刷掉落；两个开关互不修改、互不代管生命周期。

## 2. 非目标

- 不把翻译、LLM、F6、Normal/Idle Exploration Auto-SL 复制到新仓库。
- 不在 AutoNether 内实现第二套 F11 Auto-SL。
- 不让 AutoNether 直接引用 `AbyssMod.dll`、读取其静态字段或修改其配置。
- 不保留 `autonether-testing` 作为两个产品的长期开发分支；它只作为迁移审计来源。
- 不改变游戏服务端协议，不以原始 API 请求替代已经验证的原生客户端流程。

## 3. 产品与仓库边界

### AbyssModMod

- 主分支：`master`
- 产物：`AbyssMod.dll`
- 插件 GUID：保持 `AbyssMod`
- 配置：`BepInEx/config/AbyssMod.cfg`
- 快捷键：F6、F8、F9、F10、F11
- 所有 F11 策略、cooldown、Normal/Idle/Nether 掉落条件继续归它所有。

### Abyss-AutoNether

- 主分支：`master`
- 产物：`AutoNether.dll`
- 程序集与根命名空间：`AutoNether`
- 插件 GUID：`Abyss.AutoNether`
- 插件显示名：`Abyss AutoNether`
- 初始版本：`0.1.0`
- 配置：`BepInEx/config/Abyss.AutoNether.cfg`
- 发布目录：`BepInEx/plugins/AutoNether/`
- 快捷键：只拥有 F12；不得处理 F11 或翻译快捷键。

新插件声明对 `AbyssMod` 的 BepInEx soft dependency。该声明只用于确定可选的加载顺序，不构成编译期或运行期必需依赖。

## 4. 源码结构

新仓库采用独立 solution，不通过链接文件、submodule 或跨仓库 ProjectReference 共享实现：

```text
Abyss-AutoNether/
  AutoNether.sln
  AutoNether/
    AutoNether.csproj
    Core/
      Plugin.cs
      Config.cs
      Hotkey.cs
      Logger.cs
    Patches/
      PatchManager.cs
      Nether*.cs
    Services/
      Nether*.cs
  AutoNether.Tests/
    AutoNether.Tests.csproj
    Nether*.cs
  docs/design/
```

从迁移来源复制所有真正属于 F12 的 `Nether*` 服务、补丁和测试，并把命名空间改为 `AutoNether`。共享入口文件按新插件职责重写，不复制旧 `Plugin`、`Config`、`Hotkey`、`PatchManager` 后再用条件分支隐藏功能。

## 5. F11 与 F12 的组合协议

### 5.1 原则

F11 是 `StartQuestAsync` 的透明响应中间件；F12 是深渊流程调度器。AutoNether 的正确性只依赖它捕获到的最终 `UniTask`，不依赖 `AbyssMod` 的 `HasActiveNetherOperation`、attempt 计数或掉落列表。

### 5.2 Harmony 顺序

1. 游戏创建原生 `StartQuestAsync` task。
2. 如果安装 AbyssMod，其 `BattleSessionAutoSLPatch` postfix 可以把 `__result` 替换为 Auto-SL 包装 task。
3. AutoNether 的 battle-task capture postfix 必须排在该 postfix 之后，捕获此时最终的 `__result`。

AutoNether 同时使用以下约束：

- BepInEx soft dependency：`AbyssMod`。
- Harmony `after` owner：当前稳定 owner `AbyssMod.Patches.BattleSessionAutoSLPatch`。
- 自身稳定 Harmony owner，不复用任何 `AbyssMod.*` owner。

如果 AbyssMod 不存在，`after` 约束自然为空，AutoNether 捕获原生 task。

### 5.3 task 状态语义

- `Pending`：原生加载尚未完成，或 F11 正在 cooldown/重刷；F12 保持等待，不 GET、不重投、不切场景。
- `Succeeded`：F11 已接受目标响应，F11 已关闭并接受现有响应，或根本没有 F11；F12 才进入后续权威只读对账。
- `Faulted` / `Canceled`：F12 命名暂停并保留诊断，不自行重放请求。
- task 尚未注册：有界等待；超时后暂停，不猜测进入战斗。

AutoNether 删除 `IsF11Busy`、`AwaitingF11` 以及对 `BattleSessionAutoSL.HasActiveNetherOperation` 的直接依赖。等待最终 task 已包含全部必要同步语义。

### 5.4 开关语义

- 仅 F12：自动爬塔，战斗使用原生一次响应。
- 仅 F11：用户手动选路，每场战斗按 F11 策略 Auto-SL。
- F11 + F12：F12 自动选路；每次战斗由 F11 包装 task 重刷至满足条件，再进入战斗。
- 两者都关：完全原生。
- F11 与 F12 永不修改对方配置或开关。
- 用户在 F11 重刷期间关闭 F12：AutoNether 等当前 task 安全终止后转为 Disabled，不取消 F11。
- 用户关闭 F11：AbyssMod 按自身既有语义接受当前或上一份有效响应；F12 继续运行。
- 用户要立即停止重刷时必须关闭 F11；关闭 F12 不是 F11 的取消命令。

## 6. 配置所有权

以下项目迁入 `Abyss.AutoNether.cfg`：

- `MaximumDepth`
- `SoftErosionLimit`
- `MinimumCharacterHpPermille`
- `CombatLane`
- `CodeReloadReserve`
- `TreasureMode`
- `ShopMode`
- `DetailedLogging`
- checkpoint/return 选择策略

旧的 `BattleSessionAutoSLNetherPreserveItemIds` 同时承担了两个不同职责，拆分后不再共享：

- `AbyssMod.cfg` 的 F11 配置表示“什么掉落能让重刷停止”。
- `Abyss.AutoNether.cfg` 的 `CheckpointPreserveItemIds` 表示“十层 checkpoint 优先送回什么”。

AutoNether 不隐式读取、复制或同步 `AbyssMod.cfg`。希望两者采用相同物品 ID 时，由用户在两个配置中明确填写；这避免单独安装、热重载或插件版本不一致时出现隐藏行为。

配置全部支持 AutoNether 自己的实时重载。配置无效时保留现有 fail-closed 原则：记录具体键和值并暂停，不自动回退到危险策略。

## 7. 入口、日志与生命周期

- 新 `Plugin.Load()` 只初始化 AutoNether 配置、F12 hotkey、F12 Harmony patches、controller 和持久 lease 恢复。
- 不初始化 HTTP、翻译缓存、字体、Toast、MachineTranslator 或 MasterMapping。
- 日志源显示为 `AutoNether`，消息前缀统一为 `[F12][AutoNether]`；迁移后不再输出 `[F12][NetherClimb]` 与 `AbyssMod` 混合身份。
- 加载日志必须报告 build profile、插件版本、是否检测到 AbyssMod，以及 capture patch 的实际排序状态。
- 卸载、F12 关闭、场景丢失和终端暂停继续恢复原生 Auto/最高倍速 lease，并清除所有 owner/task/popup 注册。
- 持久 lease 文件移入 AutoNether 自己的插件数据目录，不能与 `AbyssMod` 路径共用。

## 8. 构建与游戏目录约束

- 所有 restore、test、build 和反编译验证都在 `docker run --rm` 中执行。
- 游戏目录只读挂载为 `/game:ro`；反编译资料只读挂载。
- 构建产物写入新仓库的 `release/` 或临时构建目录，不自动部署到游戏目录。
- 项目不得引用 `BepInEx/plugins/AbyssMod/AbyssMod.dll`。
- 若仍需 `Utility.dll`，必须先证明 AutoNether 实际需要；默认设计为移除 Toast/字体依赖，从而不引用它。

## 9. 迁移与提交顺序

1. 初始化新 Git 仓库并提交本设计。
2. 建立最小 `AutoNether` 插件与测试项目，使空插件能够在只读游戏引用下构建。
3. 迁移纯模型、策略、状态机和单元测试，完成命名空间重写。
4. 迁移 runtime bridge、Harmony patch 与生产 E2E seam。
5. 重写入口、配置、日志、F12 hotkey 和持久路径。
6. 以测试先行移除所有 F11/翻译引用，并实现最终 task capture 顺序。
7. 执行完整测试、Release build、程序集依赖审计和双插件静态装载审计。
8. 生成 `AutoNether.dll`，但未经明确批准不部署到游戏目录。
9. 新仓库验证完成后，将 `AbyssModMod/autonether-testing` 标记为迁移来源，不在该分支继续实现新功能。

迁移采用新仓库的干净提交历史，不复制 `autonether-testing` 的实验提交链；原提交仍可在旧仓库追溯。

## 10. 验收标准

### 静态隔离

- `AutoNether.dll` 的程序集引用和字符串扫描中不存在对 `AbyssMod.dll`、翻译、LLM、F6/F11 handler 的硬依赖。
- `AbyssMod.dll` 与 `AutoNether.dll` 具有不同程序集名、插件 GUID、配置文件、Harmony owner 和数据目录。
- 新插件只注册 F12/Nether 所需 patches。

### 自动流程

- standalone：不安装 AbyssMod，F12 可以从 FloorSelection 完整运行至下一稳定楼层。
- coexist/F11 off：安装两者但 F11 关闭，行为与 standalone 相同。
- coexist/F11 on：包装 task 在多次 retry 中保持 Pending；AutoNether 不提前 GET/切场景，最终接受后仅继续一次。
- F12 off during F11：不取消或重放 F11；task 结束后 AutoNether 变为 Disabled。
- F11 off during retry：AbyssMod 接受响应后，AutoNether 正常继续。
- fault/cancel/missing task：命名暂停，零重复 mutation。

### 回归

- 迁移来源中已有的路线、侵蚀/HP、战斗设置 lease、战斗结算、事件、奖励确认、商店、代码选择、checkpoint、Continue 和 Result 测试全部保留并通过。
- 新增 capture-order、standalone、coexist、mid-flight toggle 和独立配置测试。
- Docker full test 通过；使用只读游戏目录的 Release build 为 0 error、0 warning。

## 11. 风险与现场边界

- Harmony owner 或游戏方法签名在客户端更新后变化：启动时输出精确 resolver/ordering 诊断；无法证明最终 task 时 fail closed。
- 两插件实际加载顺序与静态预期不符：测试 patch metadata；现场日志报告 owner 与排序，禁止退回 `IsBusy` 轮询补丁。
- recovered direct-Wait Event/Recovery/Treasure 仍没有可归属 parent task：维持既有命名暂停、零 mutation、零 GET 的安全边界。
- 首次双 DLL 游戏现场验证属于最终交付边界；静态测试不能宣称替代真实 IL2CPP/Harmony 装载验证。
