---
name: feel-add-feedbacks
description: "Use this skill whenever the user wants to add juice, game feel or feedback to something with Feel's MMF Player — e.g. 'make the coin pop when collected', 'add juice to my jump', 'play a feedback when the enemy gets hit', 'chain these effects one after another', 'wait for the feedback to finish then load the level', 'change the feedback intensity from code', 'my MMF_Player does nothing'. Covers creating MMF_Player components, adding feedbacks in the inspector or from C#, triggering from code, UnityEvents, animation events or Timeline, pauses, loops, runtime changes, completion callbacks, channel events and disabling feedback types. Do NOT use for camera shake (see feel-screen-shake), post-processing pulses (see feel-post-processing-effects), springs (see feel-springs), audio management (see feel-sound-manager) or new feedback classes (see feel-custom-feedback). When in doubt whether a juice request could involve Feel, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Add Feedbacks with the MMF Player

The `MMF_Player` component (namespace `MoreMountains.Feedbacks`) holds an ordered list of feedbacks (`MMF_Feedback` subclasses such as `MMF_Scale`, `MMF_Position`, `MMF_Particles`, `MMF_MMSoundManagerSound`) and plays them when told to. This skill lets an agent create players, fill them with feedbacks in the editor or from C#, wire them to gameplay (code, UnityEvents, animation events, Timeline), sequence them with pauses and loops, tweak them at runtime and react when they complete.

## When to use this skill

- "add some juice when the player jumps / lands"
- "make the pickup scale up and pop when collected"
- "when the enemy is hit, flash it, spawn particles and play a sound"
- "play these effects one after the other with a small delay"
- "loop this feedback 3 times" / "repeat forever until I stop it"
- "trigger the feedback from my button's OnClick"
- "play a feedback on frame 25 of my animation" / "from a Timeline marker"
- "how do I change the feedback's duration/intensity from code?"
- "run code when the MMF Player is done"
- "my MMF_Player doesn't play" / "the feedback shows a warning icon"
- "add a settings toggle that turns off all scale feedbacks"

Not for:
- Camera, Cinemachine or UI shake, zoom, fade, flash → `feel-screen-shake`
- Bloom / vignette / chromatic aberration / lens distortion → `feel-post-processing-effects`
- Spring-driven motion → `feel-springs`
- Sound manager tracks, volume, playlists → `feel-sound-manager` (a single sound feedback inside a player is fine here)
- Writing a new feedback type → `feel-custom-feedback`
- Setting up the MMSoundManager or music playlists → `feel-sound-and-playlist-setup`

## Prerequisites

- Feel 6.1 on Unity 6000.5+, any render pipeline.
- **Install check:** `System.Type.GetType("MoreMountains.Feedbacks.MMF_Player, MoreMountains.Tools")` must not be null, and the folder `Assets/Feel/MMFeedbacks` must exist. If either fails, stop and tell the user Feel is not installed; it is available at https://assetstore.unity.com/packages/tools/particles-effects/feel-183370.
- Scripts that reference Feel types need `using MoreMountains.Feedbacks;`. The `MoreMountains.Tools` assembly is auto-referenced, so code in `Assembly-CSharp` compiles without changes; code inside a custom assembly definition must add a reference to `MoreMountains.Tools`.
- Some feedback categories only exist when a package is installed (for example `TextMesh Pro/*` needs uGUI 2.x, `Camera/Cinemachine*` needs Cinemachine). See [references/feedback-catalog.md](references/feedback-catalog.md).

## Quick start

Goal: a cube that "pops" (scale bump) when gameplay code calls it.

1. Open the target scene. Select or create the object to animate (for example a `Cube`).
2. Create an empty GameObject named `PopFeedback` (`GameObject > Create Empty`) and add the component **MMF Player** (`Add Component > More Mountains > Feedbacks > MMF Player`).
3. In the MMF Player inspector, open **Add new feedback** and choose `Transform > Scale` (or type `Scale` in the search field).
4. In the new Scale feedback, drag the cube into **Animate Scale Target**. Defaults: `AnimateScaleDuration = 0.2`, `RemapCurveZero = 1`, `RemapCurveOne = 2`, curves that rise and fall back to 0, so the cube grows and returns to its original size.
5. Or do steps 2–4 from an editor script (this is what the inspector does internally: record undo, then `AddFeedback(type)`):

