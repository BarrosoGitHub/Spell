using UnityEngine;
using MoreMountains.Feedbacks;
using UnityEngine.Scripting.APIUpdating;
#if MM_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// This feedback allows you to control HDRP exposure over time, in any exposure mode.
	/// Fixed exposure is used by the Fixed mode, compensation by all other modes (Automatic, Automatic Histogram, Curve Mapping, Physical Camera),
	/// and limit min / limit max by the Automatic and Automatic Histogram modes. Use the Modify toggles to pick the values to animate.
	/// It requires you have in your scene an object with a Volume
	/// with Exposure active, and a MMExposureShaker_HDRP component.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MM_HDRP
	[FeedbackPath("PostProcess/Exposure HDRP")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.HDRP")]
	[FeedbackHelp("This feedback allows you to control Exposure over time, in any exposure mode. " +
	              "Fixed Exposure is used by the Fixed mode, Compensation by all other modes (Automatic, Automatic Histogram, Curve Mapping, Physical Camera), " +
	              "and Limit Min / Limit Max by the Automatic and Automatic Histogram modes. Use the Modify toggles to pick the values to animate. " +
	              "It requires you have in your scene an object with a Volume " +
	              "with Exposure active, and a MMExposureShaker_HDRP component.")]
	public partial class MMF_Exposure_HDRP : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.PostProcessColor; } }
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif

		/// the duration of this feedback is the duration of the shake
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;

		[MMFInspectorGroup("Exposure", true, 17)]
		/// the duration of the shake, in seconds
		[Tooltip("the duration of the shake, in seconds")]
		public float Duration = 0.2f;
		/// whether or not to reset shaker values after shake
		[Tooltip("whether or not to reset shaker values after shake")]
		public bool ResetShakerValuesAfterShake = true;
		/// whether or not to reset the target's values after shake
		[Tooltip("whether or not to reset the target's values after shake")]
		public bool ResetTargetValuesAfterShake = true;

		[MMFInspectorGroup("Fixed Exposure", true, 18)]
		/// whether or not this feedback should modify the fixed exposure (used in Fixed exposure mode)
		[Tooltip("whether or not this feedback should modify the fixed exposure (used in Fixed exposure mode)")]
		public bool ModifyFixedExposure = true;
		/// the curve to animate the intensity on
		[Tooltip("the curve to animate the intensity on")]
		public AnimationCurve FixedExposure = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapFixedExposureZero = 8.5f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapFixedExposureOne = 6f;
		/// whether or not to add to the initial intensity
		[Tooltip("whether or not to add to the initial intensity")]
		public bool RelativeFixedExposure = false;

		[MMFInspectorGroup("Compensation", true, 19)]
		/// whether or not this feedback should modify the exposure compensation (used in Automatic, Automatic Histogram, Curve Mapping and Physical Camera exposure modes)
		[Tooltip("whether or not this feedback should modify the exposure compensation (used in Automatic, Automatic Histogram, Curve Mapping and Physical Camera exposure modes)")]
		public bool ModifyCompensation = false;
		/// the curve to animate the compensation on
		[Tooltip("the curve to animate the compensation on")]
		public AnimationCurve Compensation = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapCompensationZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapCompensationOne = 2f;
		/// whether or not to add to the initial compensation
		[Tooltip("whether or not to add to the initial compensation")]
		public bool RelativeCompensation = true;

		[MMFInspectorGroup("Limit Min", true, 20)]
		/// whether or not this feedback should modify the minimum exposure limit (used in Automatic and Automatic Histogram exposure modes)
		[Tooltip("whether or not this feedback should modify the minimum exposure limit (used in Automatic and Automatic Histogram exposure modes)")]
		public bool ModifyLimitMin = false;
		/// the curve to animate the limit min on
		[Tooltip("the curve to animate the limit min on")]
		public AnimationCurve LimitMin = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapLimitMinZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapLimitMinOne = 2f;
		/// whether or not to add to the initial limit min
		[Tooltip("whether or not to add to the initial limit min")]
		public bool RelativeLimitMin = true;

		[MMFInspectorGroup("Limit Max", true, 21)]
		/// whether or not this feedback should modify the maximum exposure limit (used in Automatic and Automatic Histogram exposure modes)
		[Tooltip("whether or not this feedback should modify the maximum exposure limit (used in Automatic and Automatic Histogram exposure modes)")]
		public bool ModifyLimitMax = false;
		/// the curve to animate the limit max on
		[Tooltip("the curve to animate the limit max on")]
		public AnimationCurve LimitMax = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		public float RemapLimitMaxZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		public float RemapLimitMaxOne = -2f;
		/// whether or not to add to the initial limit max
		[Tooltip("whether or not to add to the initial limit max")]
		public bool RelativeLimitMax = true;

		/// <summary>
		/// Triggers a Exposure shake
		/// </summary>
		/// <param name="position"></param>
		/// <param name="attenuation"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			MMExposureShakeEvent_HDRP.Trigger(FixedExposure, FeedbackDuration, RemapFixedExposureZero, RemapFixedExposureOne, RelativeFixedExposure, intensityMultiplier,
				ChannelData, ResetShakerValuesAfterShake, ResetTargetValuesAfterShake, NormalPlayDirection, ComputedTimescaleMode,
				modifyFixedExposure: ModifyFixedExposure,
				modifyCompensation: ModifyCompensation, compensation: Compensation, remapCompensationZero: RemapCompensationZero, remapCompensationOne: RemapCompensationOne, relativeCompensation: RelativeCompensation,
				modifyLimitMin: ModifyLimitMin, limitMin: LimitMin, remapLimitMinZero: RemapLimitMinZero, remapLimitMinOne: RemapLimitMinOne, relativeLimitMin: RelativeLimitMin,
				modifyLimitMax: ModifyLimitMax, limitMax: LimitMax, remapLimitMaxZero: RemapLimitMaxZero, remapLimitMaxOne: RemapLimitMaxOne, relativeLimitMax: RelativeLimitMax);

		}

		/// <summary>
		/// On stop we stop our transition
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);

			MMExposureShakeEvent_HDRP.Trigger(FixedExposure, FeedbackDuration, RemapFixedExposureZero,
				RemapFixedExposureOne, RelativeFixedExposure, channelData:ChannelData, stop:true);
		}

		/// <summary>
		/// On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			MMExposureShakeEvent_HDRP.Trigger(FixedExposure, FeedbackDuration, RemapFixedExposureZero,
				RemapFixedExposureOne, RelativeFixedExposure, channelData:ChannelData, restore:true,
				modifyFixedExposure: ModifyFixedExposure, modifyCompensation: ModifyCompensation,
				modifyLimitMin: ModifyLimitMin, modifyLimitMax: ModifyLimitMax);
		}

		/// <summary>
		/// Automaticall sets up the post processing profile and shaker
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			#if MM_HDRP && UNITY_EDITOR
			MMHDRPHelpers.GetOrCreateVolume<Exposure, MMExposureShaker_HDRP>(Owner, "Exposure");
			#endif
		}
	}
}