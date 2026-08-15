# Abyss AutoNether

`AutoNether.dll` 是独立的 BepInEx IL2CPP 深渊自动爬塔插件。它可以单独安装；同时安装 `AbyssMod.dll` 时，也可以和 F11 Auto-SL 配合，但两个开关、配置与生命周期互不修改。

## 安装与使用

1. 将 `AutoNether.dll` 放入 `BepInEx/plugins/AutoNether/`。
2. 启动游戏并进入深渊的楼层选择画面。
3. 按 F12 开启或关闭自动爬塔。F12 只在深渊内生效。
4. 首次启动后配置位于 `BepInEx/config/Abyss.AutoNether.cfg`，修改后会自动重载。

插件会自动选择经安全策略证明可达的路线，处理战斗、事件、奖励确认、商店、深渊代码、十层 checkpoint、Continue 与 Result。进入战斗时会临时启用游戏原生 Auto 与最高倍速，并在战斗结束、关闭 F12、暂停或卸载时恢复原设置。

## 与 AbyssMod F11 共存

- 只开 F12：按服务器原始掉落进入战斗并自动爬塔。
- 只开 F11：保留 AbyssMod 的手动选路 + 战斗 Auto-SL。
- F11 与 F12 同时开启：F12 自动选路；进入每场战斗前，F11 按自己的配置完成重刷；AutoNether 等待最终 task 完成后才继续。
- 关闭 F12 不会取消或修改 F11；关闭 F11 也不会修改 F12。

这种共存不引用 `AbyssMod.dll`，只在同一个游戏 `StartQuestAsync` 返回值上以最低 Harmony postfix 优先级观察最终 task。因此没有安装 AbyssMod 时仍可独立工作。

## 配置

配置段为 `[AutoNether]`：

- `MaximumDepth = 130`：最大目标层数；仍受服务器和主数据上限约束。
- `StrategyMode = Equipment`：显式策略模式，默认为后期装备爬塔；也可由用户改为 `Research`，插件不会自动探测模式。
- `ResearchPrimaryFamily = Unknown`：`Research` 模式必须显式设为 `Rush`、`Impact`、`Safe` 或 `Risk`；保持 `Unknown` 会在任何原生开塔动作前拒绝配置。
- `ResearchSecondaryFamily = Unknown`：可选的副研究家族；`Unknown` 表示禁用。未知值、与主家族相同或互相对立的组合会在开塔前拒绝。
- `SoftErosionLimit = 90`：预计达到该侵蚀度前暂停；100 始终为硬停止。
- `MinimumCharacterHpPermille = 300`：战斗入场与后续路线偏好的 HP 软门槛，300 表示 30%。普通活动的确定性扣血只以“每名当前存活角色扣后仍大于 0”为硬资格；软门槛不会把仍存活的活动结果误判为死亡。
- `CombatLane = Auto`：深渊代码流派，可为 `Auto`、`Rush`、`Impact`。
- `CodeReloadReserve = 1`：保留的深渊代码重抽次数。
- `TreasureMode = KeyOnly`：优先选择已验证的单钥匙选项。没有钥匙时，HP 面板形状本身不构成放宽死亡规则的证明；只有该选项同时绑定权威、可达的 rank-5 宝箱目标，或被证明是唯一可到达正常终点的路线，才允许“至少一名角色存活”的窄例外。当前生产接线没有这种预验证路线证明时会安全暂停。`Off` 也会安全暂停。
- `ShopMode = Off`：默认通过原生关闭流程离店；`EquipmentBags` 购买已验证且买得起的装备袋。
- `DetailedLogging = true`：输出有界的 `[F12][AutoNether]` 诊断日志。
- `CheckpointPreserveItemIds =`：十层 checkpoint 优先送回的十进制物品 ID；可用逗号、分号或空格分隔，留空走安全默认策略。

无效配置、未知主数据或无法唯一确认的原生绑定会 fail-closed：插件暂停并记录原因，不会猜测 API、重复 mutation 或偷偷跳过功能。

活动中的“扣 HP 换宝箱钥匙”同样不会仅凭 `Damage + NetherKey` 内容形状放宽规则。它还必须绑定一个权威可达的 rank-5 宝箱目标、确认当前没有钥匙，并证明可达范围内不存在更优且买得起的深渊币钥匙来源；否则仍按普通活动要求每名存活角色扣后都大于 0。无论哪种窄例外，扣血后全队死亡始终拒绝。

开塔边界同样由显式模式决定：`Research` 从 0 层开始且目标不会超过权威 70 层 Boss；`Equipment` 从不高于归一化 Boss 目标的最高已解锁十层 checkpoint 开始，没有可用 checkpoint 时从 0 层开始。目标层会对齐当前主数据中的权威 Boss，缺少权威楼层或启动绑定时不会发出开塔请求。

## 日志与排查

加载成功时应看到：

```text
Abyss AutoNether 0.1.0 loaded; F12 controls Nether auto-climb.
[F12][AutoNether][Diag] event=build ... abyssModDetected=True|False interop=final-task-capture
```

按 F12 后应看到 `event=hotkey-input`、`event=toggle-result`，随后是 `audit=snapshot`、`audit=route` 和原生 task/reconcile 日志。若没有触发：

1. 确认 `LogOutput.log` 中插件名为 `Abyss AutoNether`，而不是只看到 `AbyssMod`。
2. 确认 DLL 位于独立目录，且未误用旧 autonether-testing 的 `AbyssMod.dll`。
3. 必须先进入深渊楼层选择画面；其他场景的 F12 会被拒绝。
4. 保持 `DetailedLogging=true`，提交从 `event=build` 到首次 `event=pause` 或 `toggle-result` 的完整日志。
5. 若日志显示 `BindingUnavailable`，不要反复点击或手动绕过；它表示当前客户端绑定需要更新。

持久化战斗设置租约位于 `BepInEx/config/Abyss.AutoNether/battle-settings-lease.json`。崩溃后不要手动删除活动租约；插件会在下次取得精确战斗设置访问器时恢复用户原设置并删除它。

当前已知安全边界：如果 F12 是在一个早已存在、且无法再取得所属 parent task 的 Event/Recovery/Treasure `Wait` 流程中开启，插件会命名暂停、零 mutation、零 GET，要求用户完成该界面后重新开启。
