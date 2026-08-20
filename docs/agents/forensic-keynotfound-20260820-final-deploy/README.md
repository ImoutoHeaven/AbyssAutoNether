# Treasure Cache Final Review and Deployment

Evidence ID: `forensic-keynotfound-20260820-final-deploy`

This final gate performs a fresh current-game Cpp2IL diffable/ISIL review,
focused and full tests, diff/product/release audits, a warnings-as-errors Release
build, and an atomic deployment into the game's matched AutoNether plugin folder.
The repository and game are read-only except for the narrowly mounted evidence
directory and `BepInEx/plugins/AutoNether` deployment target.

## Outcome

- Fresh Cpp2IL acquisition/diffable/ISIL: `0/0/0`, Unity `6000.3.8f1`.
- Fresh native Review: PASS; current Nether flow Treasure-cache references: `0`.
- Focused regressions: `3/3` PASS.
- Full regression: `1331/1331` PASS.
- Product isolation: PASS.
- Release build: `0` warnings, `0` errors; release audit PASS.
- Previous installed DLL SHA-256:
  `914bf43c42c3c044ad420b285272e500ed8b7824c30b7df0a434885f8c16f51c`.
- Built and deployed DLL SHA-256:
  `763fe37c4addd3df4ec30e8dbff5fd4dd8f10ed28bd9db8445d09c93e6b5dbf5`.
- Deployed size: `1858560` bytes; exactly one `AutoNether.dll` under plugins.
- Independent post-deploy read-only `docker run --rm` hash/count check: PASS.

The deployed path is
`C:/Users/Eden/PixelAbyssX/dotabyss_x_cl/BepInEx/plugins/AutoNether/AutoNether.dll`.
