---
name: feel-springs
description: "Use this skill whenever the user wants bouncy, springy, wobbly or elastic motion or values using Feel's springs — e.g. 'make the button bounce when clicked', 'add squash and stretch to my character', 'springy scale on hover', 'follow the mouse with a spring', 'wobble the camera FOV', 'spring the light intensity', 'bouncy UI', 'jelly effect', 'bump this every time I hit it'. Covers Feel's 70+ spring components (MMSpringScale, MMSpringPosition, MMSpringSquashAndStretch, MMSpringCameraFieldOfView...), MoveTo/Bump commands, driving springs from spring feedbacks, events or code, clamping, custom spring components and the raw MMSpringFloat/Vector/Color API. Do NOT use for fixed-duration curve tweens (see feel-add-feedbacks), camera shake (see feel-screen-shake) or one-shot post-processing pulses (see feel-post-processing-effects). When in doubt whether a bouncy-motion request could involve Feel, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Add Spring-Based Motion with Feel

A Feel spring drives a value (float, Vector2/3/4 or Color) towards a target with a damped oscillation instead of a fixed-duration curve. You either **MoveTo** a new value or **Bump** the value by an amount, and the spring's **Damping** and **Frequency** settle it naturally. Repeated bumps stack without fighting each other, and the motion is framerate independent. This skill lets an agent add ready-made spring components to objects, drive them from MMF Players, UnityEvents, events or code, clamp them, write new spring components, and use the raw spring types in scripts.

## When to use this skill

- "make the button squash and bounce when I click it"
- "add squash and stretch to the player when it lands"
- "bouncy scale when I hover a card"
- "make the UI panel spring into place"
- "make this image follow the mouse with a springy lag"
- "pulse the light intensity with a spring every time I shoot"
- "FOV kick that wobbles back"
- "my spring keeps going below the floor" (clamping)
- "spring a property that Feel doesn't have a spring for" (custom spring)

Not for:
- Fixed-duration tweens along a curve (Position, Scale, Rotation feedbacks) → `feel-add-feedbacks`
- Camera shake → `feel-screen-shake`
- One-shot post-processing pulses → `feel-post-processing-effects`

## Prerequisites

- Feel 6.1, Unity 6000.5+, any render pipeline.
- **Install check:** `System.Type.GetType("MoreMountains.Feedbacks.MMSpringScale, MoreMountains.Tools")` is not null (the same assembly holds `MMF_Player`). If null, tell the user Feel is not installed (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) and stop.
- Volume, TextMesh Pro and UI springs need their packages (`MM_URP`, `MM_HDRP`, `MM_POSTPROCESSING`, `MM_UGUI2`, `MM_UI` defines). See [references/spring-components.md](references/spring-components.md).
- Spring code needs `using MoreMountains.Feedbacks;` (spring classes live in that namespace; `MMChannelData` too).

## Quick start

Bouncy squash-and-stretch on a button press:

1. In the scene, select the object to animate (for example a `Cube`) and add `Add Component > More Mountains > Springs > MM Spring Squash And Stretch` (`MMSpringSquashAndStretch`). Leave **Target** empty; it uses the object's own `Transform`.
2. Create a UI Button (`GameObject > UI (Canvas) > Button - TextMeshPro`, or any existing button). In its **On Click ()** list press **+**, drag the cube in, and choose `MMSpringSquashAndStretch > BumpRandom ()`.
3. Editor-script equivalent:

```csharp
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FeelSpringQuickStart
{
    public static void AddSquashOnClick(GameObject target, Button button)
    {
        MMSpringSquashAndStretch spring = Undo.AddComponent<MMSpringSquashAndStretch>(target);
        Undo.RecordObject(button, "Bind spring");
        UnityEventTools.AddPersistentListener(button.onClick, spring.BumpRandom);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(target.scene);
    }
}
```

