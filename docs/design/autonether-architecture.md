# AutoNether Architecture

This document defines the current product boundary, startup contract, runtime ownership model, and verification requirements for `AutoNether.dll`.

## Product boundary

AutoNether is a standalone BepInEx IL2CPP plugin with these identities:

| Property | Value |
|---|---|
| Assembly | `AutoNether.dll` |
| Root namespace | `AutoNether` |
| Plugin GUID | `Abyss.AutoNether` |
| Display name | `Abyss AutoNether` |
| Configuration | `BepInEx/config/Abyss.AutoNether.cfg` |
| Installation directory | `BepInEx/plugins/AutoNether/` |
| Hotkey | F12 |

AutoNether owns only Nether automation. Translation, LLM features, F6 inspection, and F11 Auto-SL remain outside this product.

`AbyssMod` is an optional BepInEx soft dependency. AutoNether does not reference `AbyssMod.dll`, read its configuration or static state, control F11, or share lifecycle ownership with it.

## Startup sequence

`Plugin.Load()` performs these operations in order:

1. Bind the plugin logger.
2. Run the native compatibility precheck.
3. Bind AutoNether configuration.
4. Create the F12 hotkey component.
5. Install the registered Harmony patches.
6. Initialize the automation controller.

Only logger binding occurs before compatibility validation so failures can name every incompatible contract. Any validation failure is logged, logger ownership is released, and plugin loading aborts; no AutoNether configuration, component, controller, or patch is initialized.

BepInEx and the CLR must load the managed assembly before `Plugin.Load()` can execute. The precheck is therefore the earliest executable plugin boundary, not a check performed before the operating system reads the DLL.

## Native compatibility contract

`NetherNativeBindingCatalog` records the versioned reflection method and member contracts required by the native adapters. Runtime method callers reuse the catalog's descriptors.

The current catalog validates:

- 62 unique game methods;
- 18 IL2CPP generated callbacks or async-task methods;
- required instance field/property paths and their exact value types;
- every Harmony patch class registered by `PatchManager`.

Each game contract includes the declaring type, exact method identity, static or instance ownership, ordered parameter types, and return type. Generated static methods require their exact packaged identity; a renamed method with the same signature does not satisfy startup validation.

`NetherCompiledInteropPreflight` also resolves the plugin's compiled game and Unity type/member references through the CLR. This covers direct calls and accessors, including dependencies reached only during later automation phases.

`Plugin.Load()` uses `Validate`, which reads each required generated callback singleton and rejects an unreadable or null instance. `ValidateMetadata` characterizes assembly contracts for offline tests. Scene controller and popup instances are checked at their runtime ownership boundaries; startup validates their required member paths. Code confirmation uses the same member resolver during precheck and invocation.

The catalog covers:

- battle start, terminal, result, and battle-result continuation;
- floor selection, start-status state machine, and floor-event sequencing;
- Event, Recovery, Treasure, Shop, Return, Code Offer, Code List, Continue, Skip, Finish, Boost, hint, and erosion popups;
- Code select, cancel, reroll, replace, and transform tasks;
- battle Auto and speed settings accessors;
- every reflected game mutation issued by the runtime bridge.

The precheck resolves cataloged Harmony targets before `PatchManager` installs the patches.

`PatchManager.PatchTypes` is the only patch installation list. Tests require every source `[HarmonyPatch]` class to appear exactly once.

## Current native baseline

The compatibility catalog is validated against this game baseline:

| Input | SHA-256 |
|---|---|
| `GameAssembly.dll` | `efcb1cb47f0c927012b4843c328f6179356e22988ad185e55087887badf97581` |
| `global-metadata.dat` | `b3b62451678ba469df334680414c49f35f0c7f29710d1c4866b4f85ad44c110a` |
| `BepInEx/interop/Project.dll` | `075179ed583d6c5c78e6e67f432a547bc1a30e944dc3231e0a05a2c813be07ea` |
| `BepInEx/interop/Absf.dll` | `c1ceb05bde4fe25f66b7e48039e7031c67046cf80e227a38bd4fd31e23d6aed9` |

