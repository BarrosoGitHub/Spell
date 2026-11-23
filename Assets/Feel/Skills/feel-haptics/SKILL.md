---
name: feel-haptics
description: "Use this skill whenever the user wants vibration, haptics or controller rumble with Feel's bundled Nice Vibrations — e.g. 'make the phone vibrate when I get hit', 'haptics on my button taps', 'rumble the gamepad on explosion', 'play a haptic preset', 'light impact vibration on iOS', 'turn off vibrations in settings', 'generate a haptic from my sound effect', 'vibrations don't work on android/iphone', 'is haptics supported here'. Covers the Nice Vibrations install check and define, haptics feedbacks in an MMF_Player (Preset, Emphasis, Continuous, Clip, Control), the HapticPatterns / HapticController / DeviceCapabilities API, HapticSource and HapticReceiver, haptic clips, audio-to-haptic conversion, gamepad rumble, global enable and level, and platform caveats. Do NOT use for audio playback (see feel-sound-manager) or visual screen shake (see feel-screen-shake). When in doubt whether a vibration request could involve Feel, use this skill — Prerequisites shows how to confirm Nice Vibrations is installed."
metadata:
  asset: "Feel"
  publisher: "More Mountains"
  asset-version: "6.4"
  skill-version: "1.0.1"
  unity: "6000.5+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/particles-effects"
  asset-store-url: "https://assetstore.unity.com/packages/tools/particles-effects/feel-183370"
  documentation-url: "https://feel-docs.moremountains.com/"
  support-url: "https://feel.moremountains.com/"
  last-verified: "2026-11-20"
---

# Add Haptics with Nice Vibrations

Feel includes Nice Vibrations (namespace `Lofelt.NiceVibrations`, assembly `Lofelt.NiceVibrations`), a haptics library for iOS, Android and gamepads. Haptics can be played from an MMF Player with the `Haptics > *` feedbacks, from code with `HapticPatterns` (presets, emphasis, constant) and `HapticController` (`.haptic` clips), or with `HapticSource` components. This skill lets an agent confirm the haptics integration is active, add haptics to feedback sequences or code, use clips (including ones generated from audio), expose a vibration toggle, and handle device and platform limits.

## When to use this skill

- "vibrate the phone when the player takes damage"
- "add a light tap haptic to every button"
- "rumble the controller when the car crashes"
- "play the Success / Warning / HeavyImpact preset"
- "make a haptic that matches my explosion sound"
- "add a vibration on/off toggle and a strength slider"
- "haptics don't work on my android phone / first 30s on iOS"
- "check if this device supports haptics"

Not for:
- Playing audio → `feel-sound-manager`
- Visual camera shake → `feel-screen-shake`
- The rest of a feedback sequence → `feel-add-feedbacks`

## Prerequisites

- Feel 6.1, Unity 6000.5+, any render pipeline.
- **Install check (all three):**
  1. Folder `Assets/Feel/NiceVibrations` exists.
  2. `System.Type.GetType("Lofelt.NiceVibrations.HapticPatterns, Lofelt.NiceVibrations")` is not null.
  3. The scripting define `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` is present (`Edit > Project Settings > Player > Other Settings > Scripting Define Symbols`, or `UnityEditor.PlayerSettings.GetScriptingDefineSymbols(...)`). Feel's haptics feedbacks only compile their body when it is set. Nice Vibrations' `NiceVibrationsDefineSymbols` editor class adds it automatically on load.
  If 1 or 2 fails, Nice Vibrations was removed from the project: tell the user to re-import Feel from https://assetstore.unity.com/packages/tools/particles-effects/feel-183370 (Nice Vibrations ships inside it). If only 3 fails, trigger a script recompilation so the define gets added, then check again.