4. Enter Play mode and click the button repeatedly. While still in Play mode, tweak **Spring Settings > Damping** (0.4 by default, 0 = loose, 1 = stiff), **Frequency** (6 oscillations per second by default) and **Randomness > Bump Amount Random Value** (20 to 100 by default) to taste.
5. Expected result: the cube has an `MMSpringSquashAndStretch`; the button's `onClick` has one persistent listener targeting `BumpRandom`; every click squashes and stretches the cube, and rapid clicks stack.

## Workflows

### Workflow: Pick and add the right spring component

**Goal:** spring a specific property.

**Steps:**
1. Find the component in [references/spring-components.md](references/spring-components.md). Common picks, all under `Add Component > More Mountains > Springs`:
   - Transform: `MM Spring Position` (Vector3), `MM Spring Rotation` (Vector3), `MM Spring Scale` (Vector3), `MM Spring Squash And Stretch` (Float), `MM Spring Rotation Around` (Float).
   - Camera: `MM Spring Camera Field Of View`, `MM Spring Camera Orthographic Size` (Float).
   - Light: `MM Spring Light Intensity`, `MM Spring Light Range` (Float), `MM Spring Light Color` (Color).
   - UI: `MM Spring Image Alpha`, `MM Spring Image Fill Amount` (Float), `MM Spring Image Color` (Color), `MM Spring Rect Transform Position` (Vector3), `MM Spring Rect Transform Size Delta` (Vector2).
   - Audio: `MM Spring Audio Source Volume`, `MM Spring Audio Source Pitch` (Float).
   - Volumes: `MM Spring Vignette Intensity URP`, `MM Spring Bloom Intensity URP`, and the HDRP / Post Processing v2 equivalents.
2. Add it to the GameObject that holds the target component, or assign **Target** explicitly.
3. Inspector sections: **Target**, **Channel and TimeScale** (`Channel` 0, `TimeScaleMode` Scaled by default), **Spring Settings**, **Randomness**, **Test** (MoveTo / Bump buttons in Play mode).
4. For vector springs, the spring is unified by default (one damping/frequency for all axes); tick **Separate Axis** to tune and clamp each axis.

**Expected result:** in Play mode, the **Test** group's Bump button visibly moves the property and it settles back.

### Workflow: Drive a spring from code

**Goal:** bump or move a spring from gameplay.

**Steps:**

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class LightPulse : MonoBehaviour
{
    public MMSpringLightIntensity Spring;

    public void OnShoot()
    {
        Spring.Bump(25f);          // push the value; it springs back to its target
    }

    public void PowerUp()
    {
        Spring.MoveTo(3f);         // spring towards a new resting value
    }
}
```

Available commands on every spring component: `MoveTo(v)`, `MoveToAdditive(v)`, `MoveToSubtractive(v)`, `MoveToRandom()`, `MoveToRandom(min, max)` (float springs), `MoveToInstant(v)`, `Bump(amount)`, `BumpRandom()`, `BumpRandom(min, max)` (float springs), `Stop()`, `Finish()`, `RestoreInitialValue()`, `ResetInitialValue()`.

Bump amounts interact with the settings: a loose spring (low damping) travels far with a small bump; a stiff one needs a large bump. Start from the component's default test values (`TestBumpAmount = 75`, `TestMoveToValue = 2` on float springs) and adjust.

**Expected result:** calling the method changes the value with a springy motion.

### Workflow: Drive a spring from an MMF Player

**Goal:** fire a spring as part of a feedback sequence.

**Steps:**
1. Note the spring's value type (shown at the top of its **Spring Settings** section, also in the reference table).
2. In an MMF Player, add the matching feedback: `Springs > Spring Float` (`MMF_SpringFloat`), `Spring Vector2`, `Spring Vector3`, `Spring Vector4` or `Spring Color`.
3. Either drag the spring component into **Target Spring**, or leave it empty and use matching **Channel** settings to reach every spring of that type on the channel.
4. Choose the **Command** (`Bump` by default) and its value (`BumpAmount = 75`, `MoveToValue = 2` by default on Spring Float). Optional `OverrideDamping` / `OverrideFrequency` change the spring's settings for this command.
5. Example from the docs: add `MMSpringCameraFieldOfView` to the main camera, then a `Spring Float` feedback targeting it with `BumpAmount = 1000` for a strong FOV kick.

Built-in transform spring feedbacks also exist and need no component: `Transform > Position Spring` (`MMF_PositionSpring`), `Transform > Scale Spring` (`MMF_ScaleSpring`), `Transform > Squash and Stretch Spring` (`MMF_SquashAndStretchSpring`). Each defaults to `Mode = Bump` with damping 0.4 / frequency 6.

**Expected result:** playing the MMF Player bumps or moves the spring.

### Workflow: Drive springs with events (no reference)

**Goal:** control every spring of a type on a channel from anywhere.

**Steps:**

```csharp
using MoreMountains.Feedbacks;

