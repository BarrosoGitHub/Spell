---
name: feel-post-processing-effects
description: "Use this skill whenever the user wants to briefly animate a post-processing / volume effect as feedback using Feel — e.g. 'pulse the vignette red when I get hurt', 'chromatic aberration hit effect', 'lens distortion bump on landing', 'bloom flash on power up', 'desaturate the screen on death', 'depth of field focus pull', 'my vignette feedback does nothing', 'add a URP volume shake'. Covers URP and HDRP Volume feedbacks and Built-in Post Processing v2 feedbacks, their shaker components, Automatic Shaker Setup, remap and relative values, durations, channels, and triggering shakers from code. Do NOT use for camera position shake, zoom, fade or flash (see feel-screen-shake), spring-driven volume values (see feel-springs), or setting up a render pipeline from scratch. When in doubt whether a screen-effect request could involve Feel, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Feel"
  publisher: "More Mountains"
  asset-version: "6.4"
  skill-version: "1.2.0"
  unity: "6000.5+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/particles-effects"
  asset-store-url: "https://assetstore.unity.com/packages/tools/particles-effects/feel-183370"
  documentation-url: "https://feel-docs.moremountains.com/"
  support-url: "https://feel.moremountains.com/"
  last-verified: "2026-11-20"
---

# Animate Post-Processing with Feel Feedbacks

Feel's post-processing feedbacks (`PostProcess/*` in the **Add new feedback** menu) don't touch volumes directly. Each one broadcasts an event, and a **shaker** component sitting on the Volume receives it and animates the matching volume override over time (for example `MMF_Vignette_URP` → `MMVignetteShaker_URP` → URP `Vignette.intensity`). This skill lets an agent pick the right feedback/shaker pair for the project's render pipeline, set up the volume, tune intensity curves and remaps, and verify the effect.

## When to use this skill

- "flash the vignette red when the player takes damage"
- "chromatic aberration spike on hit" / "lens distortion bump when landing"
- "bloom burst on pickup" / "brief exposure flash" (HDRP)
- "desaturate / hue shift the screen for a moment" (Color Adjustments)
- "focus pull with depth of field when the cutscene starts"
- "add a URP / HDRP volume feedback" / "post process feedback for built-in"
- "the vignette feedback plays but nothing changes on screen"

Not for:
- Moving, rotating or zooming the camera, fade or flash overlays → `feel-screen-shake`
- Spring-driven volume values (`MMSpringVignetteIntensity_URP` and similar) → `feel-springs`
- Building the rest of the sequence (sound, particles, timing) → `feel-add-feedbacks`

## Prerequisites

- Feel 6.1, Unity 6000.5+.
- **Install check:** `System.Type.GetType("MoreMountains.Feedbacks.MMF_Player, MoreMountains.Tools")` is not null. If null, tell the user Feel is not installed (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) and stop.
- **Identify the pipeline** and check the matching define is active by resolving a type:

| Pipeline | Package | Check type resolves | Feedback names |
|---|---|---|---|
| URP | `com.unity.render-pipelines.universal` | `MoreMountains.FeedbacksForThirdParty.MMVignetteShaker_URP, MoreMountains.Tools` | `PostProcess/<Effect> URP` |
| HDRP | `com.unity.render-pipelines.high-definition` | `MoreMountains.FeedbacksForThirdParty.MMVignetteShaker_HDRP, MoreMountains.Tools` | `PostProcess/<Effect> HDRP` |
| Built-in | `com.unity.postprocessing` (Post Processing v2) | `MoreMountains.FeedbacksForThirdParty.MMVignetteShaker, MoreMountains.Tools` | `PostProcess/<Effect>` (no suffix) |

  Only use feedbacks from the project's own pipeline; mixing them does nothing. If the Built-in route is needed and Post Processing v2 is missing, ask before installing it.
- Automatic setup uses `Camera.main`, so the scene camera must be tagged `MainCamera`.

## Quick start

URP vignette pulse on hit:

1. Make sure the scene camera is tagged `MainCamera`, and save the scene (automatic setup modifies the scene and the volume profile asset).
2. Create an empty GameObject `HurtFeedback`, add **MMF Player**, then **Add new feedback > PostProcess > Vignette URP** (`MMF_Vignette_URP`).
3. Outside Play mode, expand the feedback's **Automatic Setup** group and press **Automatic Shaker Setup**. It enables **Post Processing** on the main camera's URP camera data, instantiates Feel's default global volume (`MMDefaultURPVolume`) if the scene has no `Volume`, adds a `Vignette` override to the volume profile if missing, and adds `MMVignetteShaker_URP` to the volume GameObject. Editor-script equivalent:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelVignetteQuickStart
{
    public static void CreateHurtVignette()
    {
        GameObject go = new GameObject("HurtFeedback");
        Undo.RegisterCreatedObjectUndo(go, "Create HurtFeedback");
        MMF_Player player = go.AddComponent<MMF_Player>();
        Undo.RecordObject(player, "Add Vignette URP");
        MMF_Vignette_URP vignette = (MMF_Vignette_URP)player.AddFeedback(typeof(MMF_Vignette_URP));
        vignette.Duration = 0.3f;
        vignette.RemapIntensityOne = 0.5f;
        vignette.InterpolateColor = true;
        vignette.TargetColor = Color.red;
        player.AutomaticShakerSetup(); // edit mode only; logs "You're all set."
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
```

4. Call `HurtFeedback.PlayFeedbacks()` from the damage code, or press the green **Play** button on the player in Play mode.
5. Expected result: the scene has a `Volume` whose profile contains a `Vignette` override and whose GameObject has `MMVignetteShaker_URP`; the main camera renders post-processing; playing the feedback darkens the screen edges (tinted red) over 0.3 s and returns to the initial value.

## Workflows

### Workflow: Set up any post-processing feedback manually

**Goal:** wire a feedback and shaker without automatic setup (existing volume, custom profile).

**Steps:**
1. Find the pair in [references/shaker-map.md](references/shaker-map.md) (for example `MMF_LensDistortion_URP` ↔ `MMLensDistortionShaker_URP` ↔ `LensDistortion`).
2. **URP/HDRP:** on the GameObject with the `Volume` (global, or local with the camera inside it), make sure the profile contains the override (for example **Add Override > Post-processing > Lens Distortion**). In URP, also tick **Rendering > Post Processing** on the camera.
   **Built-in:** the camera needs a `PostProcessLayer` and the scene a `PostProcessVolume` (**Is Global** ticked) whose profile has the effect.
3. Add the shaker to the **same GameObject as the volume**: `Add Component > More Mountains > Feedbacks > Shakers > PostProcessing > MM Lens Distortion Shaker URP` (shakers have `[RequireComponent(typeof(Volume))]`).
4. Add the matching feedback to an MMF Player.
5. Keep the shaker and feedback channels equal (both `Int` channel 0 by default).

**Expected result:** playing the feedback animates that override, then restores it.

### Workflow: Tune intensity, duration and shape

**Goal:** make the effect read well in the game.

**Steps:**
1. **Duration:** most feedbacks use `Duration` (for example vignette 0.2 s, chromatic aberration 0.2 s, lens distortion 0.8 s by default); some use `ShakeDuration` (bloom 0.2 s, color adjustments 1 s, depth of field 2 s).
2. **Curve:** the intensity curve (`Intensity` or `ShakeIntensity`) goes from x=0 to x=1 over the duration. Keep y between 0 and 1; the default is a bell `0 → 1 → 0`.
3. **Remap:** `RemapIntensityZero` and `RemapIntensityOne` map the curve's y=0 and y=1 to real values. Vignette defaults to 0 → 1, lens distortion to 0 → 0.5. A bell curve with remap 0 → 0.5 makes vignette intensity go 0 → 0.5 → 0.
4. **Relative values:** with `RelativeIntensity = true`, the remapped value is added to the override's current value instead of replacing it. Use it when the volume already has a non-zero base value.
5. **Intensity multiplier:** `player.PlayFeedbacks(position, 0.5f)` halves the effect; per-feedback randomness lives in the **Feedback Randomness** group.
6. **Restore behaviour:** `ResetShakerValuesAfterShake` and `ResetTargetValuesAfterShake` (both true by default on the URP vignette) restore the volume when the shake ends.
7. **Animate only some values (URP):** feedbacks that drive several values have one `Modify<Value>` toggle per value, all `true` by default: `MMF_ColorAdjustments_URP` (`ModifyPostExposure`, `ModifyHueShift`, `ModifySaturation`, `ModifyContrast`), `MMF_Bloom_URP` (`ModifyIntensity`, `ModifyThreshold`), `MMF_DepthOfField_URP` (`ModifyFocusDistance`, `ModifyAperture`, `ModifyFocalLength`), `MMF_ChannelMixer_URP` (`ModifyRed`, `ModifyGreen`, `ModifyBlue`), `MMF_WhiteBalance_URP` (`ModifyTemperature`, `ModifyTint`) and `MMF_Vignette_URP` (`ModifyIntensity`, combine with `InterpolateColor` for a color-only vignette). Untick a toggle and the shaker neither animates nor resets that value, so another system (a spring, a script) can keep driving it. The matching shakers have the same toggles, used when the shaker plays with its own values (`OnlyUseShakerValues`).
8. **HDRP exposure in any exposure mode:** `MMF_Exposure_HDRP` (and `MMExposureShaker_HDRP`) can drive four `Exposure` values, each with its own `Modify<Value>` toggle, curve, remap zero/one and relative toggle. Pick them based on the volume's Exposure **Mode**: `ModifyFixedExposure` (on by default, `FixedExposure` curve) for **Fixed**; `ModifyCompensation` (off by default, `Compensation` curve, relative by default, remaps 0 to 2) for **Automatic**, **Automatic Histogram**, **Curve Mapping** and **Use Physical Camera**; `ModifyLimitMin` / `ModifyLimitMax` (off by default) to animate the exposure range of **Automatic** and **Automatic Histogram**. For a non-Fixed volume, turn `ModifyCompensation` on and `ModifyFixedExposure` off. The feedback doesn't change the volume's mode. From code, `MMExposureShakeEvent_HDRP.Trigger` takes the same values as optional named parameters (`modifyCompensation`, `compensation`, `remapCompensationZero`...).

**Expected result:** values change as configured; the volume returns to its starting value at the end.

### Workflow: Combine several volume effects in one hit

**Goal:** a punchy "impact" made of several effects.

**Steps:**
1. In one MMF Player, add `PostProcess > Chromatic Aberration URP` (`RemapIntensityOne` 1), `PostProcess > Lens Distortion URP` (`RemapIntensityOne` 0.5), `PostProcess > Vignette URP`.
2. Press the **Automatic Shaker Setup** button at the bottom of the MMF Player settings (it runs every feedback's setup), or call `player.AutomaticShakerSetup()`.
3. Check that the volume now has all three overrides and all three shakers.

**Expected result:** one play triggers all three effects at once.

### Workflow: Target one volume among several (channels)

**Goal:** affect only a specific camera's volume (split screen, UI camera).

**Steps:**
1. On the shaker, set `ChannelMode` and `Channel` (or an `MMChannel` asset made with `Assets > Create > MoreMountains > MMChannel`).
2. On the feedback, in **Feedback Settings**, set the same channel.

**Expected result:** only the shaker on the matching channel reacts.

### Workflow: Trigger a volume shake from code

**Goal:** drive a shaker without an MMF Player.

**Steps:** every shaker file defines its event struct. For the URP vignette:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;

public class HurtVignette : MonoBehaviour
{
    public AnimationCurve Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

    public void Hurt()
    {
        // curve, duration, remapMin, remapMax, relative, attenuation, channel
        MMVignetteShakeEvent_URP.Trigger(Curve, 0.3f, 0f, 0.5f, false, 1f, new MMChannelData(MMChannelModes.Int, 0, null));
    }
}
```

Open the matching shaker file (for example `MMFeedbacks/MMFeedbacksForThirdParty/URP/Shakers/MMLensDistortionShaker_URP.cs`) to read its event's exact `Trigger` signature before calling it.

**Expected result:** the shaker on channel 0 animates its override.

### Workflow: Test a shaker in isolation

**Goal:** check the receiving side works before debugging the feedback.

**Steps:**
1. Enter Play mode.
2. Call the shaker's own `StartShaking()` (public on every `MMShaker`), which plays it with the settings stored on the shaker itself (its `ShakeDuration`, which defaults to 0.2 s, plus its own curve and remap fields):

```csharp
var shaker = Object.FindAnyObjectByType<MoreMountains.FeedbacksForThirdParty.MMVignetteShaker_URP>();
shaker.StartShaking();
```

**Expected result:** the override animates even without a feedback; if it does, any remaining problem is on the feedback side (wrong pipeline variant or channel).

## Verification

- The volume GameObject has the shaker: for URP vignette, `volume.GetComponent<MoreMountains.FeedbacksForThirdParty.MMVignetteShaker_URP>() != null`.
- The profile has the override: URP/HDRP `volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Vignette v)` (or the HDRP type) returns true.
- URP: `Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing == true`. Built-in: `Camera.main.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>() != null`.
- The player contains the matching feedback (`player.GetFeedbackOfType<MMF_Vignette_URP>() != null`) and its `Channel` equals the shaker's `Channel`.
- In Play mode, during the feedback, the runtime profile value changes (for example the vignette `intensity.value` differs from its initial value) and returns afterwards.
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MMF_<Effect>_URP` / `MMF_<Effect>_HDRP` / `MMF_<Effect>` | Feedbacks (`MoreMountains.FeedbacksForThirdParty`) | Broadcast a shake event for one volume effect. |
| `MM<Effect>Shaker_URP` / `_HDRP` / (none) | Components (`MMShaker`) | Animate one override on the Volume they sit on. |
| `MMF_Feedback.AutomaticShakerSetup()` / `MMF_Player.AutomaticShakerSetup()` | Methods (edit mode) | Create volume, override and shaker if missing. |
| `MM<Effect>ShakeEvent_URP.Trigger(...)` | Static event | Trigger a shaker from code. |
| `MMShaker.StartShaking()` / `ShakeDuration` / `Channel` | Shaker API | Play the shaker with its own settings, base duration, channel. |
| `MMF_GlobalPPVolumeAutoBlend_URP` | Feedback | Blend a whole volume's weight instead of one value. |

Read [references/shaker-map.md](references/shaker-map.md) for the full feedback → override → shaker table for URP, HDRP and Built-in.

## Common issues

- **Feedback plays, screen doesn't change** → no matching shaker on the volume, a shaker from another pipeline, or channel mismatch → check the shaker-map table, put the shaker on the volume GameObject, align channels.
- **Nothing renders at all in URP** → post-processing is off on the camera → tick **Rendering > Post Processing** on the camera (automatic setup does this).
- **NullReferenceException from a shaker at Play** → the volume profile has no override of that type → add the override (for example **Add Override > Vignette**) or rerun Automatic Shaker Setup.
- **"Automatic shaker setup is only available outside of play mode."** → setup was run in Play mode → exit Play mode and run it again.
- **A URP volume value (for example saturation) is overwritten while only another one (post exposure) should change** → every `Modify<Value>` toggle on the feedback is on by default → untick the toggles of the values to leave alone.
- **HDRP Exposure feedback plays, brightness doesn't change** → the volume's Exposure mode isn't Fixed, and the feedback only drives `fixedExposure` by default, which other modes ignore → turn `ModifyCompensation` on (and `ModifyFixedExposure` off) on `MMF_Exposure_HDRP`.
- **Effect stays on after the shake** → `ResetTargetValuesAfterShake` / `ResetShakerValuesAfterShake` were turned off, or the curve ends above 0 → re-enable reset or end the curve at y=0.
- **Effect is weaker than expected** → the remap range is small (lens distortion defaults to 0.5), or the volume's weight/priority is low, or another volume overrides it → raise `RemapIntensityOne`, check volume weight and priority.
- **Changing the profile from automatic setup affected other scenes** → automatic setup adds overrides to the volume's **shared** profile asset → if that profile is shared across scenes, duplicate it first and assign the copy.
- **Built-in feedbacks missing from the menu** → `com.unity.postprocessing` is not installed → install it (with approval), or switch to the URP/HDRP variants if the project uses those pipelines.

## Boundaries

- Feel animates existing volume overrides; it does not create custom post-processing effects or shaders. A new effect needs a custom Volume component plus a custom shaker/feedback (see `feel-custom-feedback`).
- Post-processing costs what the pipeline's effects cost, which can be significant on mobile; Feel adds no rendering of its own.
- Automatic setup is a best effort: it picks the first `Volume` found in the scene. In scenes with several volumes, set things up manually on the intended one.
- URP 2D Renderer lights (`Lights/Light2D_URP`) and other non-volume effects are regular feedbacks, see `feel-add-feedbacks`.
