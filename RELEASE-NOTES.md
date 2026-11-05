# Hull Filler v0.1.0

Adds the missing corner-to-wedge hull filler, with native three-axis stretching, hull materials and painting, convex collision, and an automatically selected mirrored variant for build symmetry.

**Supported and tested on Starship EVO Early Access 26w44a**, Steam build **26394979**, Unity **6000.6.6f1**, with **BepInEx 5.4.23.5 for Windows x64**. The plugin checks the game assembly and disables itself on other builds.

Initial single-player testing was positive. Startup and native block/collision registration passed, along with **11,910 automated checks**. Broader collision testing, save and blueprint round trips, multiplayer and mod interoperability remain unverified.

Back up your worlds. Extract the release ZIP into the game folder so the DLL lands at `BepInEx/plugins/HullFiller/HullFiller.dll`. Set `HideManagerGameObject = true` under `[Chainloader]` in `BepInEx/config/BepInEx.cfg`.

Find **Hull Filler** under **Hull Blocks**. Saves containing the new blocks require the mod and its stable IDs **1002/1003**. See the [README](https://github.com/MadTheDog/StarshipEVO-HullFiller#how-to-install) for installation, configuration and removal instructions.