// all float springs listening on channel 3 move to 2
MMChannelData channel = new MMChannelData(MMChannelModes.Int, 3, null);
MMSpringFloatEvent.Trigger(SpringCommands.MoveTo, null, channel, moveToValue: 2f);
// bump them by 50
MMSpringFloatEvent.Trigger(SpringCommands.Bump, null, channel, bumpAmount: 50f);
```

Set the springs' **Channel** to 3 first. Vector3 and Color springs have matching events (`MMSpringVector3Event`, `MMSpringColorEvent`, ...).

**Expected result:** every float spring on channel 3 reacts.

### Workflow: Clamp a spring

**Goal:** keep the value inside bounds (e.g. a bouncing character must not sink into the floor).

**Steps:**
1. On a float spring, or on each axis of a vector spring with **Separate Axis** ticked, open the clamp settings (`MMSpringClampSettings`).
2. Enable `ClampMin` and/or `ClampMax` and set `ClampMinValue` / `ClampMaxValue` (0 and 10 by default), or tick `ClampMinInitial` / `ClampMaxInitial` to use the spring's initial value as the bound.
3. Tick `ClampMinBounce` / `ClampMaxBounce` to bounce off the bound instead of stopping at it.

**Expected result:** the value never crosses the clamp; with bounce enabled, it rebounds.

### Workflow: Create a spring component for any property

**Goal:** spring a value Feel has no component for (here `Rigidbody2D.mass`).

**Steps:**
1. Create a script outside the `Feel` folder (so updates don't overwrite it):

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class Rigidbody2DMassSpring : MMSpringFloatComponent<Rigidbody2D>
{
    public override float TargetFloat
    {
        get => Target.mass;
        set => Target.mass = value;
    }
}
```

2. For other value types, inherit `MMSpringVector2Component<T>` (override `TargetVector2`), `MMSpringVector3Component<T>` (`TargetVector3`), `MMSpringVector4Component<T>` or `MMSpringColorComponent<T>`.
3. Add it to an object with the target component; it gets the full inspector, commands, channels and event support automatically.

**Expected result:** the new component compiles, appears in Add Component, and its Test buttons move the property.

### Workflow: Use the raw spring API in your own script

**Goal:** springy values inside custom code without components.

