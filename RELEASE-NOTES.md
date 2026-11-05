# Hull Filler v0.1.1

Fixes the startup rejection after the Unity 6 game update to **Steam build 26451593** (assembly hash beginning **A2C8EA0B**). Support for build **26394979** is retained. Unknown assemblies are still rejected.

All eleven inspected game types used by the mod are unchanged between the two builds. The release passed **11,915 automated checks against each assembly** and an isolated startup check on build 26451593 confirmed both block variants and native collision registration. The updated-build check was a background startup test; broader gameplay and save/blueprint tests remain unverified.

**To update:** close the game and replace `BepInEx/plugins/HullFiller/HullFiller.dll` with the DLL from **HullFiller-v0.1.1.zip**. Keep one copy installed. Your block IDs, configuration and save format stay the same.

Requires **BepInEx 5.4.23.5 for Windows x64** and Unity **6000.6.6f1**. Keep `HideManagerGameObject = true` under `[Chainloader]`; that setting was not the cause of this hash rejection.

Find **Hull Filler** under **Hull Blocks**. Saves containing the new blocks require the mod and its stable IDs **1002/1003**. See the [README](https://github.com/MadTheDog/StarshipEVO-HullFiller#how-to-install) for installation, configuration and removal instructions.
