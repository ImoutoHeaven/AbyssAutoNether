# Standalone AutoNether Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce an independent `AutoNether.dll` by migrating the proven F12 implementation from `AbyssModMod/autonether-testing` while removing translation and F11 ownership and preserving optional F11 interoperability through final-task Harmony ordering.

**Architecture:** Import the existing solution as a mechanical migration baseline, rename the product identity to `AutoNether`, and retain only the F12 Nether runtime. Replace the two mixed AbyssMod patches with AutoNether-owned battle start/terminal observers; synchronize with optional F11 only by capturing the final `StartQuestAsync` `UniTask` after AbyssMod's postfix. Use the existing state machine and native-flow tests as the regression authority, with new isolation and coexistence tests guarding the split.

**Tech Stack:** C# 12, .NET 6 BepInEx IL2CPP plugin, .NET 8 xUnit tests, HarmonyX, Docker, read-only game interop assemblies.

## Global Constraints

- Source baseline is exactly `AbyssModMod/autonether-testing@4dd2f879af962a663fe163127da64f95f2e9daad`.
- F11 baseline and separate owner remain `AbyssModMod/master@f4b79c1314cb93b9a7c56be16027a983f93ce397`.
- Product identity is `AutoNether.dll`, root namespace `AutoNether`, plugin GUID `Abyss.AutoNether`, display name `Abyss AutoNether`, version `0.1.0`.
- `AbyssMod` is an optional BepInEx soft dependency; there is no assembly reference, static-field access, configuration access, or cancellation control across plugins.
- F11 and F12 never modify or cancel each other.
- All restore, test, build, and binary inspection commands run in `docker run --rm`.
- The game directory is mounted read-only; no build or deployment writes into it.
- Existing game-native mutation/task flows are retained. Unknown bindings fail closed; no raw API fallback is introduced.
- Implementation occurs in the user-selected fresh `Abyss-AutoNether` repository, which itself is the isolation boundary; no linked worktree is created.

---

### Task 1: Import the Proven F12 Baseline and Establish Product Identity

**Files:**
- Create from source snapshot: `AutoNether/`, `AutoNether.Tests/`, `AutoNether.sln`
- Create: `scripts/verify-product-isolation.sh`
- Modify: `AutoNether/AutoNether.csproj`
- Modify: `AutoNether.Tests/AutoNether.Tests.csproj`

**Interfaces:**
- Consumes: Git object `4dd2f879af962a663fe163127da64f95f2e9daad` from `AbyssModMod`.
- Produces: an imported solution whose project/assembly paths and namespaces are `AutoNether`, plus an executable static isolation audit.

- [ ] **Step 1: Write the failing product-isolation audit**

Create `scripts/verify-product-isolation.sh` with assertions for `AutoNether.sln`, `AutoNether/AutoNether.csproj`, `AssemblyName=AutoNether`, `RootNamespace=AutoNether`, and absence of an `AbyssMod.dll` reference. Run it before import and verify it fails because the project is absent.

- [ ] **Step 2: Import the source snapshot mechanically**

Use `git archive 4dd2f879 -- AbyssMod AbyssMod.Tests AbyssMod.sln`, extract into the new repository, rename directories/projects to `AutoNether`, and mechanically rewrite C# namespaces and project paths. Do not copy the old `.git`, build outputs, docs, release DLL, or experimental history.

- [ ] **Step 3: Set the new build identity**

Set `AssemblyName`, `Product`, and `RootNamespace` to `AutoNether`, version to `0.1.0`, and build output under the new product directory. Preserve read-only game references through `ABYSS_GAME_DIR`; do not reference an installed AbyssMod assembly.

- [ ] **Step 4: Run the isolation audit to GREEN**

Run `scripts/verify-product-isolation.sh` in Docker. Expected: all identity/path checks pass and the script exits 0.

- [ ] **Step 5: Commit**

Commit as `chore: import AutoNether migration baseline`.

### Task 2: Remove Translation and Normal/F11 Surface

**Files:**
- Delete: translation/LLM/F6/F11-only files under `AutoNether/Core`, `AutoNether/Patches`, `AutoNether/Services`, `AutoNether/Models`
- Delete: corresponding tests under `AutoNether.Tests`
- Modify: `AutoNether.Tests/AutoNether.Tests.csproj`
- Modify: `scripts/verify-product-isolation.sh`

**Interfaces:**
- Consumes: mechanically imported product.
- Produces: a source/test tree containing only F12 Nether automation and generic helpers required by it.

- [ ] **Step 1: Extend the audit and verify RED**