**Steps:**

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class FollowMouseSpring : MonoBehaviour
{
    public MMSpringVector2 Spring = new MMSpringVector2(); // shows damping/frequency/clamp in the inspector
    private RectTransform _rectTransform;
    private Vector2 _localPoint;

    private void Start() { _rectTransform = GetComponent<RectTransform>(); }

    private void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform.parent as RectTransform,
            Input.mousePosition, null, out _localPoint);
        Spring.MoveTo(_localPoint);
        Spring.UpdateSpringValue(Time.deltaTime); // you must tick raw springs yourself
        _rectTransform.anchoredPosition = Spring.CurrentValue;
    }
}
```

Types: `MMSpringFloat`, `MMSpringVector2`, `MMSpringVector3`, `MMSpringVector4`, `MMSpringColor`. (The example reads `Input.mousePosition`, so it needs the legacy Input Manager or **Active Input Handling = Both**.)

**Expected result:** the UI element trails the mouse with a springy lag.

## Verification

- The object has the intended spring component: `target.GetComponent<MoreMountains.Feedbacks.MMSpringSquashAndStretch>() != null` (or the chosen type).
- Spring settings: `spring.FloatSpring.Damping` / `.Frequency` (float components), `spring.SpringVector3` (Vector3 components) or `spring.ColorSpring` (color components) hold the intended values.
- UnityEvent wiring: `button.onClick.GetPersistentEventCount() > 0` and `GetPersistentMethodName(0) == "BumpRandom"` (or the chosen command).
- Feedback wiring: the MMF Player has `MMF_SpringFloat` (or the matching type) with `TargetSpring` assigned, or with a channel equal to the spring's channel.
- In Play mode, after a `Bump`, the target property (for example `transform.localScale`) differs from its initial value, then settles back to it (within a couple of seconds with default settings).
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MMSpringFloatComponent<T>` / `MMSpringVector2/3/4Component<T>` / `MMSpringColorComponent<T>` | Abstract components | Base classes of all spring components (`Target`, channel, settings, commands). |
| `spring.MoveTo(v)` / `Bump(a)` / `BumpRandom()` / `MoveToInstant(v)` | Methods | Main spring commands. |
| `spring.Stop()` / `Finish()` / `RestoreInitialValue()` / `ResetInitialValue()` | Methods | Halt, jump to target, restore or re-capture the initial value. |
| `FloatSpring` / `SpringVector2` / `SpringVector3` / `SpringVector4` / `ColorSpring` | Fields | The spring definition on a component (damping, frequency, clamps). |
| `MMSpringFloat.Damping` (0.4) / `Frequency` (6) | Fields | Spring tension and oscillations per second. |
| `MMF_SpringFloat` (`Springs/Spring Float`) and siblings | Feedbacks | Send a command to a spring from an MMF Player. |
| `MMSpringFloatEvent.Trigger(SpringCommands, MMSpringComponentBase, MMChannelData, ...)` | Static event | Command springs by channel. |
| `MMSpringFloat` / `MMSpringVector2` / ... `.UpdateSpringValue(dt)` / `.CurrentValue` | Raw API | Springs inside your own scripts. |
| `MMSpringComponentBase.OnEquilibriumReached` | UnityEvent | Fires when the spring comes to rest. |

Read [references/spring-components.md](references/spring-components.md) for every spring component, its value type, target and menu path.

## Common issues

- **Bump does nothing** → the bump amount is tiny for a stiff spring, or the spring has no target → raise the bump (try the default 75 or more), check **Target** or that the target component exists on the same object.
- **Spring Float feedback does nothing** → value type mismatch (e.g. a Vector3 spring driven by `Spring Float`) or channel mismatch → use the feedback of the spring's type and either assign **Target Spring** or align channels.
- **Value drifts to a new resting point after MoveTo** → that is how `MoveTo` works (it changes the target) → use `Bump` to return to rest, or `RestoreInitialValue()` to go back.
- **Object sinks through the floor while bouncing** → no clamp → enable `ClampMin` with `ClampMinInitial` (and `ClampMinBounce` for a rebound).
- **Spring ignores slow motion (or should ignore it)** → set **TimeScaleMode** to `Scaled` or `Unscaled` accordingly.
- **Raw spring never moves** → `UpdateSpringValue(deltaTime)` is not called every frame → call it in `Update`.
- **Volume spring compile errors or missing** → the matching render pipeline package isn't installed → use the variant for the project's pipeline.

## Boundaries

- Springs have no fixed duration; for effects that must end at an exact time, use curve-based feedbacks (`feel-add-feedbacks`), or set `DeclaredDuration` on spring feedbacks just to inform the player's total duration.
- One spring component controls one property; several springs on the same property fight each other. Combine motions through a single spring (bumps add up), or nest objects.
- Spring components animate in Play mode; they are not an edit-mode animation tool.
- Physics-accurate simulation (mass, collisions) is outside scope; springs here are a damped-oscillator interpolation for game feel.
