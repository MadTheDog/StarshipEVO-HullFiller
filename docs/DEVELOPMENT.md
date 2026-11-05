# Building Hull Filler

Requires the .NET 8 SDK, the supported Starship EVO game files and BepInEx 5.4.23.5. Game and loader assemblies are referenced from your own installation and are not included in this repository.

```powershell
./Build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Starship EVO"
```

The default game path is Steam's usual Windows installation folder. `-LoaderDir` can point to a separate BepInEx `core` folder; it must include `Mono.Cecil.dll` for the integration checks. `-DotnetPath` can select an SDK executable, and `-RestoreSource` can select an offline package source. Neither Unity Editor nor downloaded NuGet packages are required when the .NET SDK reference packs and BepInEx files are present.

The script builds the plugin, runs the console checks, then creates `dist/HullFiller-v0.1.1.zip`. The archive contains the DLL, README, license and SHA256 checksum. It does not install or launch the game.

## Compatibility

This release accepts these inspected `Assembly-CSharp.dll` SHA256 values:

```text
26394979: EC43D7D362A26B9F7795F130118B0E1EA630B5A3AE0E1732DEB5CC7FA813FE13
26451593: A2C8EA0B33FE08AA8975E7CA05066E63E2742E9DB91957CBD3F5DD6D5776C163
```

A different hash disables registration. Supporting another build requires inspecting its APIs and resource lifecycle, updating the compatibility gate, rebuilding, and testing it. Merely removing the check does not establish compatibility.

## Implementation

- `BuildCompatibility.cs`: exact allowlist for the two inspected game assemblies; unknown assemblies remain rejected.
- `HullGeometry.cs`: the four vertices of the stock wedge-minus-corner solid, outward faces, and registration/readiness policy.
- `PlacementMath.cs`: reflected placement of asymmetric stretched shapes, using paired IDs and a proper block rotation.
- `ResourceMethodPolicy.cs`: distinguishes the constructed generic mesh-load call from Unity's non-generic overload.
- `MeshFactory.cs`: generates Unity meshes with flat normals, tangents, hull material indices and independent native preview copies.
- `Plugin.cs`: validates compatibility, registers both blocks after stock hull resources, and adds their convex shapes to the native collider table.
- `NativePatches.cs`: scoped mesh loading, material reuse, volume factor, symmetry, and resource readiness hooks.

The plugin uses `netstandard2.1` and the game's Unity 6 assemblies. It reuses native hull materials rather than allocating entries in the stock material lookup. Reloads reuse block definitions without duplicating catalogue entries; collider registration runs again when native resources are reallocated.

The game swallows allocation callback exceptions, so the mod also gates `GameResourcesManager.OnResourceLoaded`. This prevents a failed or replaced collider shape from being treated as ready.

## Validation

The console runner performs 11,915 checks: exact geometry and volume, every 0–15 stretch combination, all 24 cube rotations across three reflection planes with unequal scales and several grid sizes, double reflection, ID/readiness failures, generic load overloads, and native IL/compiled compatibility checks.

Runtime startup was checked on both supported builds in isolated game copies with BepInEx 5.4.23.5; the updated build used a background startup check. The eleven inspected game types used by the mod decompile identically between these builds. The log confirmed IDs 1002/1003, four native collision vertices and two sloped planes per variant, with the native bounding planes supplying axis-aligned constraints. The initial player check was positive. This does not establish broad collision correctness, save/blueprint round trips, multiplayer support or performance gains.
