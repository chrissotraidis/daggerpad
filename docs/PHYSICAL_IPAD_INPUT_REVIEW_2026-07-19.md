# DaggerPad physical iPad input review

Date: 2026-07-19
Device context: M2 iPad Pro with hardware keyboard and trackpad attached
Source: first physical-device playtest after the 2026-07-17 Simulator refinement

## Why this review exists

The Simulator pass proved that the controls rendered and that individual menus could be reached. It could not prove sustained movement, simultaneous touch, physical keyboard feel, trackpad feel, combat clarity, or interference from iPadOS edge gestures. This physical-iPad pass found that the previous "balanced" preset was technically reachable but not a good default.

## Reported issues

| Issue | Player impact | Source finding | Resolution in this iteration | Retest status |
| --- | --- | --- | --- | --- |
| The default touch template is too large and does not feel usable immediately. | The controls obscure the game and force the player to configure the HUD before playing. | The preset used a 340-point fixed movement stick and a 235-point attack control. | Replace the fixed stick with left/right screen-half touch surfaces and reduce the visible controls by roughly one third to one half. | Physical iPad retest required. |
| The interface feels oversized for an iPad, including the start/load presentation. | The game reads like a phone UI enlarged to tablet size. | The touch overlay sizes are DaggerPad-owned. The start/load presentation is the classic 320x200 Daggerfall screen scaled to the display and is a separate UI system. | Refine the gameplay overlay now. Track classic menu scale as a separate item so its hit regions and original art are not casually broken. | Gameplay HUD retest required; classic-menu scale remains open. |
| Keyboard movement feels faster or travels farther than expected while holding `W`, `A`, `S`, or `D`. | Mixed keyboard/trackpad play feels uncontrolled. | Keyboard movement is continuous by design. The prior HUD also placed a persistent Run toggle in the main right-hand cluster, where an accidental touch could leave running enabled. Unity's configured movement axes are joystick-only, so source review does not support a claim that `WASD` is double-injected. | Remove Run from the primary cluster and put it in More. Preserve normal `WASD` semantics until a measured walk-speed reproduction proves an engine defect. | Open: compare a timed walk with touch, keyboard, and keyboard after Run is toggled. |
| Some touch buttons work, but it is hard to tell which inputs registered. | Players repeat taps and cannot distinguish an unavailable action from a missed touch. | `TouchscreenButton` overrode Unity pointer handlers without calling the base `Button` handlers, bypassing the configured pressed-color transition. | Restore the base pressed/released visual state and add short labels to the primary controls. | Physical iPad retest required. |
| Pressing Enter appears to do nothing. | Keyboard/trackpad users cannot discover the pointer-mode modifier. | Enter is bound to `ActivateCursor`; it switches between captured look and pointer interaction but had no textual confirmation. | Show `Pointer mode` or `Look mode` in the HUD when Enter changes the mode on mobile. | Physical iPad keyboard/trackpad retest required. |
| It is difficult to understand modifier commands with the iPad keyboard attached. | The player cannot form a reliable mental model of keyboard plus trackpad controls. | The game exposes desktop bindings but gives almost no mobile-mode feedback. | Clarify Enter immediately in-game and document the default hardware bindings. A broader binding overlay remains open. | Partial; broader discoverability remains open. |
| Switching between keyboard/trackpad and touch is awkward. | Using `WASD` while touching Attack or other controls feels inconsistent. | The previous fixed stick consumed a large lower-left region while oversized actions consumed the right side. | Use the left half as a movement surface and the right half as a look surface while retaining small action buttons. Hardware input remains available concurrently. | Physical mixed-input retest required. |
| Attack appears to do nothing even after equipping items. | Combat looks broken because equipping and readying are separate Daggerfall states. | `SwingWeapon` is ignored while the weapon is sheathed, and the touch UI did not explain that state. | A sheathed Attack tap now readies the weapon and displays `Weapon readied - tap ATTACK again.` The next tap attacks in click-to-attack mode. | Physical combat retest required. |
| The bottom gear invokes or collides with iPadOS behavior. | Opening DaggerPad's control editor is unreliable and risks leaving the game context. | The editor control was placed near the home-indicator gesture area. | Move the smaller editor control to the top-left safe region, above the More drawer. | Physical edge-gesture retest required. |
| The on-screen control editor exposes too many choices before the default itself works. | Customization becomes a prerequisite instead of an optional advanced feature. | Three presets and a powerful editor exist, but the standard preset still inherited phone-oriented assumptions. | Migrate built-in presets to version 3 with the new iPad-first default. Preserve custom layouts unchanged. | Upgrade and physical retest required. |

