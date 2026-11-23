using UnityEngine;
using UnityEngine.Rendering;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
#if MM_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// Add this class to a Camera with a HDRP exposure post processing and it'll be able to "shake" its values by getting events.
	/// Fixed exposure is used by the Fixed exposure mode, compensation by all other modes (Automatic, Automatic Histogram, Curve Mapping and Physical Camera),
	/// and limit min / limit max by the automatic modes.
	/// </summary>
	#if MM_HDRP
	[RequireComponent(typeof(Volume))]
	#endif
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/PostProcessing/MM Exposure Shaker HDRP")]
	public class MMExposureShaker_HDRP : MMShaker
	{
		[MMInspectorGroup("Exposure Intensity", true, 46)]
		/// whether or not this shaker should modify the fixed exposure (used in Fixed exposure mode)
		[Tooltip("whether or not this shaker should modify the fixed exposure (used in Fixed exposure mode)")]
		public bool ModifyFixedExposure = true;
		/// whether or not to add to the initial value
		[Tooltip("whether or not to add to the initial value")]
		public bool RelativeIntensity = false;
		/// the curve used to animate the intensity value on
		[Tooltip("the curve used to animate the intensity value on")]
		public AnimationCurve ShakeFixedExposure = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapFixedExposureZero = 8.5f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapFixedExposureOne = 6f;

		[MMInspectorGroup("Compensation", true, 47)]
		/// whether or not this shaker should modify the exposure compensation (used in Automatic, Automatic Histogram, Curve Mapping and Physical Camera exposure modes)
		[Tooltip("whether or not this shaker should modify the exposure compensation (used in Automatic, Automatic Histogram, Curve Mapping and Physical Camera exposure modes)")]
		public bool ModifyCompensation = false;
		/// whether or not to add to the initial value
		[Tooltip("whether or not to add to the initial value")]
		public bool RelativeCompensation = true;
		/// the curve used to animate the compensation value on
		[Tooltip("the curve used to animate the compensation value on")]
		public AnimationCurve ShakeCompensation = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapCompensationZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapCompensationOne = 2f;

		[MMInspectorGroup("Limit Min", true, 48)]
		/// whether or not this shaker should modify the minimum exposure limit (used in Automatic and Automatic Histogram exposure modes)
		[Tooltip("whether or not this shaker should modify the minimum exposure limit (used in Automatic and Automatic Histogram exposure modes)")]
		public bool ModifyLimitMin = false;
		/// whether or not to add to the initial value
		[Tooltip("whether or not to add to the initial value")]
		public bool RelativeLimitMin = true;
		/// the curve used to animate the limit min value on
		[Tooltip("the curve used to animate the limit min value on")]
		public AnimationCurve ShakeLimitMin = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapLimitMinZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapLimitMinOne = 2f;

		[MMInspectorGroup("Limit Max", true, 49)]
		/// whether or not this shaker should modify the maximum exposure limit (used in Automatic and Automatic Histogram exposure modes)
		[Tooltip("whether or not this shaker should modify the maximum exposure limit (used in Automatic and Automatic Histogram exposure modes)")]
		public bool ModifyLimitMax = false;
		/// whether or not to add to the initial value
		[Tooltip("whether or not to add to the initial value")]
		public bool RelativeLimitMax = true;
		/// the curve used to animate the limit max value on
		[Tooltip("the curve used to animate the limit max value on")]
		public AnimationCurve ShakeLimitMax = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapLimitMaxZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapLimitMaxOne = -2f;

		#if MM_HDRP
		protected Volume _volume;
		protected Exposure _exposure;
		protected float _initialFixedExposure;
		protected float _initialCompensation;
		protected float _initialLimitMin;
		protected float _initialLimitMax;
		protected float _originalShakeDuration;
		protected bool _originalModifyFixedExposure;
		protected AnimationCurve _originalShakeFixedExposure;
		protected float _originalRemapFixedExposureZero;
		protected float _originalRemapFixedExposureOne;
		protected bool _originalRelativeFixedExposure;
		protected bool _originalModifyCompensation;
		protected AnimationCurve _originalShakeCompensation;
		protected float _originalRemapCompensationZero;
		protected float _originalRemapCompensationOne;
		protected bool _originalRelativeCompensation;
		protected bool _originalModifyLimitMin;
		protected AnimationCurve _originalShakeLimitMin;
		protected float _originalRemapLimitMinZero;
		protected float _originalRemapLimitMinOne;
		protected bool _originalRelativeLimitMin;
		protected bool _originalModifyLimitMax;
		protected AnimationCurve _originalShakeLimitMax;
		protected float _originalRemapLimitMaxZero;
		protected float _originalRemapLimitMaxOne;
		protected bool _originalRelativeLimitMax;

		/// <summary>
		/// On init we initialize our values
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_volume = this.gameObject.GetComponent<Volume>();
			_volume.profile.TryGet(out _exposure);
		}

		/// <summary>
		/// Shakes values over time
		/// </summary>
		protected override void Shake()
		{
			if (ModifyFixedExposure)
			{
				float newFixedExposure = ShakeFloat(ShakeFixedExposure, RemapFixedExposureZero, RemapFixedExposureOne, RelativeIntensity, _initialFixedExposure);
				_exposure.fixedExposure.Override(newFixedExposure);
			}
			if (ModifyCompensation)
			{
				float newCompensation = ShakeFloat(ShakeCompensation, RemapCompensationZero, RemapCompensationOne, RelativeCompensation, _initialCompensation);
				_exposure.compensation.Override(newCompensation);
			}
			if (ModifyLimitMin)
			{
				float newLimitMin = ShakeFloat(ShakeLimitMin, RemapLimitMinZero, RemapLimitMinOne, RelativeLimitMin, _initialLimitMin);
				_exposure.limitMin.Override(newLimitMin);
			}
			if (ModifyLimitMax)
			{
				float newLimitMax = ShakeFloat(ShakeLimitMax, RemapLimitMaxZero, RemapLimitMaxOne, RelativeLimitMax, _initialLimitMax);
				_exposure.limitMax.Override(newLimitMax);
			}
		}

		/// <summary>
		/// Collects initial values on the target
		/// </summary>
		protected override void GrabInitialValues()
		{
			_initialFixedExposure = _exposure.fixedExposure.value;
			_initialCompensation = _exposure.compensation.value;
			_initialLimitMin = _exposure.limitMin.value;
			_initialLimitMax = _exposure.limitMax.value;
		}

		/// <summary>
		/// When we get the appropriate event, we trigger a shake
		/// </summary>
		/// <param name="intensity"></param>
		/// <param name="duration"></param>
		/// <param name="amplitude"></param>
		/// <param name="relativeIntensity"></param>
		/// <param name="attenuation"></param>
		/// <param name="channel"></param>
		public virtual void OnExposureShakeEvent(AnimationCurve intensity, float duration, float remapMin, float remapMax, bool relativeIntensity = false,
			float attenuation = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true,
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false,
			bool modifyFixedExposure = true,
			bool modifyCompensation = false, AnimationCurve compensation = null, float remapCompensationZero = 0f, float remapCompensationOne = 0f, bool relativeCompensation = true,
			bool modifyLimitMin = false, AnimationCurve limitMin = null, float remapLimitMinZero = 0f, float remapLimitMinOne = 0f, bool relativeLimitMin = true,
			bool modifyLimitMax = false, AnimationCurve limitMax = null, float remapLimitMaxZero = 0f, float remapLimitMaxOne = 0f, bool relativeLimitMax = true)
		{
			if (!CheckEventAllowed(channelData) || (!Interruptible && Shaking))
			{
				return;
			}

			if (stop)
			{
				Stop();
				return;
			}

			if (restore)
			{
				if (OnlyUseShakerValues)
				{
					ResetTargetValues();
					return;
				}
				// we restore the values the event asks for, then put our own settings back
				bool shakerModifyFixedExposure = ModifyFixedExposure;
				bool shakerModifyCompensation = ModifyCompensation;
				bool shakerModifyLimitMin = ModifyLimitMin;
				bool shakerModifyLimitMax = ModifyLimitMax;
				ModifyFixedExposure = modifyFixedExposure;
				ModifyCompensation = modifyCompensation;
				ModifyLimitMin = modifyLimitMin;
				ModifyLimitMax = modifyLimitMax;
				ResetTargetValues();
				ModifyFixedExposure = shakerModifyFixedExposure;
				ModifyCompensation = shakerModifyCompensation;
				ModifyLimitMin = shakerModifyLimitMin;
				ModifyLimitMax = shakerModifyLimitMax;
				return;
			}

			_resetShakerValuesAfterShake = resetShakerValuesAfterShake;
			_resetTargetValuesAfterShake = resetTargetValuesAfterShake;

			if (resetShakerValuesAfterShake)
			{
				_originalShakeDuration = ShakeDuration;
				_originalModifyFixedExposure = ModifyFixedExposure;
				_originalShakeFixedExposure = ShakeFixedExposure;
				_originalRemapFixedExposureZero = RemapFixedExposureZero;
				_originalRemapFixedExposureOne = RemapFixedExposureOne;
				_originalRelativeFixedExposure = RelativeIntensity;
				_originalModifyCompensation = ModifyCompensation;
				_originalShakeCompensation = ShakeCompensation;
				_originalRemapCompensationZero = RemapCompensationZero;
				_originalRemapCompensationOne = RemapCompensationOne;
				_originalRelativeCompensation = RelativeCompensation;
				_originalModifyLimitMin = ModifyLimitMin;
				_originalShakeLimitMin = ShakeLimitMin;
				_originalRemapLimitMinZero = RemapLimitMinZero;
				_originalRemapLimitMinOne = RemapLimitMinOne;
				_originalRelativeLimitMin = RelativeLimitMin;
				_originalModifyLimitMax = ModifyLimitMax;
				_originalShakeLimitMax = ShakeLimitMax;
				_originalRemapLimitMaxZero = RemapLimitMaxZero;
				_originalRemapLimitMaxOne = RemapLimitMaxOne;
				_originalRelativeLimitMax = RelativeLimitMax;
			}

			if (!OnlyUseShakerValues)
			{
				TimescaleMode = timescaleMode;
				ShakeDuration = duration;
				ModifyFixedExposure = modifyFixedExposure;
				ShakeFixedExposure = intensity;
				RemapFixedExposureZero = remapMin * attenuation;
				RemapFixedExposureOne = remapMax * attenuation;
				RelativeIntensity = relativeIntensity;
				ModifyCompensation = modifyCompensation;
				if (compensation != null) { ShakeCompensation = compensation; }
				RemapCompensationZero = remapCompensationZero * attenuation;
				RemapCompensationOne = remapCompensationOne * attenuation;
				RelativeCompensation = relativeCompensation;
				ModifyLimitMin = modifyLimitMin;
				if (limitMin != null) { ShakeLimitMin = limitMin; }
				RemapLimitMinZero = remapLimitMinZero * attenuation;
				RemapLimitMinOne = remapLimitMinOne * attenuation;
				RelativeLimitMin = relativeLimitMin;
				ModifyLimitMax = modifyLimitMax;
				if (limitMax != null) { ShakeLimitMax = limitMax; }
				RemapLimitMaxZero = remapLimitMaxZero * attenuation;
				RemapLimitMaxOne = remapLimitMaxOne * attenuation;
				RelativeLimitMax = relativeLimitMax;
				ForwardDirection = forwardDirection;
			}

			Play();
		}

		/// <summary>
		/// Resets the target's values
		/// </summary>
		protected override void ResetTargetValues()
		{
			base.ResetTargetValues();
			if (ModifyFixedExposure) { _exposure.fixedExposure.Override(_initialFixedExposure); }
			if (ModifyCompensation) { _exposure.compensation.Override(_initialCompensation); }
			if (ModifyLimitMin) { _exposure.limitMin.Override(_initialLimitMin); }
			if (ModifyLimitMax) { _exposure.limitMax.Override(_initialLimitMax); }
		}

		/// <summary>
		/// Resets the shaker's values
		/// </summary>
		protected override void ResetShakerValues()
		{
			base.ResetShakerValues();
			ShakeDuration = _originalShakeDuration;
			ModifyFixedExposure = _originalModifyFixedExposure;
			ShakeFixedExposure = _originalShakeFixedExposure;
			RemapFixedExposureZero = _originalRemapFixedExposureZero;
			RemapFixedExposureOne = _originalRemapFixedExposureOne;
			RelativeIntensity = _originalRelativeFixedExposure;
			ModifyCompensation = _originalModifyCompensation;
			ShakeCompensation = _originalShakeCompensation;
			RemapCompensationZero = _originalRemapCompensationZero;
			RemapCompensationOne = _originalRemapCompensationOne;
			RelativeCompensation = _originalRelativeCompensation;
			ModifyLimitMin = _originalModifyLimitMin;
			ShakeLimitMin = _originalShakeLimitMin;
			RemapLimitMinZero = _originalRemapLimitMinZero;
			RemapLimitMinOne = _originalRemapLimitMinOne;
			RelativeLimitMin = _originalRelativeLimitMin;
			ModifyLimitMax = _originalModifyLimitMax;
			ShakeLimitMax = _originalShakeLimitMax;
			RemapLimitMaxZero = _originalRemapLimitMaxZero;
			RemapLimitMaxOne = _originalRemapLimitMaxOne;
			RelativeLimitMax = _originalRelativeLimitMax;
		}

		/// <summary>
		/// Starts listening for events
		/// </summary>
		public override void StartListening()
		{
			base.StartListening();
			MMExposureShakeEvent_HDRP.Register(OnExposureShakeEvent);
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		public override void StopListening()
		{
			base.StopListening();
			MMExposureShakeEvent_HDRP.Unregister(OnExposureShakeEvent);
		}
		#endif
	}

	/// <summary>
	/// An event used to trigger exposure shakes.
	/// Fixed exposure is used by the Fixed exposure mode, compensation by all other modes, and limit min / limit max by the automatic modes.
	/// </summary>
	public partial struct MMExposureShakeEvent_HDRP
	{
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		static private event Delegate OnEvent;
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(AnimationCurve fixedExposure, float duration, float remapMin, float remapMax, bool relativeFixedExposure = false,
			float attenuation = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true,
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false,
			bool modifyFixedExposure = true,
			bool modifyCompensation = false, AnimationCurve compensation = null, float remapCompensationZero = 0f, float remapCompensationOne = 0f, bool relativeCompensation = true,
			bool modifyLimitMin = false, AnimationCurve limitMin = null, float remapLimitMinZero = 0f, float remapLimitMinOne = 0f, bool relativeLimitMin = true,
			bool modifyLimitMax = false, AnimationCurve limitMax = null, float remapLimitMaxZero = 0f, float remapLimitMaxOne = 0f, bool relativeLimitMax = true);

		static public void Trigger(AnimationCurve fixedExposure, float duration, float remapMin, float remapMax, bool relativeFixedExposure = false,
			float attenuation = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true,
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false,
			bool modifyFixedExposure = true,
			bool modifyCompensation = false, AnimationCurve compensation = null, float remapCompensationZero = 0f, float remapCompensationOne = 0f, bool relativeCompensation = true,
			bool modifyLimitMin = false, AnimationCurve limitMin = null, float remapLimitMinZero = 0f, float remapLimitMinOne = 0f, bool relativeLimitMin = true,
			bool modifyLimitMax = false, AnimationCurve limitMax = null, float remapLimitMaxZero = 0f, float remapLimitMaxOne = 0f, bool relativeLimitMax = true)
		{
			OnEvent?.Invoke(fixedExposure, duration, remapMin, remapMax, relativeFixedExposure, attenuation, channelData, resetShakerValuesAfterShake,
				resetTargetValuesAfterShake, forwardDirection, timescaleMode, stop, restore,
				modifyFixedExposure,
				modifyCompensation, compensation, remapCompensationZero, remapCompensationOne, relativeCompensation,
				modifyLimitMin, limitMin, remapLimitMinZero, remapLimitMinOne, relativeLimitMin,
				modifyLimitMax, limitMax, remapLimitMaxZero, remapLimitMaxOne, relativeLimitMax);
		}
	}
}
