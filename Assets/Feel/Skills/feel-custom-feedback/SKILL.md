---
name: feel-custom-feedback
description: "Use this skill whenever the user wants to write C# that extends Feel with their own feedback type — e.g. 'make a custom feedback', 'create an MMF feedback that calls my own script', 'a feedback that changes my shader value / my component', 'extend the TMP count to feedback to add a suffix', 'write a new feedback for Feel', 'my custom feedback doesn't show in the add feedback menu', 'inherit from MMF_Feedback', 'make a shaker for my component'. Covers the MMF_Feedback template (FeedbackPath, CustomInitialization, CustomPlayFeedback, CustomStopFeedback, FeedbackDuration), coroutines via Owner, extending existing feedbacks, MMF_FeedbackBase value-over-time feedbacks, setup warnings, inspector groups, type toggles and the event + MMShaker pattern. Do NOT use for configuring existing feedbacks (see feel-add-feedbacks) or custom spring components (see feel-springs). When in doubt whether a request to extend Feel in code applies, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Write a Custom Feel Feedback

Every Feel feedback is a plain serializable C# class that derives from `MoreMountains.Feedbacks.MMF_Feedback`. It is not a MonoBehaviour: `MMF_Player` stores it with `[SerializeReference]` and calls its lifecycle methods. Any subclass with a `[FeedbackPath]` attribute automatically appears in the MMF Player's **Add new feedback** dropdown. This skill lets an agent write new feedbacks, extend existing ones, build value-over-time feedbacks quickly with `MMF_FeedbackBase`, and, for broadcast-style effects, write the matching event and `MMShaker`.

## When to use this skill

- "create a custom feedback that calls my `Health.Heal()`"
- "I want a feedback that animates my shader's `_Dissolve` value over time"
- "extend the TMP Count To feedback so it adds a $ suffix"
- "make a feedback that toggles my component on and off"
- "my new feedback class doesn't show up in Add new feedback"
- "my feedback needs a coroutine, how do I start one?"
- "make a shaker for my component so a feedback can drive it on a channel"

Not for:
- Configuring or triggering existing feedbacks → `feel-add-feedbacks`
- New spring components (`MMSpringFloatComponent<T>`) → `feel-springs`
- Feedback categories missing because a package isn't installed → the **Requires** column of [../feel-add-feedbacks/references/feedback-catalog.md](../feel-add-feedbacks/references/feedback-catalog.md)

## Prerequisites

- Feel 6.1, Unity 6000.5+, any render pipeline.
- **Install check:** `System.Type.GetType("MoreMountains.Feedbacks.MMF_Feedback, MoreMountains.Tools")` is not null. If null, tell the user Feel is not installed (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) and stop.
- **Where to put the file:** in the user's own folder (for example `Assets/Scripts/Feedbacks/`), never inside `Assets/Feel/`, so a Feel update does not delete or overwrite it.
- **Assemblies:** `MoreMountains.Tools` is auto-referenced, so scripts in `Assembly-CSharp` compile as-is. If the target folder has its own `.asmdef`, add `MoreMountains.Tools` to its references.
- Before writing a new feedback, check [../feel-add-feedbacks/references/feedback-catalog.md](../feel-add-feedbacks/references/feedback-catalog.md): an existing feedback (`GameObject > Property`, `Events > Unity Events`, `Renderer > ShaderController`, `Renderer > Material Set Property`) may already do the job.

## Quick start

A minimal feedback that calls a method on one of the user's components (here, a "heal" feedback):

1. Create `Assets/Scripts/Feedbacks/MMF_Heal.cs`:

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

