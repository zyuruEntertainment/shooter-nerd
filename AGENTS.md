# shooter nerd — agent notes

Unity project. There is no `dotnet`, `npm`, `cargo`, or Makefile. Open/build/test in the **Unity Editor** (URP + Input System packages). The `.csproj` / `.slnx` at root are IDE intellisense only and are gitignored; they are not build entrypoints.

## Source layout
All gameplay code lives under `Assets/Scripts/`, compiled into `Assembly-CSharp`:
- `Configuration/` — `SensitivitySettings` (scene singleton: separate `LookSensitivity` and `FireSensitivity`)
- `Input/` — `PCInput` (Unity Input System), `MobileInput` / `FireJoystick` / `DynamicJoystick` (legacy touch)
- `Movement/` — `PlayerMovement` (consumes look + move input, rotates camera)
- `Combat/` — `ArcadeWeaponController` (player fire rate gated by `fireInterval / FireSensitivity`), `BotRangedCombat` (bot ranged shooting, independent of state machine), `CombatHealth` (damage + `OnTakeDamage` event)
- `Physics/` — `PlayerPhysics`
- `Bot/` — bot AI/state machines: `BotController`, `BotStateMachine`, `BotState`, `BotSettings`, `BotMovement`, `BotCombat`, `BotVision`, `BotPerception`, `BotHearing`, `BotDashController`, `BotJumpController`

Scenes: `Assets/Scenes/Gameplay.unity` (main), `BuyMenu.unity`.

## Bot AI behavior (recently changed)
- **Strafe state**: bot stops chasing at `EngageRange` (default 10f) and moves left/right perpendicular to the player while shooting. Direction changes every `StrafeChangeInterval`. Ranged shooting is handled by `BotRangedCombat` (runs independently each frame when `CanSeePlayer` within its `attackRange`).
- **Flee state**: when bot takes damage (`CombatHealth.OnTakeDamage`), it enters Flee for `FleeDuration` (default 2s), runs directly away from the player at `FleeSpeed`, and dashes away immediately (`forceDash`). After timer expires, normal state logic resumes.
- **State priority**: Flee > visible (Attack / Strafe / Chase) > LastKnown (Search) > Heard (Investigate) > Patrol / Idle.
- Bot **must have a `CombatHealth` component** to detect damage and flee. Without it, `OnTakeDamage` never fires and the bot won't react to being shot.

## Key conventions (easy to miss)
- `SensitivitySettings` is a **scene singleton** (`Awake` sets `Instance`). A GameObject with this component must exist in the active scene, or look/fire sensitivity fall back to defaults (`1f`).
- Look sensitivity is applied **with `Time.deltaTime`** in `PlayerMovement.RotatePlayer`; fire rate is `fireInterval / FireSensitivity` in `ArcadeWeaponController.Fire`. Mobile `lookMultiplier` (`MobileInput`) is a separate pre-multiplier, not part of `SensitivitySettings`.
- PC input uses the **Unity Input System** package (`Mouse.current`/`Keyboard.current`); mobile uses legacy `IPointerDown/IDrag/IPointerUp` touch handlers. Both feed `PlayerMovement`.
- `BotDashController.TryDash` supports `forceDash` parameter for immediate dash while fleeing (bypasses close-range timer).

## Repo hygiene
Gitignored (do not commit / do not edit expecting them to persist): `Library/`, `Temp/`, `obj/`, `Build/`, `*.log`, `.vs/`, `*.csproj`, `*.sln`, `*.userprefs`. `build.log`/`build2.log` are untracked build output.

## Verification
- No test files exist and no CI workflows are present (`com.unity.test-frameware` is installed but unused).
- Only editor script: `Assets/Editor/SnapToFloor.cs` (runs in Editor only).