- **Device requirements:** iPhone 7+/iOS 11+ for basic haptics (iPhone 8+/iOS 13+ for clips and modulation); Android API 17+ with an actuator (API 26+ with amplitude control for clips). Gamepad rumble needs the **Input System** package and **Active Input Handling** set to **Input System** or **Both**.
- **Build:** iOS builds must be made on a Mac. Unity Cloud Build is not supported out of the box.
- Haptics can't be felt in the Editor on desktop without a gamepad. Verify wiring in the Editor; verify the feel on a device.

## Quick start

A light-impact haptic on a button press, via an MMF Player:

1. Run the install check.
2. On the button's MMF Player (or a new one, see `feel-add-feedbacks`), **Add new feedback > Haptics > Haptic Preset** (`MMF_NVPreset`). `Preset` defaults to `LightImpact`; pick `Selection`, `Success`, `Warning`, `Failure`, `LightImpact`, `MediumImpact`, `HeavyImpact`, `RigidImpact` or `SoftImpact`.
3. Editor-script equivalent:

```csharp
using Lofelt.NiceVibrations;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FeelHapticsQuickStart
{
    public static void AddTapHaptic(MMF_Player player)
    {
        Undo.RecordObject(player, "Add Haptic Preset");
        MMF_NVPreset preset = (MMF_NVPreset)player.AddFeedback(typeof(MMF_NVPreset));
        preset.Preset = HapticPatterns.PresetType.LightImpact;
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
    }
}
```

4. Bind the button's **On Click ()** to the player's `PlayFeedbacks()`.
5. Expected result: the player contains an `MMF_NVPreset` with `Preset == LightImpact`; on a supported phone, every tap produces a short light vibration. (Without code: `HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);`.)

## Workflows

### Workflow: Haptics from code

**Goal:** trigger vibrations directly from gameplay scripts.

**Steps:**

```csharp
using Lofelt.NiceVibrations;
using UnityEngine;

public class PlayerHaptics : MonoBehaviour
{
    public HapticClip HitClip; // assign a .haptic asset

    public void OnLand()   { HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact); }
    public void OnShoot()  { HapticPatterns.PlayEmphasis(1.0f, 0.5f); }        // amplitude, frequency (0-1)
    public void OnEngine() { HapticPatterns.PlayConstant(0.6f, 0.2f, 1.0f); }  // amplitude, frequency, duration (s)
    public void OnHit()
    {
        HapticController.fallbackPreset = HapticPatterns.PresetType.HeavyImpact; // used where clips aren't supported
        HapticController.Play(HitClip);
    }
    public void StopAll() { HapticController.Stop(); }
}
```

`HapticController.Play(clip)` checks `DeviceCapabilities.meetsAdvancedRequirements` and plays the fallback preset on devices that can't play clips. Unsupported features are silently ignored, so no guard checks are needed.

**Expected result:** each method produces its vibration on supported hardware and does nothing elsewhere.

### Workflow: Haptics feedbacks in an MMF Player

**Goal:** sync haptics with the visual and audio feedbacks.

**Steps:** add from `Haptics >` in **Add new feedback**:
- `Haptic Preset` (`MMF_NVPreset`): `Preset`.
- `Haptic Emphasis` (`MMF_NVEmphasis`): short sharp tap; `MinAmplitude`/`MaxAmplitude`, `MinFrequency`/`MaxFrequency` (all 1 by default) for random ranges.
- `Haptic Continuous` (`MMF_NVContinuous`): sustained vibration; amplitude, frequency and `MinDuration`/`MaxDuration` (1 s by default), optional `UseRealTimeModulation`.
- `Haptic Clip` (`MMF_NVClip`): plays a `.haptic` `Clip`, with `FallbackPreset` (`LightImpact` by default), `Loop`, `SeekTime`, level and frequency-shift randomization.
- `Haptic Control` (`MMF_NVControl`): `ControlType` = `Stop`, `EnableHaptics`, `DisableHaptics`, `AdjustHapticsLevel` (with `OutputLevel`), `Initialize` or `Release`.

Each has a **Settings** group (`MMFeedbackNVSettings`) with `OnlyPlayIfHapticsSupported` (true by default), `OnlyPlayIfAdvancedRequirementsMet`, and `ForceGamepadID` / `GamepadID` for multi-controller games. Do not use `Haptics DEPRECATED!` (`MMF_Haptics`).