[AddComponentMenu("")]
[System.Serializable]
[FeedbackHelp("Heals the target Health component by the specified amount when played.")]
[FeedbackPath("Gameplay/Heal")]
public partial class MMF_Heal : MMF_Feedback
{
    /// a static bool used to disable all feedbacks of this type at once
    [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
    public static bool FeedbackTypeAuthorized = true;

    #if UNITY_EDITOR
    public override Color FeedbackColor => MMFeedbacksInspectorColors.DebugColor;
    public override bool EvaluateRequiresSetup() => (TargetHealth == null);
    public override string RequiredTargetText => TargetHealth != null ? TargetHealth.name : "";
    public override string RequiresSetupText => "This feedback requires a TargetHealth to be set.";
    #endif

    // this feedback is instant
    public override float FeedbackDuration => 0f;

    [MMFInspectorGroup("Heal", true, 12, true)]
    [Tooltip("the Health component to heal")]
    public Health TargetHealth;
    [Tooltip("the amount of health to restore, multiplied by the player's intensity")]
    public float Amount = 10f;

    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
    {
        if (!Active || !FeedbackTypeAuthorized || (TargetHealth == null))
        {
            return;
        }
        float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
        TargetHealth.Heal(Amount * intensityMultiplier);
    }
}
```

(`Health` and `Heal(float)` stand for the user's own component and method; replace them with the real ones before compiling.)

2. Wait for compilation and check the Console for errors.
3. On any MMF Player, open **Add new feedback**: a `Gameplay` category now contains `Heal`. Add it and assign **Target Health**.
4. Expected result: the class compiles; `Gameplay/Heal` is listed (also in `Tools > More Mountains > MMFeedbacks > Output MMF_Feedbacks list`); the feedback header shows a warning icon until `TargetHealth` is set; in Play mode, playing the player calls `Heal`.

## Workflows

### Workflow: Write a feedback from the template

**Goal:** a new feedback with full lifecycle support.

**Steps:**
1. Required: derive from `MMF_Feedback`, add `[System.Serializable]` and `[FeedbackPath("Category/Name")]`. The path decides the dropdown entry; a class without it is skipped by the dropdown. `[AddComponentMenu("")]` keeps it out of Unity's Add Component menu.
2. Override what you need (all `protected`, signatures as in Feel 6.1):
   - `CustomInitialization(MMF_Player owner)`: cache references. Call `base.CustomInitialization(owner)`.
   - `CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)`: **required** (abstract). Start with `if (!Active || !FeedbackTypeAuthorized) return;`.
   - `CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)`: stop what play started.
   - `CustomSkipToTheEnd(...)`, `CustomRestoreInitialValues()`, `CustomReset()`, `CustomPlayerComplete()`: optional.
3. Override `FeedbackDuration` so the player can compute its total duration and holding pauses work. For a timed feedback, use `public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }`.
4. Respect the player context: use `ComputeIntensity(feedbacksIntensity, position)` for strength, `NormalPlayDirection` to support reverse play, and `FeedbackDeltaTime` / `FeedbackTime` instead of `Time.deltaTime` so scaled/unscaled settings and editor preview work.
5. Optional editor polish inside `#if UNITY_EDITOR`: `FeedbackColor` (colors in `MMFeedbacksInspectorColors`), `EvaluateRequiresSetup()`, `RequiresSetupText`, `RequiredTargetText`. Group fields with `[MMFInspectorGroup("Name", true, colorIndex, requiresSetup)]`: the last argument puts the warning icon on that group.
6. Add `[Tooltip]`s to public fields; they show in the inspector.
7. Give every static field, auto-property or event a statics cleanup attribute, or Unity's analyzer warns (UAL0010 / UAL0013). Feel uses `[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]`, which resets the member to its initializer on every Play mode transition, as a domain reload used to. It requires the class (and any containing class) to be `partial`, which is why the template is `public partial class`. Use `[NoAutoStaticsCleanup]` only when a value must survive exiting Play mode, or is set by a static constructor (auto cleanup does not re-run it). Feel requires Unity 6000.5+, so the attribute needs no `#if` guard.
8. Optional automatic targets: override `HasAutomatedTargetAcquisition => true` and `AutomateTargetAcquisition()` to set the main target with `FindAutomatedTarget<T>()` (or `FindAutomatedTargetGameObject()`). If the feedback also has a list of extra targets, override `HasAutomatedMultipleTargetAcquisition => true` and `AutomateMultipleTargetAcquisition()`, and fill the list with `FindAutomatedTargets<T>(list, mainTarget)` (or `FindAutomatedTargetGameObjects(list, mainTarget)`). These helpers return the list (created if null), honour the user's `ListMode`, skip duplicates and never add `mainTarget`. Both run before `CustomInitialization`, so size any per-target caches there. Example (`MMF_Flicker` works this way):

