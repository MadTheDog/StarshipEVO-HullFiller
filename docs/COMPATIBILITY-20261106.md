# Hull Filler compatibility maintenance, 6 November 2026

## Target and evidence

- Target is **Steam build ID 26451593 / 26w45a**, on the `unity6` branch, Unity 6000.6.6f1.
- The installed Steam appmanifest has buildid and TargetBuildID 26451593, StateFlags 4, UpdateResult 0 and depot 711981 manifest 2594540325782390880. Its installed assembly matches this project's retained build26451593 test assembly, SHA256 A2C8EA0B33FE08AA8975E7CA05066E63E2742E9DB91957CBD3F5DD6D5776C163. Installed files were distinguished from staged downloads.
- Current-day Steam appinfo evidence supplied by the coordinating investigation lists unity6 at 26451593 and public/experimental at 26122752. The developer's release issue was fetched directly and identifies 26w45a, posted 5 November. [Developer release](https://github.com/tsunamayo/Starship-EVO/issues/8106)
- The older retained Steam build 26394979 assembly is a regression fixture, SHA256 EC43D7D362A26B9F7795F130118B0E1EA630B5A3AE0E1732DEB5CC7FA813FE13. Its precise year-week release identifier was not independently verified during this maintenance.

## Changes

- The source candidate is v0.1.3, based on published v0.1.2. Build with `Build.ps1`, supplying `-GameDir`, `-LoaderDir` and `-DotnetPath` for your installation. An optional `-RestoreSource` selects an offline package feed.
- Removed the whole-assembly SHA256 allowlist. Assembly SHA256 and MVID are logged for diagnostics and cannot reject an otherwise compatible feature. Diagnostic hashing failures only produce a warning.
- Added read-only Cecil preflight before Harmony patching or block registration. It validates compiled game member bindings/accessibility, static versus instance fields, exact Harmony signatures, the three typed mesh-loader patch site patterns, the injected collider path field, the collision geometry field layout, reserved enum slots and sufficient geometry table capacity.
- Harmony targets now specify overload signatures. Unrelated added overloads are accepted. The preflight also requires the resource-ready call to remain outside swallowed allocation exceptions.
- Collider creation supplies all five arguments explicitly. The inlined `uniformScale` enum value is checked. Changes to optional defaults cannot silently change the candidate's supplied arguments. `ResizeMode.BlockFull` is a static field, rather than an enum, and is already covered by member binding checks.
- Existing runtime ID occupancy, four-slot collider occupancy, generated shape vertex/plane counts, collider ownership on reload and resource-ready guards remain in place.
- IDs 1002/1003 retain legacy geometry and config keys. IDs 1004/1005 retain flat-base geometry. Mesh paths, geometry, save identities and collider slots 58-61 are preserved.

## Failure behavior

The two active and two legacy filler definitions form one collision-dependent feature. Missing required contracts disable this feature before registration and unpatch its hooks. Stock behavior is preserved at that stage. Once filler definitions exist, collision initialization/ownership failure continues to block resource-ready callbacks. Proceeding into a world with incorrectly colliding saved filler blocks is unsafe, so this necessary existing safety boundary remains.

No raw GPU buffer or shader patches are introduced. Runtime stock material availability, custom generated mesh geometry and native collider registration still require startup/gameplay verification. Metadata admission does not prove unchanged implementation semantics in an untested future release.

## Verification

- The original unknown-hash rejection was reproduced as one failing test before removing the identity gate.
- Final build targets the installed 26451593 assembly. Plugin and test builds have zero warnings and zero errors.
- The full geometry, stretch, symmetry, ID conflict, readiness, native integration and serialized assembly mutation suite passes **36,120 checks with zero failures** against each retained assembly. Final hashes and command output are retained in local evidence.
- Accepted mutations include changed MVID/version/assembly bytes, an unrelated new type, unrelated overloads, a larger collision table and changed optional defaults with explicit caller arguments.
- Rejected mutations include changed patch return signature, missing injected field, changed static field binding, missing BlockFull field, changed inlined stretch enum, removed mesh-loader site, occupied collider slot, native geometry layout change, insufficient collider capacity and a swallowed resource-ready failure.
- Assembly mutation fixtures are metadata-only regression inputs. They are never installed or executed as game assemblies.
- Candidate startup, interactive placement, legacy save/blueprint round trips, collision behavior, multiplayer and other-mod interoperability were **not run** in this maintenance. Existing v0.1.2 startup evidence remains historical evidence for that release.

## Artifacts and rollback

- The build writes `dist/HullFiller-v0.1.3-candidate.zip`. This binary candidate has not been released. Source maintenance is tracked on `main`.
- Published v0.1.0/v0.1.1/v0.1.2 archives and both isolated test installations retain their existing plugin files. Candidate binaries were not installed into any game copy.
- Raw manifests, logs, local test profiles and game binaries remain outside this repository. Verification records cover both assembly hashes listed above.
- No game process was launched or stopped during this maintenance. Runtime acceptance remains pending.

## Remaining acceptance

Verify candidate startup in a disposable 26451593 copy/profile, active and legacy shape placement/stretch/symmetry, collision and save/blueprint round trips. A binary release requires separate runtime acceptance and authorization.