```csharp
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelQuickStart
{
    public static MMF_Player CreatePopFeedback(Transform target)
    {
        GameObject go = new GameObject("PopFeedback");
        Undo.RegisterCreatedObjectUndo(go, "Create PopFeedback");
        MMF_Player player = go.AddComponent<MMF_Player>();

        Undo.RecordObject(player, "Add Scale feedback");
        MMF_Scale scale = (MMF_Scale)player.AddFeedback(typeof(MMF_Scale));
        scale.Label = "Pop";
        scale.AnimateScaleTarget = target;
        scale.AnimateScaleDuration = 0.2f;

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(go.scene);
        return player;
    }
}
```

6. Trigger it from gameplay code:

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public MMF_Player PopFeedback; // assign PopFeedback in the inspector

    public void Collect()
    {
        PopFeedback?.PlayFeedbacks();
    }
}
```

7. Expected result: the Hierarchy contains `PopFeedback` with an `MMF_Player` whose `FeedbacksList` has one `MMF_Scale` targeting the cube. In Play mode, pressing the green **Play** button at the bottom of the MMF Player inspector (or calling `PlayFeedbacks()`) makes the cube scale up and back down within 0.2 s.

## Workflows

### Workflow: Build a multi-feedback player

**Goal:** one player that does several things at once (e.g. hit reaction: particles, sound, flicker, freeze frame).

**Steps:**
1. Add an `MMF_Player` to an empty GameObject, usually a child of the object it reacts for, named after the event (`HitFeedback`, `JumpFeedback`).
2. Add feedbacks with **Add new feedback**. Common picks (class in brackets, full list in [references/feedback-catalog.md](references/feedback-catalog.md)):
   - `Particles > Particles Play` (`MMF_Particles`): set **Bound Particle System**. `Mode` defaults to `Play`.
   - `Particles > Particles Instantiation` (`MMF_ParticlesInstantiation`): spawns a particle prefab.
   - `Audio > MMSoundManager Sound` (`MMF_MMSoundManagerSound`): set **Sfx**; see `feel-sound-manager`.
   - `Renderer > Flicker` (`MMF_Flicker`): set **Bound Renderer**; defaults `FlickerDuration = 0.2`, `FlickerPeriod = 0.04`, `Mode = Color`.
   - `Time > Freeze Frame` (`MMF_FreezeFrame`): `FreezeFrameDuration = 0.1` by default. It changes the time scale, so the scene needs an `MMTimeManager`; the feedback's **Automatic Setup** button adds one.
   - `Transform > Position` / `Rotation` / `Scale` / `Squash and Stretch` / `Wiggle`.
3. Expand each feedback and fix every warning icon. A warning means a required target is unassigned (`EvaluateRequiresSetup()` returned true); the help box says which field.
4. Rename feedbacks by double-clicking their title, or by setting `Label`, so that code can find them by label later.

**Expected result:** every feedback header shows no warning icon; in Play mode the green **Play** button plays all feedbacks at once.

### Workflow: Let feedbacks find their targets automatically

**Goal:** have feedbacks grab their own target(s) at initialization instead of assigning them by hand. Useful on prefabs, and to hit many renderers, animators or lights at once (for example flicker every renderer of a character).

**Steps:**
1. Expand the feedback's **Feedback Settings** group. Feedbacks that support it show **Automated Target Acquisition** (main target). Set its `Mode` (default `None`): `Self` (the MMF Player's GameObject), `AnyChild`, `ChildAtIndex`, `Parent`, or a reference holder mode (`FirstReferenceHolder`, `PreviousReferenceHolder`, `ClosestReferenceHolder`, `NextReferenceHolder`, `LastReferenceHolder`). A non-`None` mode also clears the missing-target warning icon.
2. Feedbacks with a list of extra targets also show **Automated Multiple Target Acquisition**, which fills that list with every matching component it finds. Supported feedbacks and the list they fill: `Renderer > Flicker` (`ExtraBoundRenderers`), `Renderer > Material Set Property` (`ExtraTargetRenderers`, each added with the feedback's `MaterialID`), `Renderer > MMBlink` (`ExtraTargetBlinks`), `Renderer > ShaderController` (`TargetShaderControllerList`), `Lights > Light` (`ExtraLights`), `Animation > Animation Parameter` / `Animator Play State` / `Animation Crossfade` (`ExtraBoundAnimators`), `GameObject > FloatController` (`ExtraTargetFloatControllers`), `Physics > Rigidbody` / `Rigidbody2D` (`ExtraTargetRigidbodies` / `ExtraTargetRigidbodies2D`), `Physics > Explosion Force` / `Explosion Force 2D` (`ExtraTargetRigidbodies` / `ExtraTargetRigidbodies2D`, used when `TargetMode` is `Rigidbodies`), `GameObject > Set Active` / `Destroy` (`ExtraTargetGameObjects`).
3. Set its fields:
   - `Mode` (default `None`): `Self`, `Children` (all descendants of the MMF Player's GameObject, not the GameObject itself), `SelfAndChildren`, `ChildAtIndex`, `Parents`, `AllReferenceHolders` (every `MMF Reference Holder` in the player), or one of the five single reference holder modes.
   - `ChildIndex` (default 0, `ChildAtIndex` only).
   - `IncludeChildren` (default `true`, `ChildAtIndex` and reference holder modes only): also search the children of that object.
   - `IncludeInactive` (default `false`): also pick targets on inactive GameObjects when searching children or parents.
   - `ListMode`: `Append` (default) adds found targets after the ones already in the list and skips duplicates; `Replace` clears the list first.
   The main target is never added to the extra list, so combining both acquisitions (for example main = `AnyChild`, extras = `Children`) does not double up. These feedbacks still need their main target to play, so set **Automated Target Acquisition** too (or assign the main target by hand).
4. Reference holders: add `Feedbacks > MMF Reference Holder` to the player and set its `GameObjectReference`. Feedbacks set to a reference holder mode use that object. With `ForceReferenceOnAll` checked, the holder becomes the target source of every compatible feedback in the player. Their extra lists are then filled from the holder's object too, but only for feedbacks whose multiple acquisition `Mode` is not `None`.
5. From code, set the modes before the player initializes, or call `player.Initialization()` afterwards. Assign new settings objects: on a feedback created with `AddFeedback`, both fields start out null.

```csharp
using MoreMountains.Feedbacks;