Add forbidden source symbols and files: `MachineTranslator`, `TranslationManager`, `TranslationPatch`, `BattleSessionAutoSL`, F6/F8/F9/F11 input handlers, `AbyssMod.Services`, and any `Reference Include="AbyssMod"`. Run and verify the copied baseline fails.

- [ ] **Step 2: Delete unrelated source and tests**

Retain all `Nether*.cs` production and test files, plus only proven generic dependencies such as configuration reload primitives. Remove translation, normal/idle Auto-SL, F6 inspector, enhancement and unrelated item/UI patches.

- [ ] **Step 3: Narrow test compilation**

Remove deleted linked production files and unrelated tests from `AutoNether.Tests.csproj`. Preserve every Nether policy/state/native-flow/E2E test that does not assert the obsolete F11 busy state.

- [ ] **Step 4: Run the audit to GREEN**

Expected: no forbidden code ownership or assembly reference remains.

- [ ] **Step 5: Commit**

Commit as `refactor: isolate AutoNether source surface`.

### Task 3: Create the Independent Plugin Entry, Configuration, and F12 Lifecycle

**Files:**
- Rewrite: `AutoNether/Core/Plugin.cs`
- Rewrite: `AutoNether/Core/Config.cs`
- Rewrite: `AutoNether/Core/Hotkey.cs`
- Rewrite: `AutoNether/Patches/PatchManager.cs`
- Retain/modify: `AutoNether/Core/ConfigAutoReload.cs`, `AutoNether/Core/Logger.cs`
- Create tests: `AutoNether.Tests/AutoNetherPluginContractTests.cs`
- Create tests: `AutoNether.Tests/AutoNetherConfigContractTests.cs`

**Interfaces:**
- Produces: `Plugin.Load/Unload`, F12-only `Hotkey.Update`, independent config entries, and an AutoNether-only patch registry.

- [ ] **Step 1: Write plugin contract tests and verify RED**

Characterize source metadata and configuration ownership: exact GUID/name/version, soft dependency on `AbyssMod`, F12 present, F11 absent, no translation initialization, and `CheckpointPreserveItemIds` owned by AutoNether.

- [ ] **Step 2: Implement the minimal entrypoint**

Initialize logging, config, config auto-reload, F12 MonoBehaviour, patches, persisted lease recovery, and controller. On unload call the controller cleanup. Do not initialize HTTP, fonts, Toast, mapping, or translation.

- [ ] **Step 3: Implement independent configuration**

Bind maximum depth, soft erosion, minimum HP, combat lane, reload reserve, treasure mode, shop mode, detailed logging, and `CheckpointPreserveItemIds` under `Abyss.AutoNether.cfg`. Migrate existing defaults and comments faithfully.

- [ ] **Step 4: Implement F12-only hotkey behavior**

Poll config reload and controller every frame, debounce F12, log input/dispatch, and toggle only AutoNether. F11 and all translation keys are absent.

- [ ] **Step 5: Implement the patch registry and run tests GREEN**

Register only Nether lifecycle patches and the two new battle observer patches defined in Task 4. Run focused plugin/config tests.

- [ ] **Step 6: Commit**

Commit as `feat: add standalone AutoNether plugin lifecycle`.

### Task 4: Replace Direct F11 Coupling with Final-Task Capture

**Files:**
- Create: `AutoNether/Patches/NetherBattleStartTaskCapturePatch.cs`
- Create: `AutoNether/Patches/NetherBattleTerminalPatch.cs`
- Modify: `AutoNether/Services/NetherRuntimeBridge.cs`
- Modify: `AutoNether/Services/NetherBattleSettlementCoordinator.cs`
- Modify: `AutoNether/Services/NetherAutoClimbModels.cs`
- Modify: `AutoNether/Services/NetherAutoClimbStateMachine.cs`
- Modify: `AutoNether/Services/NetherAutoClimbController.cs`
- Modify tests: state, settlement, ingress and production E2E suites
- Create tests: `AutoNether.Tests/NetherBattleTaskInteropContractTests.cs`

**Interfaces:**
- Produces: a postfix which captures the final `UniTask<BattleSessionStatusResponseEntity>` after optional AbyssMod wrapping, and a terminal observer independent of F11 tracing.

- [ ] **Step 1: Write coexistence and standalone RED tests**

Assert that patch metadata declares `HarmonyAfter("AbyssMod.Patches.BattleSessionAutoSLPatch")`; no `AbyssMod` CLR type is referenced; Pending blocks GET/scene progress; Succeeded continues once; Faulted/Canceled pauses; F12-off drains without cancel/replay.

