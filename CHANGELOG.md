# Changelog

## 0.1.1

- Add support for Steam build 26451593, fixing the startup rejection for assembly hash A2C8EA0B after the Unity 6 update.
- Keep support for Steam build 26394979 and continue rejecting unknown assemblies.
- Keep existing block IDs, geometry, configuration and save format unchanged.

## 0.1.0

- Initial corner-to-wedge hull filler and mirrored variant.
- Native hull stretching, material selection, painting and convex collision.
- Automatic reflected placement for build symmetry.
- Stable save IDs, compatibility gate and collision conflict checks.
