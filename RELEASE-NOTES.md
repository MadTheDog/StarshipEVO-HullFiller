# Hull Filler v0.1.3 candidate

Replaces whole-assembly hash rejection with checks of the dependencies Hull Filler needs. Changed game build identities can pass when those contracts remain compatible. Required member/signature changes, invalid mesh-loader patch sites and incompatible collision contracts disable the feature before registration.

Target is **Steam build ID 26451593 / 26w45a**, Unity 6 Mono. The retained 26394979 assembly is also covered by automated regression checks. Assembly identity remains diagnostic data, with no build/hash allowlist.

Both assemblies pass **36,120 automated checks**. Changes to assembly MVID/version, unrelated types/overloads, sufficient collider capacity and explicitly unused optional defaults pass. Actual binding, patch, geometry, collision-slot and resource-readiness failures are rejected. Collider stretch enum values are explicitly validated.

All four block IDs, geometry families, config keys and save behavior are preserved. Candidate startup and gameplay have not been tested. See [the compatibility report](docs/COMPATIBILITY-20261106.md) for evidence and remaining acceptance.

The v0.1.3 source candidate is available on `main`. Its binary package `HullFiller-v0.1.3-candidate.zip` has not been released. Published v0.1.2 archives remain available for rollback.

## Previous published v0.1.2 notes

Makes the flat triangular face the placement base. New Hull Filler blocks attach and rotate against that face using the normal build controls, instead of anchoring on an edge that needs scaffolding underneath.

**After updating, select Hull Filler again from Hull Blocks and replace your old hotbar entry.** The catalogue uses the new face-backed pair (IDs 1004/1005). Existing placed blocks and old hotbar/eyedropper entries retain the legacy definitions (IDs 1002/1003), so saved builds do not rotate or shift. Keep the existing configuration; new flat-base ID settings are added automatically.

Supports Steam builds **26394979** and **26451593**, Unity **6000.6.6f1**, with BepInEx **5.4.23.5 for Windows x64**. Requires free collision slots **58–61**. All four block IDs must be distinct and unused by other mods.

Passed **36,094 automated checks against each supported assembly**, including complete triangular base contact through all stretch combinations, mirrored placement for both families, and unchanged legacy geometry. A separate background startup on build 26451593 registered all four shapes and validated their native collision. Interactive flat-face placement and broader save/blueprint testing remain for player confirmation.

Close the game and replace `BepInEx/plugins/HullFiller/HullFiller.dll` with the DLL in **HullFiller-v0.1.2.zip**. Keep one copy installed. [Full installation and removal instructions](https://github.com/MadTheDog/StarshipEVO-HullFiller#how-to-install).
