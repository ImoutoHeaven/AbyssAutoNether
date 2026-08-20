# Fresh KeyNotFound RCA Evidence

Evidence ID: `forensic-keynotfound-20260820-e`

This directory records fresh, read-only Docker-native evidence for the
`_MNetherFloorTreasures_ not found` runtime failure. Generated native artifacts
and command results are copied here only from an ephemeral `docker run --rm`
container; the game tree is mounted read-only.

## Observed failure

The supplied `BepInEx/LogOutput.log` reaches a stable floor-selection snapshot,
then pauses at strategy evidence capture with:

`Il2CppException:System.Collections.Generic.KeyNotFoundException:_MNetherFloorTreasures_not_found`

The redacted exact signal is retained in sibling evidence
`forensic-keynotfound-20260820-d/log-relevant-signals.redacted.txt`.

## Immutable current-game inputs

- `BepInEx/interop/Project.dll`:
  `033a5d1e92df1f90d15b4f33312fb935327fd2baa87811b7860b227d6c1c75f4`
- `GameAssembly.dll`:
  `f2ad94781c161fe93040463b884c328599a40c78079aecacbe17a9b78edfc767`
- `ドットアビスX_Data/il2cpp_data/Metadata/global-metadata.dat`:
  `d7dffa623675ac493a0a4c7cfe8dc729bc37846b455a5284af94a901c1e25c27`

Cpp2IL acquisition, diffable output, and ISIL output all exited zero. Cpp2IL
reported `2022.1.0-pre-release.21+58fc404ac503f4e512055cafc48c03088fc6e224`
and Unity `6000.3.8f1`.

The original full searches ran against their complete ephemeral output trees.
This committed evidence retains only the native files that anchor the findings;
`isil-files.txt` and the SHA-256 manifests enumerate that curated set.

## RCA

1. Fresh `MasterDataStore` ISIL shows `GetCache<T>()` calling the backing
   dictionary's `TryGetValue`; its false branch constructs
   `KeyNotFoundException`. This exactly explains the supplied runtime log.
2. The residual `MNetherFloorTreasures` type still exists for generated
   serialization and contains only `id` and `m_nether_map_floor_id`, but a full
   current-native Nether namespace search finds zero flow references to it.
3. Fresh `NetherFloorModel.CreateModel` evidence resolves map floors,
   restrictions, battles, and battle stages, and carries the live `ExtendId`;
   it does not request `MNetherFloorTreasures`.
4. Fresh `NetherFloorMasterResolver` exposes Treasure/Event authority through
   `GetMNetherFloorEvents(mapFloorId, extendId)`: positive `extendId` selects the
   exact Event master row, with map-floor selection only as its native fallback.

The root cause was therefore the plugin's stale unconditional cache request,
not capture timing, an incomplete global master-store initialization, or a
corrupt local game tree.

## RED / GREEN

- RED: `red-test-output.txt` records the exact exception escaping the bounded
  capture seam; `red-repeat-output.txt` reproduces it 3/3 times.
- Focused GREEN: sibling evidence
  `forensic-keynotfound-20260820-review-repair/review-repair-output.txt` records
  the root-cause source contract and current-native Event-authority mapper tests
  passing 3/3.
- Full GREEN: the same fresh-native review-repair run records 1331/1331 tests
  passing, zero failures, product-isolation PASS, and diff-check PASS.

## Fix boundary

Production visible-evidence capture no longer requests the residual Treasure
cache. It supplies the explicit current-native contract to the pure mapper,
which resolves the exact `ExtendId -> MNetherFloorEvents -> event part -> MItems`
chain. The exact Event master row ID becomes the authoritative Treasure content
master ID. Known canonical rank-five evidence remains eligible for route
priority; unresolved Event/Part/Item or typed semantic evidence still fails
closed without fabricating IDs or invalidating known sibling nodes.

## Final deployment

`forensic-keynotfound-20260820-final-deploy/final-deploy-output.txt` records the
final fresh-native Review, focused 3/3, full 1331/1331, zero-warning Release
build, release audit, and deployment. The installed DLL SHA-256 is
`763fe37c4addd3df4ec30e8dbff5fd4dd8f10ed28bd9db8445d09c93e6b5dbf5`;
an independent read-only Docker verification matched it after the deployment
container exited.
