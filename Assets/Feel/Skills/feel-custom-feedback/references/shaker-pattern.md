# Event + shaker + feedback pattern (Feel 6.1)

Read this file when a custom feedback must drive receivers by channel (the way Feel's post-processing, camera and audio-source feedbacks work) instead of acting on one referenced target.

The pattern has three parts:

1. **Event struct**: a static, fire-and-forget delegate. `Trigger()` invokes every registered listener.
2. **Shaker** (`MMShaker` subclass): sits on the object to affect, registers for the event, checks the channel, and animates the value in its own `Update` loop over `ShakeDuration`.
3. **Feedback** (`MMF_Feedback` subclass): calls `Trigger()` with its settings and `ChannelData`.

The example below drives `LineRenderer.widthMultiplier`. It is modelled on Feel's `MMAudioSourceVolumeShaker` (`Assets/Feel/MMFeedbacks/MMFeedbacks/Shakers/MMAudioSourceVolumeShaker.cs`) and `MMF_AudioSourceVolume`; read those for the full reference implementation. Put each class in its own file named after the class, outside `Assets/Feel/`.

## 1. Event

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

// partial: required by [AutoStaticsCleanup], whose generated code clears the event's listeners
public partial struct LineWidthShakeEvent
{
    // static events survive play mode when domain reload is disabled: Unity clears them
    // automatically on every Play mode transition thanks to [AutoStaticsCleanup]
    [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
    static private event Delegate OnEvent;

    static public void Register(Delegate callback) { OnEvent += callback; }
    static public void Unregister(Delegate callback) { OnEvent -= callback; }

    public delegate void Delegate(AnimationCurve curve, float duration, float remapMin, float remapMax,
        float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true,
        bool resetTargetValuesAfterShake = true, bool forwardDirection = true,
        TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false);

    static public void Trigger(AnimationCurve curve, float duration, float remapMin, float remapMax,
        float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true,
        bool resetTargetValuesAfterShake = true, bool forwardDirection = true,
        TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
    {
        OnEvent?.Invoke(curve, duration, remapMin, remapMax, feedbacksIntensity, channelData,
            resetShakerValuesAfterShake, resetTargetValuesAfterShake, forwardDirection, timescaleMode, stop, restore);
    }
}
```

## 2. Shaker

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineWidthShaker : MMShaker
{
    [Tooltip("the curve to shake the width multiplier on, normalized 0-1 on both axes")]
    public AnimationCurve ShakeWidth = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    [Tooltip("the width multiplier to map the curve's 0 to")]
    public float RemapWidthZero = 1f;
    [Tooltip("the width multiplier to map the curve's 1 to")]
    public float RemapWidthOne = 2f;

    protected LineRenderer _lineRenderer;
    protected float _initialWidth;
    protected float _originalShakeDuration;
    protected AnimationCurve _originalShakeWidth;
    protected float _originalRemapWidthZero;
    protected float _originalRemapWidthOne;

    protected override void Initialization()
    {
        base.Initialization();
        _lineRenderer = GetComponent<LineRenderer>();
    }

    protected override void GrabInitialValues()
    {
        _initialWidth = _lineRenderer.widthMultiplier;
    }

    protected override void Shake()
    {
        _lineRenderer.widthMultiplier = ShakeFloat(ShakeWidth, RemapWidthZero, RemapWidthOne, false, _initialWidth);
    }

    protected override void ResetTargetValues()
    {
        base.ResetTargetValues();
        _lineRenderer.widthMultiplier = _initialWidth;
    }

    protected override void ResetShakerValues()
    {
        base.ResetShakerValues();
        ShakeDuration = _originalShakeDuration;
        ShakeWidth = _originalShakeWidth;
        RemapWidthZero = _originalRemapWidthZero;
        RemapWidthOne = _originalRemapWidthOne;
    }

    public virtual void OnLineWidthShakeEvent(AnimationCurve curve, float duration, float remapMin, float remapMax,
        float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true,
        bool resetTargetValuesAfterShake = true, bool forwardDirection = true,
        TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
    {
        if (!CheckEventAllowed(channelData) || (!Interruptible && Shaking)) { return; }
        if (stop) { Stop(); return; }
        if (restore) { ResetTargetValues(); return; }

        _resetShakerValuesAfterShake = resetShakerValuesAfterShake;
        _resetTargetValuesAfterShake = resetTargetValuesAfterShake;
        if (resetShakerValuesAfterShake)
        {
            _originalShakeDuration = ShakeDuration;
            _originalShakeWidth = ShakeWidth;
            _originalRemapWidthZero = RemapWidthZero;
            _originalRemapWidthOne = RemapWidthOne;
        }
        if (!OnlyUseShakerValues)
        {
            TimescaleMode = timescaleMode;
            ShakeDuration = duration;
            ShakeWidth = curve;
            RemapWidthZero = remapMin * feedbacksIntensity;
            RemapWidthOne = remapMax * feedbacksIntensity;
            ForwardDirection = forwardDirection;
        }
        Play();
    }

    // MMShaker calls StartListening from OnEnable, so the shaker re-registers after domain reloads
    public override void StartListening()
    {
        base.StartListening();
        LineWidthShakeEvent.Register(OnLineWidthShakeEvent);
    }

    public override void StopListening()
    {
        base.StopListening();
        LineWidthShakeEvent.Unregister(OnLineWidthShakeEvent);
    }
}
```

## 3. Feedback

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

[AddComponentMenu("")]
[System.Serializable]
[FeedbackHelp("Shakes the width of LineRenderers that have a LineWidthShaker on the same channel.")]
[FeedbackPath("Renderer/Line Width Shake")]
public partial class MMF_LineWidthShake : MMF_Feedback
{
    [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
    public static bool FeedbackTypeAuthorized = true;
    public override bool HasChannel => true;
    public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }

    [MMFInspectorGroup("Line Width", true, 12)]
    [Tooltip("the duration of the shake, in seconds")]
    public float Duration = 0.3f;
    [Tooltip("the curve to shake the width on")]
    public AnimationCurve Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    [Tooltip("the width multiplier to map the curve's 0 to")]
    public float RemapZero = 1f;
    [Tooltip("the width multiplier to map the curve's 1 to")]
    public float RemapOne = 2f;

    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
    {
        if (!Active || !FeedbackTypeAuthorized) { return; }
        float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
        LineWidthShakeEvent.Trigger(Curve, FeedbackDuration, RemapZero, RemapOne, intensityMultiplier, ChannelData,
            true, true, NormalPlayDirection, ComputedTimescaleMode);
    }

    protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
    {
        if (!Active || !FeedbackTypeAuthorized) { return; }
        LineWidthShakeEvent.Trigger(Curve, FeedbackDuration, RemapZero, RemapOne, stop: true, channelData: ChannelData);
    }

    protected override void CustomRestoreInitialValues()
    {
        if (!Active || !FeedbackTypeAuthorized) { return; }
        LineWidthShakeEvent.Trigger(Curve, FeedbackDuration, RemapZero, RemapOne, restore: true, channelData: ChannelData);
    }
}
```

## Checks

- The scripts compile without UAL0010 / UAL0013 warnings (every static member carries a statics cleanup attribute).
- Add `LineWidthShaker` to an object with a `LineRenderer`; its `Channel` (0 by default) must match the feedback's.
- In Play mode, playing an MMF Player with `Renderer/Line Width Shake` widens the line and restores it after `Duration`.
- `MMShaker` members used above (`ShakeDuration`, `Interruptible`, `Shaking`, `OnlyUseShakerValues`, `TimescaleMode`, `ForwardDirection`, `_resetShakerValuesAfterShake`, `_resetTargetValuesAfterShake`, `CheckEventAllowed`, `ShakeFloat`, `Play`, `Stop`) are defined in `Assets/Feel/MMFeedbacks/MMFeedbacks/Core/MMShaker.cs`.
