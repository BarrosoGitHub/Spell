---
name: feel-screen-shake
description: "Use this skill whenever the user wants the camera, screen or UI to shake, kick, zoom, fade or flash using Feel — e.g. 'screen shake when the player gets hit', 'camera shake on explosion', 'shake my cinemachine camera', 'make the whole UI shake', 'zoom punch on landing', 'fade to black', 'white flash on damage', 'my camera shake isn't working', 'option to turn off screen shake'. Covers Cinemachine Impulse, the MMCameraShaker rig, MMCinemachineCameraShaker, UI shake, Camera Zoom, Fade and Flash feedbacks, Automatic Shaker Setup, channels, shaking from code and accessibility toggles. Do NOT use for post-processing pulses like chromatic aberration (see feel-post-processing-effects), shaking a regular object (see feel-add-feedbacks) or spring-based FOV bumps (see feel-springs). When in doubt whether a camera-shake request could involve Feel, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Feel"
  publisher: "More Mountains"
  asset-version: "6.4"
  skill-version: "1.0.0"
  unity: "6000.5+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/particles-effects"
  asset-store-url: "https://assetstore.unity.com/packages/tools/particles-effects/feel-183370"
  documentation-url: "https://feel-docs.moremountains.com/"
  support-url: "https://feel.moremountains.com/"
  last-verified: "2026-11-20"
---

# Add Screen and Camera Shake with Feel

Feel shakes the screen through feedbacks on an `MMF_Player`. Most camera feedbacks don't move the camera themselves: they broadcast an event, and a **shaker** component on the camera (or a Cinemachine listener) receives it and does the work. This skill lets an agent pick the right shake route for the project's camera setup, add both halves (feedback and receiver), trigger shakes from code, and give players an option to turn shakes off.

## When to use this skill

- "screen shake when I take damage" / "camera shake on explosion"
- "shake my cinemachine camera" / "add an impulse when the boss lands"
- "my camera isn't cinemachine, can I still shake it?"
- "shake just the UI / the HUD"
- "zoom in quickly when I hit something" / "FOV punch"
- "fade the screen to black then back" / "white flash when hit"
- "camera shake isn't working" / "feedback plays but the camera doesn't move"
- "add a setting to disable screen shake for accessibility"

Not for:
- Pulsing chromatic aberration, lens distortion, bloom, vignette → `feel-post-processing-effects`
- Shaking or wiggling an object (not the camera) → `feel-add-feedbacks` (`Transform > Position Shake`, `Transform > Wiggle`)
- Spring-driven FOV or position bumps → `feel-springs`
- Building the rest of a feedback sequence → `feel-add-feedbacks`

## Prerequisites

- Feel 6.1, Unity 6000.5+, any render pipeline.
- **Install check:** `System.Type.GetType("MoreMountains.Feedbacks.MMF_Player, MoreMountains.Tools")` is not null. If it is null, tell the user Feel is not installed (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) and stop.
- **Cinemachine routes** need `com.unity.cinemachine` (3.x recommended; 2.x also supported). Check that `System.Type.GetType("MoreMountains.FeedbacksForThirdParty.MMF_CinemachineImpulse, MoreMountains.Tools")` resolves; if not, Cinemachine is missing. Installing a package changes the project, so ask the user first (or use `Tools > More Mountains > MMFeedbacks > Install All Dependencies`).
- **Fade and Flash** need uGUI (`com.unity.ugui`), which is present by default.
- Automatic setup uses `Camera.main`, so the scene camera must be tagged `MainCamera`.

## Quick start

Cinemachine impulse shake, the recommended route when Cinemachine is installed:

1. Make sure the scene has a camera tagged `MainCamera`.
2. Create an empty GameObject `ShakeFeedback`, add **MMF Player**, then **Add new feedback > Camera > Cinemachine Impulse**. When added, the feedback already has a raw signal (`MM_6D_Shake` noise profile) and `Velocity = (5, 5, 5)`.
3. Expand the feedback's **Automatic Setup** group and press **Automatic Shaker Setup** (outside Play mode). It adds a `CinemachineBrain` to the main camera if needed, creates a `CinemachineCamera` if none exists, and adds a `CinemachineImpulseListener` to it. The same thing from an editor script:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelShakeQuickStart
{
    public static void CreateImpulseShake()
    {
        GameObject go = new GameObject("ShakeFeedback");
        Undo.RegisterCreatedObjectUndo(go, "Create ShakeFeedback");
        MMF_Player player = go.AddComponent<MMF_Player>();
        Undo.RecordObject(player, "Add Cinemachine Impulse");
        player.AddFeedback(typeof(MMF_CinemachineImpulse));
        player.AutomaticShakerSetup(); // runs every feedback's automatic setup
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
```

4. Call `ShakeFeedback.PlayFeedbacks()` from gameplay, or press the green **Play** button on the player in Play mode.
5. Expected result: the scene contains a `CinemachineCamera` with a `CinemachineImpulseListener`, the main camera has a `CinemachineBrain`, and playing the feedback shakes the view. The Console logs a "... automatic shaker setup : ... You're all set." line.

## Workflows

### Workflow: Choose the right route

**Goal:** pick the setup that matches the project.

**Steps:**
1. Find the camera setup: look for `Unity.Cinemachine.CinemachineBrain` on `Camera.main`, and for `CinemachineCamera` objects in the scene.
2. Decide:
   - Cinemachine present → **Cinemachine Impulse** (Quick start) for physical-feeling shakes, or **Camera Shake on a Cinemachine camera** if the user wants Feel's simple duration/amplitude/frequency controls.
   - No Cinemachine → **Camera Shake with an MMCameraShaker rig**.
   - Only the HUD should shake → **UI shake**.
3. Tell the user which route you picked and why.

**Expected result:** one route chosen before you change anything.

### Workflow: Camera Shake with an MMCameraShaker rig (no Cinemachine)

**Goal:** shake a regular `Camera`.

**Steps:**
1. Add an `MMF_Player` and the feedback **Camera > Camera Shake** (`MMF_CameraShake`). Defaults: `CameraShakeProperties = (Duration 0.1, Amplitude 0.2, Frequency 40)`.
2. Automatic: press **Automatic Shaker Setup** on the feedback (or call `player.AutomaticShakerSetup()`). With no Cinemachine camera in the scene, it creates `CameraRig > CameraShaker` at the main camera's position, adds `MMCameraShaker` (which brings a required `MMWiggle`) to `CameraShaker`, sets the wiggle to position mode with `WigglePermitted = false` and `WiggleType = Noise`, and re-parents the main camera under it.
3. Manual equivalent: create `CameraRig` (empty) > `CameraShaker` (empty, child) > your camera (child). On `CameraShaker`, add `More Mountains > Feedbacks > Shakers > Camera > MM Camera Shaker`; on its `MMWiggle`, tick **Position**, set **Wiggle Type** to **Noise** and untick **Wiggle Permitted**.
4. Why the rig: the shaker offsets its own transform, so the camera keeps any position your camera controller writes to `CameraRig`. If your controller moves the camera object itself, move that logic to `CameraRig`.
5. Increase `Amplitude` and `Duration` for stronger hits (for example `Duration 0.3, Amplitude 2, Frequency 40`).

**Expected result:** hierarchy `CameraRig/CameraShaker/<Main Camera>`, `CameraShaker` has `MMCameraShaker` and `MMWiggle`; playing the feedback jitters the view for the set duration.

### Workflow: Camera Shake on a Cinemachine camera

**Goal:** use `MMF_CameraShake` (duration/amplitude/frequency) with a Cinemachine camera.

**Steps:**
1. On the `CinemachineCamera`, add a **CinemachineBasicMultiChannelPerlin** noise component, set its **Noise Profile** to a 6D shake profile (Feel ships `Assets/Feel/MMFeedbacks/MMFeedbacksForThirdParty/Cinemachine/Resources/MM_6D_Shake.asset`), and set **Amplitude Gain** and **Frequency Gain** to 0 so the camera is still at rest.
2. Add `More Mountains > Feedbacks > Shakers > Cinemachine > MM Cinemachine Camera Shaker` (`MMCinemachineCameraShaker`) to the same object.
3. Add **Camera > Camera Shake** to an MMF Player; a good starting point is `Duration 0.3, Amplitude 2, Frequency 40`.

**Expected result:** playing the feedback drives the Perlin noise gains and the camera shakes, then settles back to 0.

### Workflow: UI shake

**Goal:** shake a whole canvas panel instead of the camera.

**Steps:**
1. Under the Canvas, create an empty `UIContainer` and move the UI elements to shake under it.
2. On `UIContainer`, add `MMWiggle` (tick **Position**, untick **Wiggle Permitted**) and `MMCameraShaker`.
3. Add **Camera > Camera Shake** to an MMF Player. UI moves in pixels, so use larger amplitudes; the documented example uses `Duration 0.5, Amplitude 20, Frequency 0.03`.
4. If a real camera shaker also exists, give the UI shaker and its feedback their own **Channel** (see the channel workflow) so they don't both react.

**Expected result:** only the `UIContainer` contents shake.

### Workflow: Camera zoom punch, fade and flash

**Goal:** add the other common camera feedbacks.

**Steps:**
1. **Zoom:** add **Camera > Camera Zoom** (`MMF_CameraZoom`). Defaults: `ZoomMode = For`, `ZoomFieldOfView = 30`, `ZoomTransitionDuration = 0.05`, `ZoomDuration = 0.1`. The receiver is `MMCameraZoom` on a regular camera (`More Mountains > Feedbacks > Shakers > Camera > MM Camera Zoom`) or `MMCinemachineZoom` on a Cinemachine camera. **Automatic Shaker Setup** adds the right one.
2. **Fade:** add **Camera > Fade** (`MMF_Fade`). `FadeType` is `FadeIn` (to opaque), `FadeOut` (to clear) or `Custom`, with `Duration = 1` by default. Automatic setup creates `FadeCanvas` (screen-space overlay) with a black full-screen `FadeImage` and an `MMFader`. Manual: a full-screen black UI Image with `MMFader` (it adds a `CanvasGroup`; set its alpha to 0).
3. **Flash:** add **Camera > Flash** (`MMF_Flash`, `FlashColor = white`, `FlashDuration = 0.2`, `FlashAlpha = 1`). Automatic setup creates `FlashCanvas` with a `FlashImage` and an `MMFlash`.
4. Fade and flash match receivers by ID: `MMF_Fade.ID` ↔ the fader's ID, `MMF_Flash.FlashID` ↔ the flash's ID (both 0 by default).

**Expected result:** zoom narrows the FOV briefly; fade darkens or clears the screen; flash shows a brief white overlay.

### Workflow: Shake from code without a player

**Goal:** trigger a shake directly from gameplay code.

**Steps:**

```csharp
using MoreMountains.Feedbacks;

// processed by MMCameraShaker and MMCinemachineCameraShaker listening on channel 0
MMCameraShakeEvent.Trigger(0.3f, 2f, 40f, 0f, 0f, 0f, false, new MMChannelData(MMChannelModes.Int, 0, null));
// stop shakes on that channel (only MMCinemachineCameraShaker listens to the stop event)
MMCameraShakeStopEvent.Trigger(new MMChannelData(MMChannelModes.Int, 0, null));
```

Signature: `Trigger(float duration, float amplitude, float frequency, float amplitudeX, float amplitudeY, float amplitudeZ, bool infinite = false, MMChannelData channelData = null, bool useUnscaledTime = false)`.

**Expected result:** the shaker on channel 0 shakes for 0.3 s.

### Workflow: Use channels to target one camera

**Goal:** shake only one camera (split screen, UI vs world).

**Steps:**
1. On the shaker, set `ChannelMode` (`Int` by default) and `Channel`, or use `MMChannel` mode with an asset created via `Assets > Create > MoreMountains > MMChannel`.
2. On the feedback, under **Feedback Settings**, set the same `ChannelMode` and `Channel` / `MMChannelDefinition`.

**Expected result:** only shakers whose channel matches react.

### Workflow: Let players turn screen shake off

**Goal:** an accessibility or settings toggle.

**Steps:**
1. Each feedback type has a static switch. Turn off the ones your game uses:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;

public static class ShakeSettings
{
    public static void SetScreenShake(bool enabled)
    {
        MMF_CameraShake.FeedbackTypeAuthorized = enabled;
        MMF_CinemachineImpulse.FeedbackTypeAuthorized = enabled;
        MMF_CameraZoom.FeedbackTypeAuthorized = enabled;
    }
}
```

2. Wire `SetScreenShake` to the settings UI (for example a Toggle's `onValueChanged`).
3. No-code alternative: add an `MMFeedbacksAuthorizations` component to the scene and untick the camera feedbacks.

**Expected result:** with the setting off, the camera feedbacks play nothing; other feedbacks still play.

## Verification

- Cinemachine route: `Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>() != null`, and at least one `CinemachineCamera` has a `CinemachineImpulseListener`.
- Rig route: `Object.FindAnyObjectByType<MoreMountains.Feedbacks.MMCameraShaker>() != null`, its GameObject has an `MMWiggle` with `PositionActive == true`, and `Camera.main.transform.parent` is that GameObject.
- Cinemachine camera shake: the `CinemachineCamera` has both `CinemachineBasicMultiChannelPerlin` and `MoreMountains.FeedbacksForThirdParty.MMCinemachineCameraShaker`.
- Zoom: an `MMCameraZoom` (regular camera) or `MMCinemachineZoom` (Cinemachine) exists.
- Fade/flash: `Object.FindAnyObjectByType<MoreMountains.Tools.MMFader>()` / `Object.FindAnyObjectByType<MoreMountains.Feedbacks.MMFlash>()` is not null.
- The player contains the intended feedback type (`player.GetFeedbackOfType<MMF_CameraShake>() != null`, etc.) and feedback and shaker channels match.
- In Play mode, `Camera.main.transform.position` (or the `CameraShaker` local position) changes while the feedback plays and returns to rest afterwards.
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MoreMountains.FeedbacksForThirdParty.MMF_CinemachineImpulse` | Feedback (`Camera/Cinemachine Impulse`) | Emits a Cinemachine impulse (`m_ImpulseDefinition`, `Velocity`). |
| `MoreMountains.Feedbacks.MMF_CameraShake` | Feedback (`Camera/Camera Shake`) | Broadcasts `MMCameraShakeEvent` with `CameraShakeProperties`. |
| `MoreMountains.Feedbacks.MMCameraShaker` | Component | Receives camera shake events; needs `MMWiggle`. |
| `MoreMountains.FeedbacksForThirdParty.MMCinemachineCameraShaker` | Component | Receives camera shake events on a Cinemachine camera via Perlin noise. |
| `MoreMountains.Feedbacks.MMF_CameraZoom` | Feedback (`Camera/Camera Zoom`) | Zoom; received by `MMCameraZoom` / `MMCinemachineZoom`. |
| `MoreMountains.Feedbacks.MMF_Fade` / `MoreMountains.Tools.MMFader` | Feedback / Component | Full-screen fade. |
| `MoreMountains.Feedbacks.MMF_Flash` / `MoreMountains.Feedbacks.MMFlash` | Feedback / Component | Full-screen flash. |
| `MMF_Player.AutomaticShakerSetup()` | Method (editor) | Runs every feedback's automatic setup. |
| `MMCameraShakeEvent.Trigger(...)` / `MMCameraShakeStopEvent.Trigger(...)` | Static events | Shake from code / stop Cinemachine camera shakes. |
| `<FeedbackClass>.FeedbackTypeAuthorized` | Static bool | Enable/disable a feedback type globally. |

## Common issues

- **Feedback plays but nothing shakes** → no receiver: impulse needs a `CinemachineImpulseListener` on the active Cinemachine camera; Camera Shake needs an `MMCameraShaker` or `MMCinemachineCameraShaker` → run **Automatic Shaker Setup**, or add the receiver manually.
- **Camera Shake feedback does nothing although a Cinemachine camera exists** → when a Cinemachine camera already exists, Camera Shake's automatic setup only ensures the brain and impulse listener; it does not add `MMCinemachineCameraShaker` → follow "Camera Shake on a Cinemachine camera", or use the Cinemachine Impulse feedback instead.
- **MMCinemachineCameraShaker does nothing** → the Cinemachine camera has no `CinemachineBasicMultiChannelPerlin` noise component (the shaker silently skips the shake when none is found) → add one with a noise profile and gains at 0.
- **Automatic setup throws a NullReferenceException** → no camera is tagged `MainCamera` → tag the scene camera, then retry.
- **The camera shakes once, then the controller snaps it back (or the shake is ignored)** → a script writes the camera's own transform every frame → move the camera controller to `CameraRig` and keep the shaker on the child.
- **Only one of several cameras should shake, but all do** → all shakers are on channel 0 → assign distinct channels on shakers and feedbacks.
- **Impulse feels too strong or too weak** → `Velocity` scales the impulse (default 5,5,5); the player's intensity (`PlayFeedbacks(position, intensity)`) also multiplies it.
- **Shake ignores slow motion (or should ignore it)** → set the feedback's **Timing > Timescale Mode** to `Unscaled` or `Scaled` as needed.

## Boundaries

- Feel has no "view shake" that moves the rendered image itself; to shake the whole rendered frame, render to a RenderTexture displayed on a UI element and shake that element (UI shake workflow).
- Cinemachine impulse behaviour (signal shapes, propagation, dissipation) is Cinemachine's own system; Feel only fires the impulse. Advanced impulse tuning belongs to Cinemachine's documentation.
- Automatic Shaker Setup is a best-effort scene change. It can re-parent the main camera or create cameras and canvases, so make sure the scene is saved and under version control first.
- Pass-through post-processing shakes (lens distortion, chromatic aberration) are covered by `feel-post-processing-effects`.
