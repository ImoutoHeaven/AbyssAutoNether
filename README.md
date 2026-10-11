# Abyss AutoNether

AutoNether is a standalone BepInEx IL2CPP plugin for Nether automation. F12 controls routing, battles, Code offers, events, rewards, shops, checkpoints, and settlement. `AbyssMod.dll` is optional.

**Current limitation:** the standalone build supplies no native-backed semantic provider for battle tiers and item classifications. Actions that depend on that evidence remain blocked as unknown. A successful startup check establishes binding compatibility, not complete unattended-run support.

## Install and start

1. Use a game installation with BepInEx IL2CPP and generated game interop assemblies.
2. Place `AutoNether.dll` in `BepInEx/plugins/AutoNether/` and restart the game.
3. Check the log for `AutoNether native compatibility precheck passed`.
4. Enter the Nether floor-selection screen and press F12 to toggle automation.

Settings live in `BepInEx/config/Abyss.AutoNether.cfg`. Saved changes reload automatically once the file stabilizes.

## Strategy modes

- **Equipment** (default): climbs to a Boss-aligned target and builds a safe Code portfolio. Eligible new Codes can fill spare capacity; replacing a held Code requires a strict improvement. Native Code power is a fallback when richer combat valuation is unavailable.
- **Research**: collects Codes from explicitly configured families, preferring the first incomplete target. It tries the other configured family before rerolling once or declining. It starts at floor zero and settles at a Boss when its objectives or Code capacity are reached, with a floor-70 ceiling.

Both modes enforce route safety and reject uniform crest grants that conflict with their actual recipients. See the [behavior specification](docs/specs/evidence-backed-strategy-modes.md) for the complete rules.

## Configuration

All keys are in `[AutoNether]`.

| Key | Default | Purpose |
|---|---|---|
| `MaximumDepth` | `150` | Equipment target; resolves to a Boss using current master data. |
| `StrategyMode` | `Equipment` | Select `Equipment` or `Research` explicitly. |
| `ResearchPrimaryFamily` | `Unknown` | Required for Research: `Rush`, `Impact`, `Safe`, or `Risk`. |
| `ResearchSecondaryFamily` | `Unknown` | Optional second family; `Unknown` disables it. Opposing pairs are invalid. |
| `SoftErosionLimit` | `90` | Projected erosion threshold, from 1 to 99. Mandatory Bosses may exceed the soft limit; 100 is always forbidden. |
| `MinimumCharacterHpPermille` | `300` | Route HP threshold, from 1 to 1000; 300 means 30%. |
| `CombatLane` | `Auto` | Equipment preference: `Auto`, `Rush`, or `Impact`. |
| `CodeReloadReserve` | `1` | Native rerolls to retain in Equipment mode. Research uses at most one fallback reroll per offer. |
| `EquipmentRecoveryCodeTransformEnabled` | `false` | Opt into Equipment's tightly gated random Recovery transform. |
| `TreasureMode` | `KeyOnly` | Use one held key first; an HP payment needs explicit survival and route proof. `Off` pauses at Treasure. |
| `ShopMode` | `Off` | Leave through the native flow, or use `EquipmentBags` for verified affordable purchases. |
| `DetailedLogging` | `true` | Enable bounded `[F12][AutoNether]` decision and lifecycle diagnostics. |
| `CheckpointPreserveItemIds` | empty | Decimal item IDs to prioritize for return, separated by commas, semicolons, or spaces. Empty uses the native safe default. |

Research cannot pair `Rush` with `Impact`, or `Safe` with `Risk`. Invalid settings, ambiguous ownership, and insufficient safety evidence stop automation before the affected action.

## Battles and optional F11 support

AutoNether temporarily enables native battle Auto and maximum speed, then restores the original settings at battle termination, F12 disable, terminal pause, or unload. Restoration requires the exact native accessors.

The recovery record is `BepInEx/config/Abyss.AutoNether/battle-settings-lease.json`. Keep a valid crash-left record: it allows restoration when the accessors become available.

With AbyssMod installed, F11 may wrap the final battle-start task while F12 coordinates Nether progress. AutoNether waits for that task without replaying or cancelling it. Each plugin retains its own settings and toggle; disabling F12 leaves F11 alone.

## Troubleshooting

- **Load failure:** retain the `[AutoNether precheck]` errors. A game binding mismatch requires updated bindings; bypassing validation is unsafe.
- **F12 does not start:** confirm the plugin loaded and the game is on the Nether floor-selection screen. Look for `event=hotkey-input` and `event=toggle-result`.
- **Automation pauses:** keep `DetailedLogging=true` and retain the log from startup through the first pause. Unknown semantics, stale snapshots, and unsafe routes are deliberate stop conditions.
- **`BindingUnavailable`:** indicates missing structural binding or ownership evidence. An unsuitable Code offer is normally declined instead.

## Developer reference

See the [domain glossary](CONTEXT.md), [behavior specification](docs/specs/evidence-backed-strategy-modes.md), and [architecture and verification guide](docs/design/autonether-architecture.md).