**Expected result:** the haptic fires at the same moment as the other feedbacks in the player.

### Workflow: Use haptic clips (samples or generated from audio)

**Goal:** rich, designed haptics.

**Steps:**
1. Bundled clips: `Assets/Feel/NiceVibrations/HapticSamples/` contains 131 royalty-free `.haptic` clips in folders such as `ApplicationUX`, `Footsteps`, `Impacts`, `Music`. Assign one to `MMF_NVClip.Clip` or a `HapticClip` field.
2. From a sound already in the same MMF Player: add `Haptic Clip`, open its **Audio To Haptic** group and press **Convert MMSM Sound feedback Audio Clip to Haptic**. It takes the clip of the first `MMSoundManager Sound` feedback on the player (or the one whose label matches `MMSMSoundFeedbackLabel`), writes `<clipname>.haptic` next to the audio file, and assigns it to `Clip`. `SampleCount` (256) sets the resolution. This creates an asset; tell the user where it was written.
3. Custom clips authored in Lofelt Haptics Studio can be dropped into `Assets/` as `.haptic` files.

**Expected result:** `Clip` references a `HapticClip` asset; playing the player plays it on capable devices (and the fallback preset on others).

### Workflow: Component-based haptics (HapticSource / HapticReceiver)

**Goal:** audio-like haptic components without code.

**Steps:**
1. Add **exactly one** `HapticReceiver` (`Add Component > Nice Vibrations > Haptic Receiver`) to an object that lives for the whole app, typically the main camera. It initializes Nice Vibrations on scene load and exposes the global level and on/off switch.
2. Add `HapticSource` (`Add Component > Nice Vibrations > Haptic Source`) to objects that play clips; set `clip`, `loop`, `level`, `frequencyShift` and `priority` (0 highest, 255 lowest; only one clip plays at a time, so higher-priority sources win).
3. Call `hapticSource.Play()` / `Stop()` from code or UnityEvents.

**Expected result:** sources play their clips and respect priority.

### Workflow: Vibration settings toggle and strength

**Goal:** let players disable or scale haptics.

**Steps:**
1. Code: `HapticController.hapticsEnabled = false;` to disable, `true` to enable; `HapticController.outputLevel = 0.5f;` for global strength.
2. Feedback alternative: `Haptics > Haptic Control` with `EnableHaptics` / `DisableHaptics` / `AdjustHapticsLevel`.
3. To silence only Feel's haptics feedbacks, set `MMF_NVPreset.FeedbackTypeAuthorized = false` (and likewise for `MMF_NVClip`, `MMF_NVEmphasis`, `MMF_NVContinuous`).
4. Persist the setting yourself (e.g. `PlayerPrefs`) and re-apply it at startup.

**Expected result:** with the toggle off, no haptics play; the level slider scales intensity.

### Workflow: Gamepad rumble

**Goal:** rumble on PC/console controllers.

**Steps:**
1. Make sure the Input System package is installed and **Active Input Handling** is `Input System` or `Both` (ask before changing project settings).
2. The same APIs (`HapticPatterns`, `HapticController`, `HapticSource`, the feedbacks) automatically play on a connected gamepad.
3. Multiple gamepads: `GamepadRumbler.SetCurrentGamepad(index);` (0-based) or the feedback settings' `ForceGamepadID` / `GamepadID`.

**Expected result:** a connected gamepad rumbles. Frequency shift has no effect on gamepads.

## Verification

