# Building Hull Filler

Requires the .NET 8 SDK, the supported Starship EVO game files and BepInEx 5.4.23.5. Game and loader assemblies are referenced from your own installation and are not included in this repository.

```powershell
./Build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Starship EVO"
```

The default game path is Steam's usual Windows installation folder. `-LoaderDir` can point to a separate BepInEx `core` folder; it must include `Mono.Cecil.dll` for the integration checks. `-DotnetPath` can select an SDK executable, and `-RestoreSource` can select an offline package source. Neither Unity Editor nor downloaded NuGet packages are required when the .NET SDK reference packs and BepInEx files are present.

The script builds the plugin, runs the console checks, then creates `dist/HullFiller-v0.1.2.zip`. The archive contains the DLL, README, license and SHA256 checksum. It does not install or launch the game.

## Compatibility

This release accepts these inspected `Assembly-CSharp.dll` SHA256 values:

```text
26394979: EC43D7D362A26B9F7795F130118B0E1EA630B5A3AE0E1732DEB5CC7FA813FE13
26451593: A2C8EA0B33FE08AA8975E7CA05066E63E2742E9DB91957CBD3F5DD6D5776C163
```

A different hash disables registration. Supporting another build requires inspecting its APIs and resource lifecycle, updating the compatibility gate, rebuilding, and testing it. Merely removing the check does not establish compatibility.

## Implementation

- `BuildCompatibility.cs`: exact allowlist for the two inspected game assemblies; unknown assemblies remain rejected.
- `HullGeometry.cs`: legacy stock wedge-minus-corner vertices, a rigid reorientation making the triangular face the Y=0 base, outward faces, and registration/readiness policy.
- `PlacementMath.cs`: reflected placement of asymmetric stretched shapes, using paired IDs and a proper block rotation.
- `ResourceMethodPolicy.cs`: distinguishes the constructed generic mesh-load call from Unity's non-generic overload.
- `MeshFactory.cs`: generates Unity meshes with flat normals, tangents, hull material indices and independent native preview copies.
- `Plugin.cs`: validates compatibility, reserves all four block IDs, registers the active/legacy pairs after stock hull resources, and adds their convex shapes to the native collider table.
- `NativePatches.cs`: scoped mesh loading, material reuse, volume factor, symmetry, and resource readiness hooks.

The plugin uses `netstandard2.1` and the game's Unity 6 assemblies. It reuses native hull materials rather than allocating entries in the stock material lookup. Reloads reuse block definitions without duplicating catalogue entries; collider registration runs again when native resources are reallocated.

The game swallows allocation callback exceptions, so the mod also gates `GameResourcesManager.OnResourceLoaded`. This prevents a failed or replaced collider shape from being treated as ready.

## Placement and saved-block compatibility

Native ActionBuild aligns local +Y with the hit surface normal, treats local Y=0 as the base plane, and clamps the vertical resize direction. The legacy filler had only an edge at Y=0. The active filler is the same solid rotated by +90 degrees about Z and translated back into native hull bounds; its marked X=-0.5 triangle becomes the entire Y=0 base. The active mirror is then reflected across X, preserving the same base plane. Native placement code requires no patches.

Changing an existing mesh in place would rotate geometry in saved worlds. Instead, the active pair uses default IDs 1004/1005 and collider slots 58/59, while IDs 1002/1003 and slots 60/61 retain their original geometry and config keys. Legacy definitions remain available to existing worlds and eyedroppers, but their master entry is removed from the Hull Blocks tab after icon registration. The tab selects the active pair. Symmetry swaps within the originating pair, so it cannot turn a legacy saved block into a differently oriented active block.

## Validation

The console runner performs 36,094 checks: exact geometry and volume, every 0–15 stretch combination, all 24 cube rotations across three reflection planes with unequal scales and several grid sizes, double reflection, ID/readiness failures, generic load overloads, and native IL/compiled compatibility checks.

v0.1.2 passed a background startup check on build 26451593 with BepInEx 5.4.23.5. The log confirmed active IDs 1004/1005 and legacy IDs 1002/1003, and native collision validation for all four shapes. The bounding planes supply axis-aligned constraints in addition to two sloped planes per shape. Automated checks run against both supported assemblies; they include full base contact at every stretch combination, unchanged legacy vertices and mirroring for both shape families. Interactive flat-face placement and broader gameplay/save/blueprint acceptance remain for player testing.