- [ ] **Step 2: Implement final-task capture patch**

Patch `ExplorationQuestPreserveAPIService.Project_Ingame_Exploration_IExplorationQuestAPIService_StartQuestAsync`. Detect Nether through the wrapped `_apiService`/`NetherAPIService`, then pass the current postfix `__result` to `NetherRuntimeBridge.ObserveBattleStartTask`. Never replace the task.

- [ ] **Step 3: Split terminal observation from settlement tracing**

Create a Nether-only prefix on `BattleResultUtility.CreateBattleResultModel` that captures authoritative character HP and clear/close terminal evidence. Remove payload tracing and normal/disaster settlement logging from AutoNether.

- [ ] **Step 4: Remove the busy-state protocol**

Delete `IsF11Busy`, `AwaitingF11`, `ObserveF11Busy`, and all `BattleSessionAutoSL.HasActiveNetherOperation` references. `PollBattleStart/PollBattleLifecycle` use only captured task status and native clear/close evidence.

- [ ] **Step 5: Run focused tests GREEN**

Run task interop, ingress, settlement, state, and E2E tests. Confirm Pending creates zero GET and zero replay, while one successful terminal task creates exactly one continuation.

- [ ] **Step 6: Commit**

Commit as `feat: coordinate optional F11 through final battle task`.

### Task 5: Complete Config Decoupling, Naming, and Persistent Paths

**Files:**
- Modify: `AutoNether/Services/NetherAutoClimbController.cs`
- Modify: `AutoNether/Services/NetherRuntimeBridge.cs`
- Modify: `AutoNether/Services/NetherBattleSettingsLease.cs`
- Modify: all production log prefixes still using `[F12][NetherClimb]` or `[Info : AbyssMod]` assumptions
- Create/modify tests: config, diagnostic logger, checkpoint preflight, lease lifecycle

**Interfaces:**
- Produces: independent checkpoint preserve policy, data paths, diagnostics, and lease recovery.

- [ ] **Step 1: Write RED tests for independent ownership**

Assert checkpoint planning reads `CheckpointPreserveItemIds`, persistent lease path is under the AutoNether data directory, and diagnostic prefix is `[F12][AutoNether]`.

- [ ] **Step 2: Replace borrowed F11 configuration**

Route checkpoint parsing exclusively to AutoNether config. Do not infer or synchronize F11 target IDs.

- [ ] **Step 3: Replace paths and diagnostics**

Move durable lease state to AutoNether's own plugin data directory and update F12 log identity. Add load-time diagnostics for plugin version, optional AbyssMod presence, and task-capture patch ordering.

- [ ] **Step 4: Run focused tests GREEN**

Run checkpoint, lease, config and diagnostics suites.

- [ ] **Step 5: Commit**

Commit as `refactor: own AutoNether config and runtime state`.

### Task 6: Restore Full Regression Coverage and Build the Standalone DLL

**Files:**
- Modify as required: `AutoNether.Tests/AutoNether.Tests.csproj`
- Create: `scripts/verify-release.sh`
- Create: `README.md`

**Interfaces:**
- Produces: fully tested source, independent release DLL, and concise install/use documentation.

- [ ] **Step 1: Run the full test suite and resolve only migration regressions**

Run the clean, self-contained Docker restore/test command below. `NuGet.config` at the repository
root supplies nuget.org, the BepInEx feed, and the Samboy feed, so this command intentionally has no
ad-hoc `--source` or `--source` workaround flags. Keep the game mount read-only and preserve all
relevant migrated tests; do not make tests pass by deleting valid route/native-flow coverage.

~~~text
MSYS_NO_PATHCONV=1 docker run --rm --mount type=bind,src=/c/Users/Eden/PixelAbyssX/Abyss-AutoNether,dst=/src --mount type=bind,src=/c/Users/Eden/PixelAbyssX/dotabyss_x_cl,dst=/game,readonly -w /src mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim bash -lc 'set -euo pipefail; export ABYSS_GAME_DIR=/game; export NUGET_PACKAGES=/tmp/nuget; dotnet test AutoNether.Tests/AutoNether.Tests.csproj -c Release --nologo --logger "console;verbosity=minimal"'
~~~

Fresh clean-cache execution `j-g4xeyq` restored both projects from the repository-configured
sources and passed 1184/1184. The pre-fix clean-cache RED `j-urwq7m` failed with NU1101 for
`BepInEx.Unity.IL2CPP` and `BepInEx.PluginInfoProps` because only nuget.org was visible.

- [ ] **Step 2: Build against the read-only current game**

