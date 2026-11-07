# Hull Filler and Starship Keel, 7 November 2026

Hull Filler v0.1.3 and Starship Keel **v0.1.2 experimental prerelease** completed isolated startup together on Starship EVO **Steam build ID 26451593 / 26w45a**, Unity **6000.6.6f1**, Windows x64 Mono, with BepInEx **5.4.23.5**. [Starship EVO release](https://github.com/tsunamayo/Starship-EVO/issues/8106) · [Starship Keel release](https://github.com/MadTheDog/Starship-Keel/releases/tag/v0.1.2)

Both official Keel archives matched GitHub release metadata and their published checksums. The archives supplied byte-identical SDK and runtime DLLs.

| Archive | Verified SHA-256 |
| --- | --- |
| `Starship-Keel-0.1.2.zip` | `1cea74eb7598695893f8c8bf7816044028c1492efcea31ee66ebe64a695d8c07` |
| `Starship-Keel-0.1.2-BepInEx.zip` | `8bb21db1fb2fcf5cafa3b1efbd8ca5618d8d002d5cc769f1e902d9c5e7094719` |

Hull Filler uses its existing BepInEx path with custom geometry, placement, and native collision. Keel's current functional API uses supported single-mesh stock templates. Hull Filler keeps its existing saved block IDs and uses no Keel SDK assembly reference. [Keel functional block contracts](https://github.com/MadTheDog/Starship-Keel/blob/v0.1.2/docs/KEEL-FUNCTIONAL.md)

A fresh build of Hull Filler commit `9e291634c8293decda52ef7fd7567f4256b5ef06` passed **36,120 automated checks** against the target assembly, with zero compiler warnings or errors. The checks cover both geometry families, stretching, symmetry, ID conflicts, and detection of changed native dependencies.

In the fresh profile, Hull Filler passed its dependency checks, registered active IDs `1004/1005` and legacy IDs `1002/1003`, and validated native collision for all four shapes. Keel ran in audit mode and observed resource initialization with 350 bricks and 28 shapes registered. The game remained running after resource initialization. BepInEx logged zero errors. The 33 game settings parsing fallbacks matched the prior Hull Filler startup without Keel.

The runtime check covered startup in audit mode. Keel experimental registration, interactive placement and symmetry, world loading, disk save and blueprint round trips, and multiplayer remain outside this result.