The current run-start mutation is:

```csharp
Project.Nether.NetherUtility
    .TransitionNetherFloorSelectionSceneFromPartyAsync(
        int partyNo,
        int useTicket,
        int startFloorLevel,
        Il2CppSystem.Threading.CancellationToken cancellationToken
    ) : Cysharp.Threading.Tasks.UniTask
```

A game update changes the baseline only after a fresh read-only Docker decomp proves the new assembly hashes, exact type and member signatures, and relevant control flow. The catalog, characterization tests, this baseline, and any affected strategy rule change together.

## Runtime ownership

The controller advances only from authoritative snapshots. Every actionable state is bound to a runtime generation and the native owner that produced it.

A mutation is issued only when:

- automation is enabled and owns the current phase;
- the expected native controller and popup are current;
- the snapshot and decision belong to the same generation;
- no parent or child transaction already owns the action;
- every required game binding passed startup validation.

After issuing a mutation, AutoNether waits for its native task or callback and reconciles the observed server/native state. It does not repeat the action because a frame remained in `Wait`, and it does not plan the next route behind an active child popup or battle.

Every confirmed mutation invalidates unexecuted route valuation. The next plan starts from a fresh snapshot after terminal confirmation or an exact child handoff.

## Popup and task lifecycle

Popup registration records the native controller, owner generation, runtime generation, and available callbacks. Model readiness is polled only while the owning controller remains current.

Candidate-local strategy rejection uses the popup's native decline/cancel path and continues the run. `BindingUnavailable` is reserved for structural contract, owner, or callback evidence that cannot safely issue any native action.

Pending native tasks are observed without synchronous `Get`, replay, or replacement. Faulted and cancelled tasks produce a named terminal pause.

## Optional F11 interoperability

F11 is a transparent battle-response middleware; F12 is the Nether flow coordinator. AutoNether installs a lowest-priority postfix on `StartQuestAsync` and observes the final `UniTask` after any optional AbyssMod wrapping.

- Pending means wait without replay or scene transition.
- Succeeded allows authoritative reconciliation and one continuation.
- Faulted or cancelled pauses AutoNether.
- Closing F12 does not cancel F11.

The plugins never modify each other's configuration or toggle state.

## Battle settings lease

AutoNether temporarily enables native Auto and maximum speed while it owns a battle. The original values are restored on battle termination, F12 disable, terminal pause, plugin unload, or recovery from the persisted lease.

The lease is stored under `BepInEx/config/Abyss.AutoNether/`. A lease is removed only after exact native accessors prove restoration.

## Failure model

Failures are local whenever ownership remains authoritative:

- an unknown Code excludes that candidate;
- an unknown Event part excludes that option;
- an unknown inventory row excludes that purchase;
- an unsafe branch excludes that route.

The controller pauses only when no proven legal choice remains, configuration is invalid, transaction identity is ambiguous, or a structural native contract is unavailable. It never substitutes raw API calls or UI clicks for an unproven native flow.

## Repository verification

All restore, test, compilation, native decompilation, and binary inspection run in ephemeral Docker containers. The game and decompilation inputs are mounted read-only; writable mounts are limited to explicit build artifacts or authorized source output.

Required gates are:

1. Native compatibility tests against the packaged current game interop assemblies.
2. Focused tests for the changed policy or lifecycle boundary.
3. The full `AutoNether.Tests` suite.
4. Warning-free Release build of `AutoNether.sln`.
5. `scripts/verify-product-isolation.sh`.
6. `scripts/verify-release.sh` against the built DLL.
7. `git diff --check`.
8. SHA-256 equality between the verified artifact and any deployed game DLL.

Deployment copies only the verified `AutoNether.dll` into `BepInEx/plugins/AutoNether/`. It does not launch the game or mutate unrelated files.