MMF_Flicker flicker = (MMF_Flicker)player.AddFeedback(typeof(MMF_Flicker));
flicker.AutomatedTargetAcquisition = new MMFeedbackTargetAcquisition { Mode = MMFeedbackTargetAcquisition.Modes.AnyChild };
flicker.AutomatedMultipleTargetAcquisition = new MMFeedbackMultipleTargetAcquisition
{
    Mode = MMFeedbackMultipleTargetAcquisition.Modes.Children,
    ListMode = MMFeedbackMultipleTargetAcquisition.ListModes.Replace
};
player.Initialization();
```

**Expected result:** after initialization (Play mode, or `player.Initialization()`), the main target field and the extra target list contain the found objects, and playing the player affects all of them.

### Workflow: Trigger a player from code, UnityEvents, animation events or Timeline

**Goal:** play the player at the right gameplay moment.

**Steps:**
1. **Code:** declare `public MMF_Player MyFeedback;`, assign it, then call one of:
   - `MyFeedback.PlayFeedbacks();`
   - `MyFeedback.PlayFeedbacks(transform.position, 1f);` passes a position (used by position-aware feedbacks such as particle instantiation) and an intensity multiplier (1 = normal, 0.5 = half).
   - `MyFeedback.StopFeedbacks();` stops the sequence and the feedbacks currently playing; `StopFeedbacks(false)` stops the sequence but lets running feedbacks finish.
   - `MyFeedback.SkipToTheEnd();` (the player must be playing) and `MyFeedback.RestoreInitialValues();`.
2. **UnityEvent** (Button `OnClick`, etc.): add an entry, drag the MMF Player object in, choose `MMF_Player > PlayFeedbacks ()`.
3. **Animation event:** put the MMF Player on the same GameObject as the `Animator`, add an animation event at the desired frame, and pick the function `PlayFeedbacks()`.
4. **Timeline:** add a Signal Emitter on the marker track, create a Signal asset, add a **Signal Receiver** to the director object, and set the reaction to the MMF Player's `PlayFeedbacks()`.
5. **Automatic:** in the player's Settings, enable `AutoPlayOnStart` or `AutoPlayOnEnable`.

**Expected result:** entering Play mode and performing the action plays the feedbacks. To check from code, read `player.IsPlaying` right after calling `PlayFeedbacks()`; it is `true` while feedbacks run.

### Workflow: Sequence feedbacks with pauses, loops or sequential mode

**Goal:** control order and timing.

**Steps:**
1. By default every feedback starts at the same time (`PlayMode = Parallel`). Each feedback can still be offset with its **Timing > Initial Delay**.
2. For strict one-after-another playback, set the player's `PlayMode` to `Sequential`; `SequentialDelay` (default 0) adds a gap after each feedback completes.
3. Insert pauses (`Pause` category):
   - `Pause > Pause` (`MMF_Pause`, `PauseDuration = 1` by default): everything above it starts together, everything below starts after the pause duration.
   - `Pause > Holding Pause` (`MMF_HoldingPause`): waits until every feedback above it has finished, then waits its own duration.
   - Script-driven pause: on `MMF_Pause`, enable `ScriptDriven`; the sequence then waits until code calls `player.ResumeFeedbacks()`. Optionally set `AutoResume` and `AutoResumeAfter` (default 0.25 s).
4. Insert loops (`Loop` category): `Loop > Looper` (`MMF_Looper`) jumps back to the last pause (`LoopAtLastPause`), the last `Loop > Looper Start` (`LoopAtLastLoopStart`), or the top. `NumberOfLoops` defaults to 2; `InfiniteLoop` repeats until stopped.
5. Per-feedback repeats: **Timing > Number Of Repeats**, **Repeat Forever**, **Delay Between Repeats** (default 1 s).
6. Reverse playback: set `Direction` to `BottomToTop` in Settings, or in code `player.Direction = MMFeedbacks.Directions.BottomToTop;`, or call `player.PlayFeedbacksInReverse()`. `ChangeDirection()` flips it.

**Expected result:** the player's displayed total duration matches the intended timeline; in Play mode feedbacks start in the configured order.

### Workflow: Change feedback values at runtime

**Goal:** adjust a feedback from code (difficulty, damage, settings) before playing it.

**Steps:**
1. Get the feedback:

```csharp
using System.Collections.Generic;
using MoreMountains.Feedbacks;