```csharp
public Renderer TargetRenderer;
public List<Renderer> ExtraTargetRenderers;

public override bool HasAutomatedTargetAcquisition => true;
protected override void AutomateTargetAcquisition() => TargetRenderer = FindAutomatedTarget<Renderer>();
public override bool HasAutomatedMultipleTargetAcquisition => true;
protected override void AutomateMultipleTargetAcquisition() => ExtraTargetRenderers = FindAutomatedTargets<Renderer>(ExtraTargetRenderers, TargetRenderer);
```

**Expected result:** the feedback compiles, is listed in **Add new feedback**, and plays.

### Workflow: Run something over time (coroutines)

**Goal:** animate across frames.

**Steps:**
1. Feedbacks aren't MonoBehaviours, so start coroutines on the host player: `Owner.StartCoroutine(MyRoutine())`, and keep the `Coroutine` handle to stop it in `CustomStopFeedback` (`Owner.StopCoroutine(_coroutine)`).
2. Inside the coroutine, advance time with `FeedbackDeltaTime` and set `IsPlaying = true/false` at start/end.

```csharp
protected Coroutine _coroutine;

protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
{
    if (!Active || !FeedbackTypeAuthorized) { return; }
    if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
    _coroutine = Owner.StartCoroutine(AnimateCo());
}

protected virtual System.Collections.IEnumerator AnimateCo()
{
    IsPlaying = true;
    float elapsed = 0f;
    while (elapsed < FeedbackDuration)
    {
        float t = elapsed / FeedbackDuration;
        // apply t to the target here
        elapsed += FeedbackDeltaTime;
        yield return null;
    }
    IsPlaying = false;
    _coroutine = null;
}

protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
{
    if (_coroutine != null) { Owner.StopCoroutine(_coroutine); _coroutine = null; }
    IsPlaying = false;
}
```

**Expected result:** the effect runs for `FeedbackDuration` and stops cleanly when the player stops.

### Workflow: Extend an existing feedback

