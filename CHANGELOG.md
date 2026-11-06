# Changelog

## 0.1.3 candidate

- Replace the whole-assembly hash allowlist with dependency checks before patching or registering blocks.
- Validate referenced game members, Harmony patch signatures, typed mesh loader sites, native collider layout/capacity and the resource-ready exception boundary.
- Keep assembly identity in diagnostics and preserve runtime ID-conflict, collider ownership and readiness checks.
- Preserve all four save IDs, legacy geometry, config keys and collision slots.
- Add serialized assembly mutation regressions for compatible identity changes and actual dependency breakage.
- Keep candidate output separate from published v0.1.2 rollback packages.

## 0.1.2

- Make the flat triangular face the native placement base for newly selected filler blocks.
- Retain the original definitions for saved blocks so existing builds do not rotate on update.
- Add active flat-base IDs 1004/1005, retaining legacy IDs/config keys 1002/1003.
- Extend collision, ID conflict and symmetry handling to both shape families.
- After updating, select Hull Filler again from Hull Blocks to replace a legacy hotbar entry.

## 0.1.1

- Add support for Steam build 26451593, fixing the startup rejection for assembly hash A2C8EA0B after the Unity 6 update.
- Keep support for Steam build 26394979 and continue rejecting unknown assemblies.
- Keep existing block IDs, geometry, configuration and save format unchanged.

## 0.1.0

- Initial corner-to-wedge hull filler and mirrored variant.
- Native hull stretching, material selection, painting and convex collision.
- Automatic reflected placement for build symmetry.
- Stable save IDs, compatibility gate and collision conflict checks.
