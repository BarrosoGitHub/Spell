using System;
using System.Collections;
using UnityEngine;

public class Interpolate : MonoBehaviour
{
    public delegate void InterpolationDelegate(float proportion);
    public delegate void UpdateDelegate();
    public delegate void CompleteDelegate();

    private float duration;
    private float life;
    private bool stopped;

    private InterpolationDelegate OnInterpolationDelegate;
    private UpdateDelegate OnUpdateDelegate = null;
    private CompleteDelegate OnCompleteDelegate = null;

    private Ease.Function easeFunction = Ease.Linear;

    public void Initialize(float duration, InterpolationDelegate OnUpdate)
    {
        this.duration = duration;
        life = 0f;
        stopped = false;

        this.OnInterpolationDelegate = OnUpdate;

        // Start the interpolation coroutine
        StartCoroutine(InterpolationCoroutine());
    }

    private IEnumerator InterpolationCoroutine()
    {
        // Defer playback so fluent SetEase/OnComplete calls configure the first sample.
        yield return null;
        if (duration <= 0f && !stopped)
        {
            OnInterpolationDelegate?.Invoke(1f);
            OnUpdateDelegate?.Invoke();
        }
        while (!stopped && life < duration)
        {
            life += Time.deltaTime;

            float proportion = Mathf.Clamp01(life / duration);
            float easedProportion = easeFunction(0, 1, proportion);

            OnInterpolationDelegate?.Invoke(easedProportion);
            OnUpdateDelegate?.Invoke();

            yield return null;
        }

        if (!stopped) OnCompleteDelegate?.Invoke();
        Destroy(this);
    }

    public void Cancel()
    {
        if (stopped) return;
        stopped = true;
        StopAllCoroutines();
        OnCompleteDelegate = null;
        Destroy(this);
    }

    private void OnDisable()
    {
        Cancel();
    }

    public Interpolate OnUpdate(UpdateDelegate onUpdateDelegate)
    {
        this.OnUpdateDelegate = onUpdateDelegate;
        return this;
    }

    public Interpolate OnComplete(CompleteDelegate onCompleteDelegate)
    {
        this.OnCompleteDelegate = onCompleteDelegate;
        return this;
    }

    public Interpolate SetEase(Ease.Function easeFunction)
    {
        this.easeFunction = easeFunction;
        return this;
    }
}

public static class InterpolateExtensions
{
    public static void CancelInterpolations(this GameObject gameObject)
    {
        foreach (Interpolate interpolation in gameObject.GetComponents<Interpolate>())
            interpolation.Cancel();
    }
    public static Interpolate.CompleteDelegate OnComplete = null;
    public static Interpolate InterpolatePosition(this GameObject gameObject, Vector3 targetPosition, float duration)
    {
        Transform transform = gameObject.transform;
        Vector3 initialPosition = transform.position;
        Vector3 deltaPosition = targetPosition - initialPosition;

        Interpolate interpolateComponent = gameObject.AddComponent<Interpolate>();
        interpolateComponent.Initialize(duration,
        (float proportion) =>
        {
            transform.position = initialPosition + (deltaPosition * proportion); // Corrected typo here
        });

        return interpolateComponent;
    }

    public static Interpolate InterpolatePosition(this Controller controller, Vector3 targetPosition, float duration)
    {
        Transform transform = controller.transform;
        Vector3 initialPositon = transform.position;
        Vector3 deltaPostion = targetPosition - initialPositon;

        Interpolate interpolateComponent = controller.gameObject.AddComponent<Interpolate>();
        interpolateComponent.Initialize(duration,
        (float proportion) =>
        {
            transform.position = initialPositon + (deltaPostion * proportion);
        });

        return interpolateComponent;
    }

    public static Interpolate InterpolateRotation(this GameObject gameObject, Quaternion targetRotation, float duration)
    {
        Quaternion initialRotation = gameObject.transform.rotation;
        Interpolate interpolateComponent = gameObject.AddComponent<Interpolate>();
        interpolateComponent.Initialize(duration,
        (float proportion) =>
        {
            gameObject.transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, proportion);
        });

        return interpolateComponent;
    }

    public static Interpolate ToFloat(this GameObject gameObject, Func<float> getter, Action<float> setter, float endValue, float duration)
    {
        Interpolate interpolate = gameObject.AddComponent<Interpolate>();
        float startValue = getter();

        interpolate.Initialize(duration, (float proportion) =>
        {
            float currentValue = Mathf.Lerp(startValue, endValue, proportion);
            setter(currentValue);
        });
        return interpolate;
    }

    public static Interpolate InterpolateMoveBy(this GameObject gameObject, Vector3 moveDistance, float duration)
    {
        Transform transform = gameObject.transform;
        Interpolate interpolateComponent = gameObject.AddComponent<Interpolate>();
        float lastProportion = 0;

        interpolateComponent.Initialize(duration,
        (float proportion) =>
        {
            transform.position += moveDistance * (proportion - lastProportion);
            lastProportion = proportion;

        });

        return interpolateComponent;
    }
}