## Version 3 default contract

- Left half: floating movement surface, analogous to `WASD`.
- Right half: floating look surface, analogous to a trackpad.
- Visible actions: Use, Attack, Ready, Bag, Pause, More, and Edit.
- Run and secondary actions live in More, reducing accidental persistent-state changes.
- Controls remain translucent and leave the middle of the game view unobstructed.
- Touch buttons visibly darken while held.
- Attack cannot fail silently solely because the weapon is sheathed.
- Enter announces whether the hardware pointer is in Pointer or Look mode.
- Edit is kept away from the iPadOS bottom-edge gesture region.

## Acceptance checklist for the next physical pass

1. Launch with the migrated `simplified-layout` and do not customize it.
2. Move continuously in all eight directions using only the left half.
3. Look through a full horizontal turn and from floor to ceiling using only the right half.
4. Hold movement and look simultaneously for at least 30 seconds without either touch dropping.
5. Tap every primary action once and confirm visible pressed feedback.
6. With the weapon sheathed, tap Attack once, confirm the readiness message and visible weapon, then tap Attack again and confirm a swing.
7. Open Bag and Pause, then return to live play without changing input mode accidentally.
8. Open More and verify Run, automap, rest, status, quick save/load, travel, journal, hand switch, and magic items remain reachable.
9. Open Edit without triggering iPadOS UI; exit with Done and with Escape.
10. With the keyboard and trackpad attached, use `WASD` plus trackpad look, then mix `WASD` with touch Attack.
11. Press Enter twice and confirm the HUD reports Pointer mode followed by Look mode, with trackpad behavior matching each state.
12. Time ten seconds of forward travel using touch, plain `W`, `Shift+W`, and `W` after deliberately toggling Run. Record distances before changing movement code.

## Still open after this source iteration

- A measured physical-device explanation for the reported keyboard movement speed.
- A compact, discoverable hardware-binding/help overlay.
- A deliberate tablet-scale treatment for Daggerfall's classic start/load and other retro UI screens.
- Full combat, inventory/equip, automap gesture, multi-touch, frame-pacing, thermal, and long-session acceptance.

## Second physical pass

The first revision was installed successfully, but the display still showed the old oversized HUD. A direct read of the iPad app container established why:

- `TouchscreenLayoutsManager_LastSelectedLayout` was `my-layout1`, not `simplified-layout`.
- `my-layout1` was an unversioned custom copy of the old phone-oriented layout.
- It enabled 32 controls at once, including a 275-point Attack button, 270-point fixed joystick, 170-point Enter button, and 142-point drawer.
- It disabled both screen-half joystick modes, leaving the fixed bottom-left joystick as the only movement surface.
- The version 3 simplified, gesture, and accessibility files were present and correctly regenerated, but the app intentionally preserved the previously selected custom layout.

This means the first revision reached the iPad binary and data container, but not the active HUD. The preservation rule was correct in isolation and wrong as an upgrade experience for this legacy test layout.

### Additional reported behavior

| Finding | Current interpretation | Version 4 response |
| --- | --- | --- |
| The visible HUD is still about 40% too large. | This was the 32-control legacy custom HUD, not the installed compact preset. | Detect this specific legacy-layout shape, preserve its file, and switch the active selection to the simplified preset once. Reduce the version 4 primary controls further, to roughly 40-60% of the legacy sizes. |
| The bottom-left thumbstick does not move the player. | Physical failure confirmed; the active layout had only the old fixed joystick available for movement. | Remove pointer-visibility blocking from touch joysticks, clear stale touch axes during layout changes, and process touch movement independently of controller enablement. |
| Touching elsewhere does not reliably look around. | The active custom layout had no movement half and depended on two mouse-look surfaces; pointer visibility could reject their touch-down events, and drag data was not explicitly refreshed. | Activate the explicit left-move/right-look split, allow touch while the hardware pointer is visible, and refresh the look surface's pointer event on every drag. |
| Inventory, save, Enter mode switching, Spellbook, weapon ready/sheathe, crouch/stand, map, and More appear to work. | These are useful physical passes, although several were exposed through an overly dense layout. | Keep the proven core actions reachable; move secondary actions into a compact two-column More drawer. |
| Show/Hide is unclear or nonfunctional; Run is uncertain. | The legacy HUD exposed many ambiguous desktop actions without a coherent mobile hierarchy. | Do not expose ambiguous secondary actions in the primary HUD. Keep Run in More and require explicit retest. |
| More works reasonably well, but its icons are bland and inconsistent with Daggerfall. | The project inherited flat white Android/Linux glyphs. | Add a unified dark-iron, tarnished-brass, and oxblood button frame with high-contrast ivory glyphs for every primary and More action. |

