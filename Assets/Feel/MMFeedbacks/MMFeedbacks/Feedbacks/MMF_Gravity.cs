using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you change the global gravity (Physics.gravity and/or Physics2D.gravity), either once, instantly then reset after a duration, or over time along a curve
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback lets you change the global gravity (Physics.gravity for 3D, Physics2D.gravity for 2D, or both).")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Physics/Gravity")]
	public partial class MMF_Gravity : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override string RequiredTargetText { get { return Mode.ToString() + " " + Dimension.ToString();  } }
		public override bool HasCustomInspectors => true;
		#endif
		public override bool HasRandomness => true;

		/// the possible modes for this feedback
		/// - Instant : the gravity is changed once, and stays that way
		/// - InstantThenReset : the gravity is changed, then reset to its previous value after Duration
		/// - OverTime : the gravity is evaluated along a curve over Duration
		public enum Modes { Instant, InstantThenReset, OverTime }
		/// the physics engines whose gravity this feedback should change
		public enum Dimensions { Physics3D, Physics2D, Both }
		/// how the new gravity is computed
		/// - Multiplier : the gravity found on play gets multiplied by a value
		/// - Absolute : the gravity moves towards a target vector
		public enum ValueModes { Multiplier, Absolute }

		[MMFInspectorGroup("Gravity", true, 61)]
		/// the physics engines whose gravity this feedback should change (2D requires the Physics 2D module)
		[Tooltip("the physics engines whose gravity this feedback should change (2D requires the Physics 2D module)")]
		public Dimensions Dimension = Dimensions.Physics3D;
		/// whether to change the gravity once, instantly then reset it after a duration, or over time along a curve
		[Tooltip("whether to change the gravity once, instantly then reset it after a duration, or over time along a curve")]
		public Modes Mode = Modes.InstantThenReset;
		/// whether to multiply the gravity found on play, or move it towards an absolute value
		[Tooltip("whether to multiply the gravity found on play (Multiplier), or move it towards an absolute value (Absolute)")]
		public ValueModes ValueMode = ValueModes.Multiplier;
		/// in InstantThenReset and OverTime modes, how long the gravity change should last, in seconds
		[Tooltip("in InstantThenReset and OverTime modes, how long the gravity change should last, in seconds")]
		[MMFEnumCondition("Mode", (int)Modes.InstantThenReset, (int)Modes.OverTime)]
		public float Duration = 1f;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over")]
		public bool AllowAdditivePlays = false;
		/// if this is true, the gravity will be reset to the value it had before this feedback played when the feedback gets stopped
		[Tooltip("if this is true, the gravity will be reset to the value it had before this feedback played when the feedback gets stopped")]
		public bool ResetOnStop = true;

		[MMFInspectorGroup("Values", true, 62)]
		/// in Instant and InstantThenReset modes, when in Multiplier value mode, the value to multiply the gravity by (0.25 is low gravity, 2 is heavy gravity, -1 flips it)
		[Tooltip("in Instant and InstantThenReset modes, when in Multiplier value mode, the value to multiply the gravity by (0.25 is low gravity, 2 is heavy gravity, -1 flips it)")]
		[MMFEnumCondition("ValueMode", (int)ValueModes.Multiplier)]
		public float Multiplier = 0.25f;
		/// in Absolute value mode, the gravity to apply to 3D physics
		[Tooltip("in Absolute value mode, the gravity to apply to 3D physics")]
		[MMFEnumCondition("ValueMode", (int)ValueModes.Absolute)]
		public Vector3 TargetGravity = new Vector3(0f, -2.5f, 0f);
		/// in Absolute value mode, the gravity to apply to 2D physics
		[Tooltip("in Absolute value mode, the gravity to apply to 2D physics")]
		[MMFEnumCondition("ValueMode", (int)ValueModes.Absolute)]
		public Vector2 TargetGravity2D = new Vector2(0f, -2.5f);

		[MMFInspectorGroup("Over Time", true, 63)]
		/// in OverTime mode, the curve along which to evaluate the gravity. Its value gets remapped between CurveRemapZero and CurveRemapOne
		[Tooltip("in OverTime mode, the curve along which to evaluate the gravity. Its value gets remapped between CurveRemapZero and CurveRemapOne")]
		public MMTweenType Curve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)),
			enumConditionPropertyName:"Mode", enumConditionValues:(int)Modes.OverTime);
		/// in OverTime mode, the value to remap the curve's zero to. In Multiplier value mode, it's the gravity multiplier. In Absolute value mode, 0 is the initial gravity and 1 is the target gravity
		[Tooltip("in OverTime mode, the value to remap the curve's zero to. In Multiplier value mode, it's the gravity multiplier. In Absolute value mode, 0 is the initial gravity and 1 is the target gravity")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float CurveRemapZero = 1f;
		/// in OverTime mode, the value to remap the curve's one to. In Multiplier value mode, it's the gravity multiplier. In Absolute value mode, 0 is the initial gravity and 1 is the target gravity
		[Tooltip("in OverTime mode, the value to remap the curve's one to. In Multiplier value mode, it's the gravity multiplier. In Absolute value mode, 0 is the initial gravity and 1 is the target gravity")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float CurveRemapOne = 0.25f;

		/// the duration of this feedback is the duration of the gravity change, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { if (Mode != Modes.Instant) { Duration = value; } } }

		protected Coroutine _coroutine;
		protected Vector3 _initialGravity;
		protected Vector2 _initialGravity2D;
		protected float _intensityMultiplier = 1f;
		protected bool _initialGravityStored = false;

		#if UNITY_EDITOR
		// NoAutoStaticsCleanup: must survive exiting Play mode, so the project's gravity can be restored once back in Edit mode
		[Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
		protected static bool _projectGravityStored;
		// NoAutoStaticsCleanup: must survive exiting Play mode, so the project's gravity can be restored once back in Edit mode
		[Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
		protected static Vector3 _projectGravity;
		// NoAutoStaticsCleanup: must survive exiting Play mode, so the project's gravity can be restored once back in Edit mode
		[Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
		protected static Vector2 _projectGravity2D;
		#endif

		/// <summary>
		/// On play, stores the current gravity and changes it according to the selected mode
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || !Application.isPlaying)
			{
				return;
			}

			if (!AllowAdditivePlays && (_coroutine != null))
			{
				return;
			}

			StoreProjectGravity();

			if (!IsPlaying)
			{
				StoreInitialGravity();
			}

			_intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);

			switch (Mode)
			{
				case Modes.Instant:
					if (NormalPlayDirection)
					{
						ApplyInstantValue();
					}
					else
					{
						ApplyGravity(_initialGravity, _initialGravity2D);
					}
					break;
				case Modes.InstantThenReset:
				case Modes.OverTime:
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(GravitySequence());
					break;
			}
		}

		/// <summary>
		/// A coroutine used to change the gravity in InstantThenReset and OverTime modes
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator GravitySequence()
		{
			IsPlaying = true;

			if (Mode == Modes.InstantThenReset)
			{
				ApplyInstantValue();
				yield return WaitFor(FeedbackDuration);
				ApplyGravity(_initialGravity, _initialGravity2D);
			}
			else
			{
				float journey = NormalPlayDirection ? 0f : FeedbackDuration;
				while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
				{
					float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
					ApplyCurveValue(remappedTime);
					journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
					yield return null;
				}
				ApplyCurveValue(FinalNormalizedTime);
			}

			_coroutine = null;
			IsPlaying = false;
		}

		/// <summary>
		/// Applies the gravity defined for Instant and InstantThenReset modes
		/// </summary>
		protected virtual void ApplyInstantValue()
		{
			ApplyValue(ValueMode == ValueModes.Multiplier ? Multiplier : 1f);
		}

		/// <summary>
		/// Evaluates the curve at the specified normalized time and applies the resulting gravity
		/// </summary>
		/// <param name="normalizedTime"></param>
		protected virtual void ApplyCurveValue(float normalizedTime)
		{
			ApplyValue(MMTween.Tween(normalizedTime, 0f, 1f, CurveRemapZero, CurveRemapOne, Curve));
		}

		/// <summary>
		/// Computes the gravity for the specified value (a multiplier, or a lerp factor between initial and target gravity in Absolute mode),
		/// scales its deviation from the initial gravity by the feedback's intensity, and applies it
		/// </summary>
		/// <param name="value"></param>
		protected virtual void ApplyValue(float value)
		{
			Vector3 gravity;
			Vector2 gravity2D;
			if (ValueMode == ValueModes.Multiplier)
			{
				gravity = _initialGravity * value;
				gravity2D = _initialGravity2D * value;
			}
			else
			{
				gravity = Vector3.LerpUnclamped(_initialGravity, TargetGravity, value);
				gravity2D = Vector2.LerpUnclamped(_initialGravity2D, TargetGravity2D, value);
			}

			gravity = Vector3.LerpUnclamped(_initialGravity, gravity, _intensityMultiplier);
			gravity2D = Vector2.LerpUnclamped(_initialGravity2D, gravity2D, _intensityMultiplier);
			ApplyGravity(gravity, gravity2D);
		}

		/// <summary>
		/// Sets the gravity on the selected physics engines
		/// </summary>
		/// <param name="gravity"></param>
		/// <param name="gravity2D"></param>
		protected virtual void ApplyGravity(Vector3 gravity, Vector2 gravity2D)
		{
			if (Dimension != Dimensions.Physics2D)
			{
				Physics.gravity = gravity;
			}
			#if MM_PHYSICS2D
			if (Dimension != Dimensions.Physics3D)
			{
				Physics2D.gravity = gravity2D;
			}
			#endif
		}

		/// <summary>
		/// Stores the gravity before this feedback changes it
		/// </summary>
		protected virtual void StoreInitialGravity()
		{
			_initialGravity = Physics.gravity;
			#if MM_PHYSICS2D
			_initialGravity2D = Physics2D.gravity;
			#endif
			_initialGravityStored = true;
		}

		/// <summary>
		/// In the editor, stores the project's gravity the first time any gravity feedback plays, so it can be restored when exiting Play mode
		/// (gravity is a project setting, and changes made to it in Play mode would otherwise stick)
		/// </summary>
		protected virtual void StoreProjectGravity()
		{
			#if UNITY_EDITOR
			if (_projectGravityStored)
			{
				return;
			}
			_projectGravity = Physics.gravity;
			#if MM_PHYSICS2D
			_projectGravity2D = Physics2D.gravity;
			#endif
			_projectGravityStored = true;
			#endif
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Registers to play mode state changes, to restore the project's gravity when exiting Play mode
		/// </summary>
		[InitializeOnLoadMethod]
		protected static void InitializeProjectGravityRestore()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		/// <summary>
		/// When exiting Play mode, restores the project's gravity if a gravity feedback changed it
		/// </summary>
		/// <param name="state"></param>
		protected static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if ((state != PlayModeStateChange.EnteredEditMode) || !_projectGravityStored)
			{
				return;
			}
			Physics.gravity = _projectGravity;
			#if MM_PHYSICS2D
			Physics2D.gravity = _projectGravity2D;
			#endif
			_projectGravityStored = false;
		}
		#endif

		/// <summary>
		/// On stop, stops the coroutine and resets the gravity if needed
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

			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
				if (ResetOnStop && Application.isPlaying)
				{
					ApplyGravity(_initialGravity, _initialGravity2D);
				}
			}
			IsPlaying = false;
		}

		/// <summary>
		/// On restore, puts the gravity back to the value it had before this feedback played
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized || !Application.isPlaying || !_initialGravityStored)
			{
				return;
			}
			ApplyGravity(_initialGravity, _initialGravity2D);
		}
	}
}