MMF_Scale scale = player.GetFeedbackOfType<MMF_Scale>();               // first match
MMF_Scale named = player.GetFeedbackOfType<MMF_Scale>("Pop");          // first match with Label "Pop"
List<MMF_Scale> all = player.GetFeedbacksOfType<MMF_Scale>();          // all matches
```

2. Change fields like any public field: `scale.AnimateScaleDuration = 0.5f;`.
3. If you changed timing or duration values, call `player.ComputeCachedTotalDuration();` before playing.
4. Many feedbacks cache data at initialization. If a change does not take effect, call `player.Initialization();` after changing values and before `PlayFeedbacks()`.
5. To vary strength without editing fields, pass an intensity: `player.PlayFeedbacks(position, 0.5f)`, or set `player.FeedbacksIntensity`.
6. Play a single feedback only: `scale.Play(transform.position, 1f);`.

**Expected result:** the next play uses the new values.

### Workflow: Run code when a player completes

**Goal:** chain logic after the feedbacks (load a scene, enable input, play another player).

**Steps:** pick one:
1. Inspector: expand **Events** in the player settings, keep `TriggerUnityEvents` on (default `true`), and bind a method to **OnComplete**.
2. Code with UnityEvents:

```csharp
private void OnEnable()  { MyPlayer.Events.OnComplete.AddListener(OnPlayerComplete); }
private void OnDisable() { MyPlayer.Events.OnComplete.RemoveListener(OnPlayerComplete); }
```

3. Global events: set `Events.TriggerMMFeedbacksEvents = true` on the player (default `false`), then register `MMFeedbacksEvent.Register(OnMMFeedbacksEvent)` with the signature `void OnMMFeedbacksEvent(MMFeedbacks source, MMFeedbacksEvent.EventTypes type)` and react to `EventTypes.Complete`. Unregister in `OnDisable`.
4. Async: `await FirstPlayer.PlayFeedbacksTask(transform.position);` then continue.

**Expected result:** the callback fires once per completed play, after every feedback in the player has finished playing.

### Workflow: Control players without a direct reference (channels)

**Goal:** play every player listening on a channel from anywhere.

**Steps:**
1. On each target player, under **Settings > Play Settings**, set `MMF_ChannelMode` (`Int` by default) and `MMF_Channel` (default 0), or `MMChannel` mode with an `MMF_ChannelDefinition` asset (create one with `Assets > Create > MoreMountains > MMChannel`).
2. Broadcast:

```csharp
using MoreMountains.Feedbacks;

