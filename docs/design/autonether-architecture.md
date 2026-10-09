# AutoNether Architecture

Runtime ownership, compatibility, and verification for `AutoNether.dll`. The [behavior specification](../specs/evidence-backed-strategy-modes.md) defines policy; the [glossary](../../CONTEXT.md) defines domain terms.

## Product and startup

AutoNether is a BepInEx IL2CPP plugin with GUID `Abyss.AutoNether`, display name `Abyss AutoNether`, root namespace `AutoNether`, and hotkey F12. Configuration is `BepInEx/config/Abyss.AutoNether.cfg`; installation is `BepInEx/plugins/AutoNether/`.

`Plugin.Load()` binds logging, validates native compatibility, binds configuration, creates the hotkey component, installs registered Harmony patches, and initializes the controller, in that order. Validation failures log each incompatible contract and abort before configuration, components, patches, or controller initialization. This is the first executable plugin boundary, after managed assembly loading.

AbyssMod is an optional soft dependency. AutoNether has no `AbyssMod.dll` reference and owns its own configuration and lifecycle. Translation and F11 Auto-SL are separate products.

## Compatibility

`NetherNativeBindingCatalog` supplies shared descriptors for reflected methods and members. Validation checks declaring type, exact method identity, static/instance ownership, ordered parameter types, return type, required member paths, and registered Harmony targets. Generated callback/task methods require their exact identities; runtime validation also checks required callback singleton instances.

`NetherCompiledInteropPreflight` resolves compiled game and Unity references, including direct calls and accessors. `ValidateMetadata` supports offline contract tests; runtime scene and popup instances are checked at their ownership boundaries. The startup log reports the method, generated-method, member, and compiled-reference counts actually checked.

`PatchManager.PatchTypes` is the installation list. Compatibility and contract tests cover battle lifecycle, floor selection, checkpoint/start/result flows, interaction popups, Code transactions, and battle-setting accessors.

For a game update, verify current assembly hashes, exact types and signatures, and relevant decompiled control flow using read-only inputs. Update bindings, characterization tests, and affected current rules together. A game-design conflict overrides an approved requirement only with that evidence, as required by [AGENTS.md](../../AGENTS.md).

## Runtime responsibilities

| Area | Main code | Responsibility |
|---|---|---|
| Entry and configuration | `Core/Plugin.cs`, `Config.cs`, `Hotkey.cs`, `ConfigAutoReload.cs` | Validate startup, bind settings, poll stable file changes, and dispatch F12/update ticks. |
| Lifecycle observations | `Patches/` | Report native controllers, popups, tasks, and terminal boundaries. |
| Flow ownership | `NetherAutoClimbController`, `NetherAutoClimbStateMachine`, lifecycle coordinators | Own phases and parent/child transactions; wait and reconcile before advancing. |
| Native adaptation | `NetherRuntimeBridge`, binding and capture services | Bind live owners, capture snapshots, invoke proven native flows, and observe results. |
| Decisions | Code, route, event, erosion, and boundary policies | Compare authoritative options after safety and compatibility checks. |
| Diagnostics and recovery | Audit services and battle-settings lease services | Explain decisions and retain restoration state across interruptions. |

A mutation requires enabled automation, a current native owner, matching decision/snapshot generations, and exclusive transaction ownership. Pending tasks block progress without synchronous result retrieval or replay. Confirmation invalidates unexecuted route valuation; planning resumes from fresh state after terminal confirmation or an exact child handoff.

Popup readiness is polled only while its owner remains current. Candidate-local rejection uses native cancel/decline when legal. Structural binding, callback, or owner ambiguity produces `BindingUnavailable`; unsafe branches and unknown dependent content are excluded locally where possible.

### Typed semantic evidence

`Plugin.Load()` supplies no semantic-provider factory. The controller can use a registered factory, and the bridge accepts its evidence only for the current snapshot fingerprint. The standalone build ships no native-backed provider, so dependent raw battle/item fields remain unknown and their actions stop safely. Binding compatibility alone does not establish semantic completeness.

## Battle tasks and settings

The `Priority.Last` battle-start postfix captures the final Nether `StartQuestAsync` task after optional F11 wrapping. AutoNether waits while it is pending, continues once after successful reconciliation, and pauses on fault or cancellation. F12 does not read or change F11 state, cancel its work, or replay its request.

Battle-setting leases temporarily enable native Auto and maximum speed. Restoration is required at battle termination, F12 disable, terminal pause, unload, and crash recovery. The record lives at `BepInEx/config/Abyss.AutoNether/battle-settings-lease.json`. Exact native accessors must confirm restoration before the record is removed; unresolved restoration blocks route mutation.

## Build and verification

The plugin targets .NET 6; tests target .NET 8. Set `ABYSS_GAME_DIR` to the game root so the projects can resolve `BepInEx/interop` and test-time `BepInEx/core` assemblies. The default Release artifact is `release/Release/net6.0/AutoNether.dll`.

Run restore, tests, builds, decompilation, and binary inspection in ephemeral Docker containers. Check `docker info` first. Use a versioned image suitable for the tool, such as `mcr.microsoft.com/dotnet/sdk:8.0` for this solution. Mount source and game inputs read-only with explicit `--mount type=bind` arguments; build from a disposable copy inside the container. Limit writable host mounts to the artifact directory that needs output.

From that disposable repository copy, with `ABYSS_GAME_DIR` set to the container's game mount:

```sh
dotnet restore AutoNether.sln
dotnet test AutoNether.Tests/AutoNether.Tests.csproj --no-restore
dotnet build AutoNether.sln -c Release --no-restore -warnaserror
sh scripts/verify-product-isolation.sh
sh scripts/verify-release.sh release/Release/net6.0/AutoNether.dll
```

Verification requires current native compatibility tests, focused policy/lifecycle regressions, the full test suite, a warning-free Release build, both audits, and `git diff --check`. These are gates, not a claim that the current checkout has passed them.

The isolation script checks product identities, forbidden cross-product source dependencies, and final-task capture. The Release script checks DLL identity strings and forbidden symbols using `strings -el`; its container must provide that tool.

Deploy only the verified DLL to `BepInEx/plugins/AutoNether/` and compare SHA-256 with the build artifact. Deployment does not launch the game or modify unrelated files.

## Source navigation

All runtime and policy services are under `AutoNether/Services/`; regression and contract tests are under `AutoNether.Tests/`. Start with `Plugin.cs` for startup, `NetherNativeCompatibilityPreflight.cs` for bindings, `NetherRunBoundaryPolicy.cs` for run limits, `NetherCodePolicy.cs` and `NetherResearchObjectivePolicy.cs` for offers, and `NetherRouteEncounterVectorPolicy.cs` / `NetherEventPolicy.cs` for route and interaction rules.