**Goal:** change one behaviour of a built-in feedback without copying it (recommended over editing Feel's source, which updates would overwrite).

**Steps:**
1. Find the class (`MMF_` + name; see the catalog) and the `protected virtual` method to change.
2. Inherit and override. Example from the documentation, adding a suffix to TMP Count To (`MMF_TMPCountTo.UpdateText(float)` is `protected virtual`; TMP feedbacks require the `MM_UGUI2` define, i.e. uGUI 2.x):

```csharp
using MoreMountains.Feedbacks;

[System.Serializable] // not inherited either: required for the player's [SerializeReference] list
[FeedbackPath("TextMesh Pro/TMP Count To With Suffix")]
public class MMF_TMPCountToSuffix : MMF_TMPCountTo
{
    public string Suffix = "$";

    protected override void UpdateText(float currentValue)
    {
        base.UpdateText(currentValue);
        TargetTMPText.text = TargetTMPText.text + Suffix;
    }
}
```

3. Give the subclass its own `[FeedbackPath]`. The attribute is not inherited (Feel reads it with `GetCustomAttributes(false)`), so a subclass without one does not appear in **Add new feedback**.

**Expected result:** the new entry appears next to the original and behaves like it, plus your change.

### Workflow: Value-over-time feedback with MMF_FeedbackBase

**Goal:** tween any numeric/colour property quickly (here a CanvasGroup alpha, as Feel's own `MMF_CanvasGroup` does).

**Steps:**
1. Derive from `MMF_FeedbackBase` (abstract; provides `Mode` = `OverTime`/`Instant`/`ToDestination`, `Duration = 0.2`, `RelativeValues`, `AllowAdditivePlays`, `StartsOff`, `EndsOff`).
2. Implement `FillTargets()`: build an `MMF_FeedbackBaseTarget` whose `Target` is an `MMPropertyReceiver` pointing at the component and property name, then add it to `_targets`:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

[System.Serializable]
[FeedbackPath("UI/My CanvasGroup Alpha")]
public class MMF_MyCanvasGroupAlpha : MMF_FeedbackBase
{
    [MMFInspectorGroup("Canvas Group", true, 12, true)]
    public CanvasGroup TargetCanvasGroup;
    public MMTweenType AlphaCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
    public float RemapZero = 0f;
    public float RemapOne = 1f;
    public float InstantAlpha;

    protected override void FillTargets()
    {
        if (TargetCanvasGroup == null) { return; }
        MMF_FeedbackBaseTarget target = new MMF_FeedbackBaseTarget();
        MMPropertyReceiver receiver = new MMPropertyReceiver();
        receiver.TargetObject = TargetCanvasGroup.gameObject;
        receiver.TargetComponent = TargetCanvasGroup;
        receiver.TargetPropertyName = "alpha";
        receiver.RelativeValue = RelativeValues;
        target.Target = receiver;
        target.LevelCurve = AlphaCurve;
        target.RemapLevelZero = RemapZero;
        target.RemapLevelOne = RemapOne;
        target.InstantLevel = InstantAlpha;
        _targets.Add(target);
    }
}
```

3. Use the real source as reference: `Assets/Feel/MMFeedbacks/MMFeedbacksForThirdParty/MMTools/Feedbacks/MMF_CanvasGroup.cs` (it also shows `ToDestination` support and automated target acquisition).

**Expected result:** the feedback animates the property along the curve over `Duration`.

### Workflow: Broadcast to receivers (event + shaker)

**Goal:** a feedback that drives any number of receivers on a channel, like Feel's post-processing and camera feedbacks.

**Steps:**
1. Write a `partial` event struct with a static `Delegate`, `Register`, `Unregister` and `Trigger`. Mark its static event `[AutoStaticsCleanup]`, so its listeners are cleared on every Play mode transition when domain reload is disabled, as Feel's own events do. No `SubsystemRegistration` reset method is needed.
2. Write a shaker deriving from `MoreMountains.Feedbacks.MMShaker`: override `Initialization()`, `Shake()` (use `ShakeFloat(curve, remapMin, remapMax, relative, initial)`), `GrabInitialValues()`, `ResetTargetValues()`, `ResetShakerValues()`, and `StartListening()` / `StopListening()` to register the event handler. The handler checks `CheckEventAllowed(channelData)` then calls `Play()`. `MMShaker` already calls `StartListening()` from `OnEnable`, which matters because static events are reset on domain reload.
3. Write the feedback: `HasChannel => true`, and in `CustomPlayFeedback` call `YourEvent.Trigger(..., ChannelData, ...)`.
4. Read [references/shaker-pattern.md](references/shaker-pattern.md) for a complete, compilable three-part example modelled on Feel's `MMAudioSourceVolumeShaker`.

**Expected result:** adding the shaker to an object and playing the feedback animates the value; channel mismatches are ignored.

## Verification

- The script compiles with no Console errors (`UnityEditor.Compilation.CompilationPipeline` finished, no red entries).
- The type resolves and is an `MMF_Feedback`: `typeof(MoreMountains.Feedbacks.MMF_Feedback).IsAssignableFrom(typeof(MMF_Heal))`.
- `FeedbackPathAttribute.GetFeedbackDefaultPath(typeof(MMF_Heal))` returns the intended path (`"Gameplay/Heal"`), and the path appears in the `Output MMF_Feedbacks list` Console output.
- Adding it with `player.AddFeedback(typeof(MMF_Heal))` returns a non-null instance, and `EvaluateRequiresSetup()` is true until targets are assigned, then false.
- In Play mode, `player.PlayFeedbacks()` produces the intended side effect, and `player.TotalDuration` reflects `FeedbackDuration`.
- The file lives outside `Assets/Feel/`.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MMF_Feedback` | Abstract class | Base of all feedbacks. |
| `[FeedbackPath("Category/Name")]` | Attribute | Required to appear in **Add new feedback**. |
| `[FeedbackHelp("...")]` | Attribute | Help text shown in the feedback inspector. |
| `[MMFInspectorGroup(name, groupAll, colorIndex, requiresSetup)]` | Attribute | Foldable inspector groups. |
| `CustomInitialization(MMF_Player owner)` / `CustomPlayFeedback(Vector3, float)` / `CustomStopFeedback(Vector3, float)` / `CustomRestoreInitialValues()` | Protected overrides | Lifecycle hooks. |
| `FeedbackDuration` | Virtual property | Duration used by the player for totals and holding pauses. |
| `Owner` | `MMF_Player` | Host player; use it for coroutines and context (`Owner.Context`). |
| `ComputeIntensity(float, Vector3)` / `FeedbackDeltaTime` / `NormalPlayDirection` / `ApplyTimeMultiplier(float)` | Helpers | Intensity, time and direction aware helpers. |
| `MMF_FeedbackBase` + `FillTargets()` | Abstract class | Value-over-time feedbacks via `MMPropertyReceiver`. |
| `HasAutomatedTargetAcquisition` + `AutomateTargetAcquisition()` / `FindAutomatedTarget<T>()` | Overrides / helper | Opt into automatic main target acquisition. |
| `HasAutomatedMultipleTargetAcquisition` + `AutomateMultipleTargetAcquisition()` / `FindAutomatedTargets<T>(list, exclude)` | Overrides / helper | Opt into automatic filling of an extra targets list. |
| `MMShaker` | Component base | Receiver side of broadcast feedbacks. |

## Common issues

- **The feedback is not in Add new feedback** → no `[FeedbackPath]` on the class itself (it is not inherited from a parent feedback), a compile error, or the class is abstract → add the attribute, fix errors, make the class concrete.
- **Fields reset or the feedback disappears after a domain reload** → the class isn't serializable, or was renamed/moved without `[MovedFrom]` → add `[System.Serializable]`; when renaming, add `[UnityEngine.Scripting.APIUpdating.MovedFrom(...)]` like Feel's own feedbacks so `[SerializeReference]` data still resolves.
- **`StartCoroutine` does not exist** → feedbacks aren't MonoBehaviours → use `Owner.StartCoroutine(...)`.
- **The player's total duration is 0 and holding pauses don't wait** → `FeedbackDuration` isn't overridden → return the real duration.
- **The feedback ignores timescale settings or freezes in editor preview** → it uses `Time.deltaTime` → use `FeedbackDeltaTime`.
- **Changes are lost after updating Feel** → the custom file was placed inside `Assets/Feel` (or a Feel file was edited) → keep custom feedbacks in your own folder and extend rather than modify.
- **CS0246 type or namespace not found for Feel types** → the script's assembly definition doesn't reference `MoreMountains.Tools` → add the reference.
- **Console warns UAL0010 / UAL0013 ("A type with static fields, automatic properties or events must have a ... AutoStaticsCleanup or NoAutoStaticsCleanup attribute")** → a static member has no statics cleanup attribute → put `[AutoStaticsCleanup]` (or `[NoAutoStaticsCleanup]`) on each static member, not on the class (a class-level attribute raises UAL0019). `[AutoStaticsCleanup]` needs the type and every containing type to be `partial` (UAL0012); use `new T[] { ... }` rather than a bare `{ ... }` array initializer, which Unity's generator copies into invalid code.
- **A static is null or false after entering or exiting Play mode** → it is `[AutoStaticsCleanup]` but was set by a static constructor or a load-time method, which auto cleanup doesn't re-run → give it a field initializer, or mark it `[NoAutoStaticsCleanup]` if its value never changes.
- **Shaker stops reacting after a script recompile** → the shaker doesn't re-register its event → register in `StartListening()` (called from `MMShaker.OnEnable`), not only in `Awake`.

## Boundaries

- Feedbacks run inside an `MMF_Player`; they are not standalone components. For a component-based effect, write a MonoBehaviour (or a spring component, see `feel-springs`) and drive it from a feedback.
- `MMF_FeedbackBase` drives properties through reflection-based `MMPropertyReceiver`; for very hot paths, a hand-written `CustomPlayFeedback` is cheaper.
- Edit-mode preview support depends on your feedback using Feel's timing helpers; it is not guaranteed for custom code.
- Don't modify Feel's own source files to customise behaviour; inherit instead, so updates remain painless.