MMChannelData channel = new MMChannelData(MMChannelModes.Int, 12, null);
MMF_PlayerEvent.Trigger(channel, true, transform.position, MMF_PlayerEvent.Modes.PlayFeedbacks);
```

Other modes include `StopFeedbacks`, `PauseFeedbacks`, `ResumeFeedbacks`, `ChangeDirection`, `RestoreInitialValues`, `SkipToTheEnd`. Players only listen in Play mode (they register in `Awake` when `Application.isPlaying`).

**Expected result:** every player on channel 12 plays.

### Workflow: Create a player entirely at runtime

**Goal:** build a player from code, e.g. one scale bump per child.

**Steps:**

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class DynamicPlayerCreator : MonoBehaviour
{
    public void Create()
    {
        MMF_Player newPlayer = gameObject.AddComponent<MMF_Player>();
        // initialize manually, otherwise Start-time initialization would run after our Play
        newPlayer.InitializationMode = MMFeedbacks.InitializationModes.Script;
        for (int i = 0; i < transform.childCount; i++)
        {
            MMF_Scale scale = (MMF_Scale)newPlayer.AddFeedback(typeof(MMF_Scale));
            scale.Label = "Scale child " + i;
            scale.AnimateScaleTarget = transform.GetChild(i);
            MMF_Pause pause = (MMF_Pause)newPlayer.AddFeedback(typeof(MMF_Pause));
            pause.PauseDuration = 0.2f;
        }
        newPlayer.Initialization();
        newPlayer.PlayFeedbacks();
    }
}
```

Remove a feedback with `player.RemoveFeedback(index)`.

**Expected result:** children bump one after another, 0.2 s apart.

### Workflow: Let players disable feedbacks (settings, accessibility)

**Goal:** turn off categories of feedback globally.

**Steps:**
1. One type, everywhere: every feedback class has a static switch, for example `MMF_Scale.FeedbackTypeAuthorized = false;`.
2. Everything: `MMFeedbacks.GlobalMMFeedbacksActive = false;`.
3. No-code option: add the `MMFeedbacksAuthorizations` component to an empty GameObject and untick the feedback types that must not play.
4. One player: disable its GameObject or component, or set `player.CanPlay = false`.
5. One feedback: untick the checkbox next to its name (sets `Active = false`).

**Expected result:** the disabled feedbacks no longer play; the others are unaffected.

## Verification