- `System.Type.GetType("Lofelt.NiceVibrations.HapticController, Lofelt.NiceVibrations") != null`.
- `UnityEditor.PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup))` contains `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED`.
- The player has the intended feedback with its key field set: `player.GetFeedbackOfType<MoreMountains.FeedbacksForThirdParty.MMF_NVPreset>().Preset == HapticPatterns.PresetType.LightImpact` (or `MMF_NVClip.Clip != null`).
- If components are used: exactly one `HapticReceiver` in the scene (`Object.FindObjectsByType<Lofelt.NiceVibrations.HapticReceiver>().Length == 1`).
- Generated clips exist on disk next to their source audio (`<name>.haptic`).
- In Play mode, calls run without exceptions. On desktop without a gamepad, nothing is felt; that is expected.
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `HapticPatterns.PlayPreset(HapticPatterns.PresetType)` | Static method | Plays one of 9 presets. |
| `HapticPatterns.PlayEmphasis(float amplitude, float frequency)` | Static method | Short sharp haptic. |
| `HapticPatterns.PlayConstant(float amplitude, float frequency, float duration)` | Static method | Sustained vibration. |
| `HapticController.Play(HapticClip)` / `Stop()` / `Load()` / `Seek(float)` / `Loop(bool)` | Static methods | Clip playback control. |
| `HapticController.hapticsEnabled` / `outputLevel` / `clipLevel` / `clipFrequencyShift` / `fallbackPreset` | Static properties | Global switch, level, modulation, fallback. |
| `HapticController.PlaybackStopped` | Static `Action` | Callback when haptics stop. |
| `DeviceCapabilities.isVersionSupported` / `meetsAdvancedRequirements` / `hasAmplitudeModulation` | Static properties | Capability checks. |
| `HapticSource` / `HapticReceiver` | Components | Component-based playback / global init and settings. |
| `GamepadRumbler.SetCurrentGamepad(int)` | Static method | Choose the target gamepad. |
| `MMF_NVPreset`, `MMF_NVEmphasis`, `MMF_NVContinuous`, `MMF_NVClip`, `MMF_NVControl` | Feedbacks (`MoreMountains.FeedbacksForThirdParty`) | Haptics inside an MMF Player. |

## Common issues

- **Haptics feedbacks don't appear in Add new feedback, or do nothing** → `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` is missing → let `NiceVibrationsDefineSymbols` add it (recompile), or add it in Player settings.
- **Nothing happens in the Editor** → desktop machines have no haptic actuator → test on a device, or with a gamepad.
- **iOS: vibrations (other than emphasis) fail for the first ~30 seconds** → a known CoreHaptics initialization issue → initialize early: add a `HapticReceiver` to the first scene, or call `HapticController.Init()` in `Awake`/`Start`.
- **Clip plays on iPhone but not on some Android phones** → the device lacks advanced capabilities (API 26+ with amplitude control) → set a `fallbackPreset` / `FallbackPreset`; `OnlyPlayIfAdvancedRequirementsMet` can skip it instead.
- **Frequency changes have no effect on Android or gamepads** → those platforms can't change actuator frequency → design around amplitude only.
- **Looping clips feel choppy on older Android** → an Android limitation, fixed in Android 12 → keep looping clips short.
- **iOS sound is gone after closing an AdMob ad** → release and re-initialize the haptic bindings around the ad: `LofeltHaptics.Release()` before showing it, `LofeltHaptics.Initialize()` after.
- **Gamepad doesn't rumble** → Input System not installed or not active, or the gamepad is connected to a phone (unsupported) → install/enable the Input System.
- **Footstep clip interrupts an explosion clip** → only one clip plays at a time → give important `HapticSource`s a lower `priority` number.

## Boundaries

- Nice Vibrations plays one haptic clip at a time; there is no mixing of several clips.
- No haptics on gamepads connected to mobile devices, no Linux gamepad support. Different haptics can play on multiple gamepads at once (each gamepad keeps its own playback state).
- Authoring new `.haptic` clips from scratch requires Lofelt Haptics Studio (an external tool); inside Unity you can only use bundled samples or convert audio with the `Haptic Clip` feedback.
- Older Nice Vibrations versions (needed for AHAP support) ship as `.unitypackage`s in `Assets/Feel/NiceVibrations/OlderVersions/`; switching versions means removing the current one first, which is outside this skill's scope.