### Version 4 layout target

- Collapsed state: only Edit and More at top-left; Pause and Bag at top-right; Use, Attack, and Ready along the far-right edge.
- Movement and look: left and right screen halves, with no permanent fixed joystick.
- Expanded More state: a two-column, six-row utility grid on the left edge.
- Visual language: compact octagonal dark-iron controls, tarnished brass corners, restrained red gems, and readable ivory action silhouettes.
- Upgrade behavior: legacy phone-style custom layouts remain on disk but no longer override the improved iPad default automatically.

## Third physical pass

The player supplied two 2048 x 1366 screenshots from the version 4 build and confirmed that right-thumb look worked but left-thumb movement did not. The visible movement pad appeared without continuously moving the player. Physical WASD moved too strongly, simultaneous movement and look remained unreliable, the compact buttons were now slightly too small, and both the normal HUD and open More drawer overlapped Daggerfall's native interface.

The complete screenshot-led audit is in [`audits/2026-07-19-touch-pass-3/README.md`](audits/2026-07-19-touch-pass-3/README.md).

Version 5 changes movement from UI-callback-driven updates to direct per-finger sampling, arbitrates touch/keyboard/controller movement instead of adding sources, preserves analog direction, and scales iPad keyboard movement to 55 percent. The HUD is reorganized into a middle-right action stack, bottom-center command strip, and top-center utility tray. Buttons now provide a strong pressed state and held labels; Enter is restored at 112 x 64; and Back plus Enter remain available while a Daggerfall window is paused.

## Fourth physical pass

Movement finally worked on the iPad and its sensitivity felt right, but the visible floating pad remained much too large. The version 5 action cluster, Inventory, and Pause did not respond; only the More drawer worked. The open top-center tray covered Daggerfall's save feedback, its per-button held labels collided under the player's thumb, and it stayed open over the control editor. Enter's text also overfilled its frame. The accepted screenshots and scoped audit are in [`audits/2026-07-19-touch-pass-4/README.md`](audits/2026-07-19-touch-pass-4/README.md).

Source review found a concrete regression behind the dead buttons: version 5 removed the normal-action branch that converts actions such as Use, Attack, Inventory, and Escape into their configured key bindings. Drawer toggling used a separate path, which is why More alone still worked.

Version 6:

- Restores the standard action-to-key path while leaving custom actions on their existing direct queue.
- Reduces the visible movement ring and knob from 400/150 to 240/90 while preserving the previously accepted 125-point logical drag radius.
- Adds a deliberate double-tap on the right look surface for Use/Take; a single look tap remains inert.
- Moves Use, Attack, and Draw lower on the right edge, closer to the right thumb's look area.
- Moves the More tray to a three-column grid below the native status bars and moves Edit into the bottom command strip.
- Closes every open drawer before the control editor appears.
- Replaces labels under held fingers with one HUD message and caps Enter text at 22 points.

### Version 6 physical acceptance

1. Confirm the visible movement artwork is roughly 40 percent smaller while forward/back/strafe sensitivity is unchanged.
2. Move and look simultaneously, then repeat with `WASD` plus right-thumb look.
3. Tap Use, Attack, Draw, Inventory, and Pause once each and confirm both press feedback and the expected game action.
4. Double-tap an object with the right thumb and confirm it is used or taken; confirm a single tap only looks.
5. Open More and confirm it begins below the health/status region and does not cover save feedback.
6. Hold two different buttons and confirm each name appears in the HUD instead of under the thumb.
7. Enter control editing with More open and confirm the drawer closes before the editor renders.
8. Confirm Enter remains easy to hit and its text fits comfortably inside its frame.

### Version 6 deployment evidence

- Unity `2022.3.62f3` completed the iOS device export without compiler errors.
- Xcode completed the physical-device build and automatic development signing for the connected M2 iPad Pro.
- The update installed over `com.chrissotraidis.daggerpad` and launched successfully without deleting the existing app container.
- A device-container read-back confirmed `simplified-layout`, preset version 6, left movement enabled, right look enabled through the right screen-half mode, the lower action positions, standalone bottom Edit, and a closed drawer with Edit removed from its contents.

This proves deployment and configuration state, not hands-on control feel. The eight checks above remain the physical acceptance gate.
