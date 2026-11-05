# Hull Filler v0.1.1

---

## Short introduction

Adds a stretchable hull shape that fills the gap between a standard corner and wedge in Starship EVO. Includes a mirrored variant for the opposite side of a build.

---

## What it does

Find **Hull Filler** under **Hull Blocks**. Use the normal hull resize controls and the shape selector to switch between the original and mirrored variants.

| Feature | What changes |
|---|---|
| Corner-to-wedge filler | Completes a standard corner into a wedge without the triangular gap. |
| Three-axis stretching | Uses the game's normal hull stretching and grid sizes. |
| Mirrored variant | Provides the opposite-handed shape. Active build symmetry selects it automatically. |
| Hull materials and painting | Uses the original hull surfaces, material selection and paint controls. |
| Shape collision | Registers native convex collision for the filler geometry, including stretched sizes. |

### What was tested

- v0.1.0 passed startup and block/collision registration on build 26394979, followed by a positive initial player check. v0.1.1 passed a separate background startup and registration check on build 26451593.
- **11,915 automated checks on both supported game assemblies**, covering geometry, all 4,096 stretch combinations, reflection across all 24 block rotations and three symmetry planes, ID conflicts and native integration.
- Testing remains limited. Broader collision setups, save/blueprint round trips, multiplayer and compatibility with other mods still need verification.

---

## Installation requirements

- **Starship EVO Unity 6 Mono branch**, Steam builds **26394979** and **26451593**, Unity **6000.6.6f1**. Other game builds are not supported by this release; the plugin checks the game assembly at startup.
- **64-bit Windows** and **BepInEx 5 for Windows x64**. Tested with [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5).

---

## How to install

1. Close Starship EVO and back up any world you plan to use. Try a fresh Creative world first.
2. In Steam, open **Starship EVO → Manage → Browse local files**. This is the folder containing `Starship EVO.exe`.
3. Extract BepInEx 5 for Windows x64 beside the executable. If BepInEx 5 already works, keep your installation. Launch the game once, then close it to generate the configuration.
4. Open `BepInEx/config/BepInEx.cfg` and set the existing `HideManagerGameObject` key under `[Chainloader]` to `true`.

   ```ini
   [Chainloader]
   HideManagerGameObject = true
   ```

5. Download **HullFiller-v0.1.1.zip** from the release's **Assets**. The auto-generated source ZIP does not contain the plugin. [Releases](https://github.com/MadTheDog/StarshipEVO-HullFiller/releases)
6. Extract the ZIP's `BepInEx` folder into the game folder. The file should land here:

   ```text
   Starship EVO/
   ├── Starship EVO.exe
   └── BepInEx/
       └── plugins/
           └── HullFiller/
               └── HullFiller.dll
   ```

7. Launch the game. Look for **Hull Filler** under **Hull Blocks**. If it is missing, check the DLL path, the loader option above and `BepInEx/LogOutput.log` for the registration messages.

To update, close the game and replace `BepInEx/plugins/HullFiller/HullFiller.dll`. Keep a single copy installed.

## How to uninstall

1. Back up your worlds and remove the Hull Filler blocks from any saves or blueprints you want to keep using without the mod. Save while the mod is still installed, or restore a pre-mod backup.
2. Close the game and delete `BepInEx/plugins/HullFiller/`.
3. Optionally delete `BepInEx/config/local.sevo.hullfiller.cfg`.

---

## Notes

- Saves and blueprints containing these blocks depend on the mod. Default brick IDs are **1002** and **1003**. Keep them stable across saves and matching between installations. Occupied IDs disable registration without replacing existing definitions.
- Collision slots **60** and **61** are reserved. A collision initialization conflict prevents resource loading rather than using the wrong collision shape.
- The filler is one sixth of the matching full hull box by volume. It uses native volume scaling for weight and hull properties.
- The mesh is generated inside the plugin. No Unity Editor, asset bundle or replacement game assets are needed to install it.
- Single-player testing only. Hot unloading is unsupported.
- Unofficial mod, unaffiliated with Moonfire Entertainment.
- [Building from source and compatibility details](docs/DEVELOPMENT.md).

---

## License

Copyright © 2026 MadTheDog. Released under the [MIT License](LICENSE).
