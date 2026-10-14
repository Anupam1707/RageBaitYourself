# RAGEBAIT YOURSELF

Unity 2022.3.62f3 project notes

## Architecture

LV1 keeps its original authored maze and gameplay flow, and now uses the same
serialized `RageHUD` and timed message panel as LV2-LV6.

LV2-LV6 are authored scene hierarchies. Their floors, walls, lights, player,
desktop camera, UI, hazards, checkpoints, interactables, and target rounds are
saved directly in the corresponding `.unity` files. `RageLevelRuntime` and the
other components in `Assets/Scripts` provide gameplay state and behavior only;
they do not create level objects, primitives, cameras, or UI at runtime.

The scene roots are organized as:

- `World`: geometry, labels, lights, platforms, props, hazards, relics, doors,
  and targets.
- `Gameplay`: player and spawn marker.
- `Lighting`: serialized directional and point lights.
- `UI`: serialized Canvas and HUD text/panel hierarchy.
- `Systems`: serialized desktop interaction components and the level runtime.

Home and Levels each contain a scene-authored `PersistentAudio` object. Its
`BGMCon` component is the only persistent menu object, so the same music
continues across either entry scene without keeping stale menu UI references.

Level content:

- LV2: forward obstacle course with a horizontal mover, rotating hazards, a
  sweeper, moving/falling platforms, checkpoints, and a troll finish line.
- LV3: jump-focused platformer with normal, moving, disappearing, deceptive,
  precision, shortcut, and final-sequence platforms.
- LV4: desktop 3D relic hunt with five real relics, a vanishing approach
  relic, a relocated relic, a fake collectible, a clue, and a locked exit.
- LV5: escape room with three components, drawer, fake item, button, lever,
  sealed key, cardboard exit, and real exit.
- LV6: five-round target arena with static, moving, evasive, fake, blinking,
  and one-valid-target finale rounds. All 16 targets are serialized in the
  scene; the runtime only activates the current round.

## Laptop controls

- WASD or arrow keys: move
- Mouse: look
- Space: jump in LV2/LV3
- E or left mouse: interact in LV4/LV5 while looking at an object
- Left mouse: fire in LV6
- Escape: release the mouse cursor

Each new level starts with three checkpoint revives. Falling below the level or
touching a deterministic hazard consumes one revive and returns the player to
the latest checkpoint. Reaching zero revives opens GameOver. Completing a
level opens Victory.

## Unity Editor setup

1. Open the project with Unity 2022.3.62f3 and let Package Manager finish
   resolving.
2. Confirm all ten scenes in `ProjectSettings/EditorBuildSettings.asset` are
   enabled. The existing order is Home, Victory, GameOver, Levels, LV1, LV2,
   LV3, LV4, LV5, LV6.
3. Open Home and use the existing level-selection buttons.
4. Open LV2-LV6 directly to inspect or edit their complete hierarchies. Changes
   to level layout should be made in the scene; scripts should remain behavior
   components.
5. Do not add `RageLevelRuntime` to LV1. Its `PlayerController` still owns the
   original coin, treasure, revive, and maze rules, while its serialized
   `RageHUD` displays the status and feedback messages.

## Testing checklist

- LV1: verify the shared HUD shows the welcome, coin, treasure, checkpoint,
  locked-exit, victory, and game-over messages while the original maze, coins,
  treasure, obstacles, revive, and completion rules still behave as before.
- LV2: run through the bars and sweepers, ride the moving platform, cross the
  falling platforms, then trigger the fake finish once before crossing again.
- LV3: use Space to jump, intentionally test the green SAFE platform, then use
  the red-looking safe platform and the difficult-looking shortcut.
- LV4: look at relics and press E/LMB; confirm the HUD reads Relics Found:
  0/5 and updates immediately, then walk to the exit.
- LV5: take Key 1, touch Key 2 once, open the drawer, ignore the fake red item,
  use the blue lever, take Key 3, trigger the cardboard exit, then use the
  revealed real exit.
- LV6: clear rounds 1-4 with the crosshair and finish round 5 by finding the
  only valid target.

The project is configured as a desktop-only Unity game. No XR package, loader,
simulator, or scene rig is required to run it.
