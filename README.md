# Abyss AutoNether

`AutoNether.dll` 是独立的 BepInEx IL2CPP 深渊自动爬塔插件。它负责 F12 自动选路、战斗、事件、奖励、商店、深渊代码、checkpoint、Continue 与 Result；不安装 `AbyssMod.dll` 也能运行。

## 安装与启动

1. 将 `AutoNether.dll` 放入 `BepInEx/plugins/AutoNether/`。
2. 完整重启游戏。
3. 确认日志出现兼容性预检成功信息。
4. 进入深渊楼层选择画面后按 F12 开启或关闭自动爬塔。

配置文件为 `BepInEx/config/Abyss.AutoNether.cfg`，保存后自动重载。

## 启动兼容性预检

AutoNether 在读取自己的配置、创建组件、初始化 controller 或安装 Harmony patch 之前，严格检查它依赖的全部游戏原函数、反射调用和 IL2CPP generated callback/task。类型、函数名、静态/实例归属、参数或返回类型任一不匹配，插件都会拒绝加载。

兼容时日志包含：

```text
AutoNether native compatibility precheck passed: methods=62, generated=18.
```

不兼容时日志包含一项或多项 `[AutoNether precheck]` 错误，随后由 BepInEx 报告插件加载失败。此时不要继续自动爬塔或手动绕过；需要针对当前游戏程序集更新原生绑定。

## 策略模式

### Equipment

默认模式。安全门槛通过且 Code 容量未满时，Equipment 会直接领取不会造成负面机制或混色纹章冲突的新 Code，不要求严格提升；多个候选优先按完整机制战斗价值排序，无法量化时使用游戏原生 `power × 实际目标人数`，仍不可比较时按 Code ID 稳定选择。只有容量已满、必须替换旧 Code 时才要求完整 portfolio 严格提升；完整机制无法量化时，原生战力数字可作为兜底，但不会推翻已证明为零或负收益的结果，也不会绕过安全门槛。

### Research

研究模式只追求配置家族的研究收益，不用显示战力、侵蚀收益或一般战斗价值筛选同家族候选。当前目标家族优先；没有时选择另一已配置家族；两者都没有时最多重抽一次，仍没有则原生取消本次 Offer。

主家族研究目标完成后，副家族成为优先目标，主家族降为 fallback。Code 容量满后不再替换：插件取消后续 Offer，并在下一场 Boss 结算窗口正常结束本次 Research run。

两种模式都拒绝会把错误纹章统一发给目标范围内角色的 Code。判断按实际 scope 进行：只有前排、后排或其他目标集合内部存在不兼容角色时才拒绝，不会因为队伍其他位置是混色而误杀安全候选。

完整行为规范见 [`docs/specs/evidence-backed-strategy-modes.md`](docs/specs/evidence-backed-strategy-modes.md)。

## 配置

配置段为 `[AutoNether]`：

| 键 | 默认值 | 含义 |
|---|---:|---|
| `MaximumDepth` | `130` | Equipment 请求的最大深度；实际目标对齐当前主数据中的 Boss。 |
| `StrategyMode` | `Equipment` | `Equipment` 或 `Research`；不会自动推断。 |
| `ResearchPrimaryFamily` | `Unknown` | Research 必填：`Rush`、`Impact`、`Safe` 或 `Risk`。 |
| `ResearchSecondaryFamily` | `Unknown` | 可选副家族；`Unknown` 表示禁用。 |
| `SoftErosionLimit` | `90` | 路线预计到达该侵蚀百分比前暂停；100 始终是硬停止。 |
| `MinimumCharacterHpPermille` | `300` | 路线安全使用的角色 HP 软门槛；300 表示 30%。 |
| `CombatLane` | `Auto` | Equipment 的 `Auto`、`Rush` 或 `Impact` 偏好。 |
| `CodeReloadReserve` | `1` | Equipment 保留的原生 Code 重抽次数。Research 的单次 fallback 重抽不使用多次预算。 |
| `EquipmentRecoveryCodeTransformEnabled` | `false` | 允许 Equipment 在严格条件下使用 Recovery 随机 Code 转换。Research 始终禁用。 |
| `TreasureMode` | `KeyOnly` | 优先使用已验证的一把钥匙选项；无法证明安全选择时暂停。 |
| `ShopMode` | `Off` | `Off` 原生离店；`EquipmentBags` 购买已验证且买得起的装备袋。 |
| `DetailedLogging` | `true` | 输出 `[F12][AutoNether]` 决策与生命周期诊断。 |
| `CheckpointPreserveItemIds` | 空 | checkpoint 优先送回的十进制物品 ID，支持逗号、分号或空格分隔。 |

无效配置、未知主数据、过期 snapshot、无法唯一绑定的 popup 或不安全路线都会 fail closed；AutoNether 不猜测隐藏数据，不重复 mutation，也不调用未经证明的原始 API 替代游戏流程。

## 与 AbyssMod F11 共存

- 只开 F12：AutoNether 使用服务器原始战斗响应自动爬塔。
- 只开 F11：AbyssMod 保持自己的手动选路与 Auto-SL 行为。
- F11 与 F12 同时开启：F12 负责流程；F11 可以包装每场战斗的最终 task。AutoNether 等最终 task 结束后才继续。
- 两个开关、配置和生命周期互不修改；关闭 F12 不是取消 F11 的命令。

AutoNether 只观察最终 `StartQuestAsync` task，不引用 `AbyssMod.dll`，也不读取 AbyssMod 的静态状态或配置。

## 战斗设置与恢复

进入战斗时，AutoNether 临时启用游戏原生 Auto 与最高倍速。战斗结束、关闭 F12、终端暂停或插件卸载时恢复原设置。

持久租约文件为 `BepInEx/config/Abyss.AutoNether/battle-settings-lease.json`。崩溃后不要手动删除仍有效的租约；插件会在重新取得精确原生设置访问器后恢复设置。

## 日志排查

按 F12 后应依次看到 `event=hotkey-input`、`event=toggle-result`，随后出现 snapshot、route、native task 和 reconcile 记录。

若没有启动：

1. 检查兼容性 precheck 是否通过。
2. 确认日志插件名为 `Abyss AutoNether`，DLL 位于独立目录。
3. 确认已经进入深渊楼层选择画面。
4. 保持 `DetailedLogging=true`，保留从 precheck/build 到首次 pause 或 toggle-result 的完整日志。
5. `BindingUnavailable` 只表示结构性绑定/所有权证据不足；普通候选不合适会取消该 Offer，而不会伪装成绑定失败。

## 开发与验证

运行架构、当前原生兼容性基线和 Docker 验证约束见 [`docs/design/autonether-architecture.md`](docs/design/autonether-architecture.md)。产品隔离与 Release 二进制审计分别由 `scripts/verify-product-isolation.sh` 和 `scripts/verify-release.sh` 执行。