- The target GameObject has exactly one `MMF_Player` (the component has `[DisallowMultipleComponent]`).
- `player.FeedbacksList.Count` equals the number of feedbacks you meant to add, and each has the expected type (`player.GetFeedbackOfType<MMF_Scale>() != null`).
- No feedback reports missing setup: in an editor script, `player.FeedbacksList.TrueForAll(f => !f.EvaluateRequiresSetup())`.
- Targets are assigned (for example `scale.AnimateScaleTarget != null`).
- In Play mode, after `PlayFeedbacks()`, `player.IsPlaying` is `true` and `player.TimesPlayed` increments; `player.TotalDuration` is greater than 0 for time-based feedbacks.
- The scene was marked dirty and saved (`EditorSceneManager.SaveOpenScenes()`) if the change should persist.
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MMF_Player` | Component | Holds `FeedbacksList` and plays it. |
| `MMF_Player.PlayFeedbacks()` / `PlayFeedbacks(Vector3 position, float intensity)` | Method | Plays the sequence. |
| `MMF_Player.StopFeedbacks(bool stopAllFeedbacks = true)` | Method | Stops the sequence (and running feedbacks if true). |
| `MMF_Player.PauseFeedbacks()` / `ResumeFeedbacks()` | Methods | Pause/resume a sequence containing pauses or loops. |
| `MMF_Player.Initialization()` | Method | (Re)initializes the player and all feedbacks. |
| `MMF_Player.AddFeedback(System.Type)` | Method | Adds a feedback by type and returns it. |
| `MMF_Player.RemoveFeedback(int index)` | Method | Removes the feedback at an index. |
| `MMF_Player.GetFeedbackOfType<T>(string label)` / `GetFeedbacksOfType<T>()` | Methods | Finds feedbacks to modify. |
| `MMF_Player.ComputeCachedTotalDuration()` | Method | Refreshes `TotalDuration` after timing changes. |
| `MMF_Player.PlayFeedbacksTask(Vector3, float)` | Async method | Awaitable play. |
| `MMF_Player.Events.OnComplete` | UnityEvent | Fires when the sequence completes. |
| `MMF_PlayerEvent.Trigger(MMChannelData, bool, Vector3, Modes)` | Static event | Commands players on a channel. |
| `MMFeedbacks.GlobalMMFeedbacksActive` | Static bool | Global kill switch. |
| `MMF_Feedback.Timing` | `MMFeedbackTiming` | Initial delay, cooldown, repeats, direction, intensity interval. |
| `MMF_Feedback.AutomatedTargetAcquisition` | `MMFeedbackTargetAcquisition` | Finds the main target at initialization (`Mode`, `ChildIndex`). |
| `MMF_Feedback.AutomatedMultipleTargetAcquisition` | `MMFeedbackMultipleTargetAcquisition` | Fills the extra target list at initialization (`Mode`, `ChildIndex`, `IncludeChildren`, `IncludeInactive`, `ListMode`). |
| `MMF_Feedback.ForceAutomateTargetAcquisition()` | Method | Runs both acquisitions again outside initialization. |

Read [references/feedback-catalog.md](references/feedback-catalog.md) when you need the class name or menu path of a specific feedback, or to check which package a feedback requires.

## Common issues

- **Nothing happens when PlayFeedbacks is called** → the reference is null, the player or its GameObject is disabled, `CanPlay` is false, or a cooldown is active → check the reference, enable the object, check `CooldownDuration` on the player and on each feedback's Timing.
- **Feedback header shows a warning icon** → a required target is missing → expand the feedback and assign the field named in the help box.
- **Runtime value changes are ignored** → the feedback cached values at initialization → call `player.Initialization()` after the change; call `ComputeCachedTotalDuration()` for timing changes.
- **A player created from code plays nothing** → it initialized at `Start`, after you called Play → set `InitializationMode = MMFeedbacks.InitializationModes.Script` and call `Initialization()` yourself before `PlayFeedbacks()`.
- **The feedback cannot be retriggered while running** → `CanPlayWhileAlreadyPlaying` is false on the player, or the feedback's `AllowAdditivePlays` is false → enable the relevant option.
- **Copied feedbacks behave strangely** → Unity's "Copy Component Values" was used → use the player's **Copy All** / **Paste** buttons (or a Unity Preset of the MMF Player), which are the supported ways to copy feedback lists.
- **Changes made in Play mode are lost** → normal Unity behaviour → press **Keep Playmode Changes** on the player before exiting Play mode.
- **OnComplete UnityEvent never fires** → `Events.TriggerUnityEvents` is off; for `MMFeedbacksEvent` listeners, `Events.TriggerMMFeedbacksEvents` must be on.
- **Automated Multiple Target Acquisition is not in the inspector** → that feedback has no list of extra targets → use one of the feedbacks listed in "Let feedbacks find their targets automatically", or one player per target.
- **Automatic targets are not found** → acquisition only runs at initialization, `Mode` is `None`, or `IncludeInactive` is off while the targets are inactive → set the mode, then enter Play mode or call `player.Initialization()`; check `IncludeInactive`.
- **SkipToTheEnd does nothing** → it only works while the player is playing, and can take up to 3 frames to complete.
- **Feedback is missing from Add new feedback** → the package it depends on is not installed, so its define is absent → install the package named in the **Requires** column of [references/feedback-catalog.md](references/feedback-catalog.md) (ask first; `Tools > More Mountains > MMFeedbacks > Install All Dependencies` installs Cinemachine, Post Processing and 2D Animation when the Feel demos are present). The define is added automatically after recompilation.

## Boundaries

- `MMF_Player` sequences effects; it is not a general-purpose tween engine. For a one-off value interpolation without a player, use `MoreMountains.Tools.MMTween` (for example `MMTween.MoveTransform(...)`), or Unity's own animation tools.
- Edit-mode preview (`Tools > More Mountains > MMFeedbacks > Enable MMF Player Preview`) is experimental: only some feedbacks support it, and it changes the scene for real. Prefer testing in Play mode.
- Feedbacks that broadcast to shakers (camera shake, post-processing, many audio/camera feedbacks) do nothing without the matching shaker in the scene; use `feel-screen-shake` or `feel-post-processing-effects` to set those up.
- Feel only drives existing Unity systems and adds no cost of its own beyond them: heavy feedbacks (lots of particles, post-processing on mobile) cost what those systems cost.