Mount the game directory as `/game:ro`, set `ABYSS_GAME_DIR=/game`, and run the Release build in
the same Docker image with the repository's `NuGet.config`; expected result is 0 errors and 0
warnings.

- [ ] **Step 3: Audit the binary and package**

Verify the output assembly name, managed references, embedded strings, plugin GUID, config name, and absence of `AbyssMod.dll`, translation, LLM, and F11 handlers. Copy only `AutoNether.dll` into repository `release/`; do not deploy it.

- [ ] **Step 4: Document installation and coexistence**

Document standalone F12 behavior, optional F11 coexistence, independent toggles, independent config files, expected logs, safe pause behavior, and uninstall/lease recovery notes.

- [ ] **Step 5: Fresh final verification**

Run `scripts/verify-product-isolation.sh`, full tests, `scripts/verify-release.sh`, read-only Release build, `git diff --check`, and `git status`. Report exact test counts and remaining live-only IL2CPP validation boundary.

Historical pre-repair Docker gate record (not final): task-group HEAD `1e1e7a0d6f0215910e9b7d1254c7771d217326ea`, parent
`5f3de38572d5526e73e8576ffe505669c1c8dbc3`; threshold-focused `j-l9m0j6` passed 111/111,
expanded focused `j-529vvn` passed 230/230, full `j-nyu1bj` passed 1186/1186, and Release
`j-3ulak2` passed with 0 warnings/0 errors. Read-only evidence audit `j-ju3ij7` recorded
`EVIDENCE_AUDIT_PASS=1`, `PRODUCT_ISOLATION_PASS=1`, and `DIFF_CHECK_EXIT=0` with
`/game` mounted read-only. The threshold RED/GREEN are `j-kmf1l2` (2 failures) and
`j-1ro4y6` (2/2), supported by fresh native decomp `j-86iu89` (`CPP2IL_EXIT=0`,
`DIFFABLE_EXIT=0`).

Current final-repair pre-amend record: worktree HEAD `beb8824604298da985965bad332b24ac9d7845c7`,
parent `5f3de38572d5526e73e8576ffe505669c1c8dbc3`; focused `j-b6bdes` passed 128/128,
expanded `j-g23akv` passed 337/337, full `j-n7g4ih` passed 1201/1201, and Release `j-dbw8t4`
passed with 0 warnings/0 errors. Read-only audit `j-1lixdk` passed product isolation and
`git diff --check`. The final SHA is intentionally not duplicated in this pre-amend plan text:
the plan is part of the amended tree, so embedding a commit's own object ID changes that object;
the post-amend Docker audit prints the exact final HEAD/parent/tree and is authoritative.

Current procurement-repair pre-amend record: `HEAD=b837c5ce1822b3b05990ff34df62ad75a974877e`,
parent `5f3de38572d5526e73e8576ffe505669c1c8dbc3`. Fresh public RED `j-xyz67j` was 3/3
failures; GREEN `j-3l24iu` was 3/3; focused `j-4gqmbu` was 123/123; expanded `j-u32ye3` was
189/189; full `j-eg7dic` was 1208/1208; and Release `j-hrbptk` was 0 warnings/0 errors.
Fresh RO native decomp jobs `j-p18rn7` and `j-phv0bh` both returned CPP2IL/DIFFABLE exit 0 and
used `/game` read-only. The post-amend Docker audit and
`refs/notes/logic-overhaul-evidence` remain authoritative for the final SHA/tree and isolation /
diff results because the plan itself is part of the amended tree.

- [ ] **Step 6: Commit**

Commit as `build: complete standalone AutoNether migration`.

Current raw-ItemType-overflow pre-amend record: `HEAD=ffa3ef96ba7862456e668195fdea6207b69543a5`,
parent `5f3de38572d5526e73e8576ffe505669c1c8dbc3`. Fresh public RED `j-lzlkcg` failed 1/1
with the exact positive raw `ItemType=2147483648` overflow; GREEN `j-ofgd9w` passed 1/1.
Warning-free focused `j-8dph58` passed 207/207, expanded `j-ie7n3h` passed 477/477, full
`j-1y9bfs` passed 1209/1209, and Release/isolation/diff `j-gwj93w` passed Release 0 warnings/0
errors, product isolation, and diff check 0. Fresh RO native Cpp2IL `j-bghfub` and post-fix
`j-5l2ncz` both passed with `MItems.cs` hash
`e69e8310aa256e60e356e84e857e1b7f92f056a952c03b96f9182e865cfd0d27` and raw `long type` at
source line 11. The post-amend Docker audit and Git note remain authoritative for final SHA/tree.
