using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you change the mouse cursor's texture, visibility and lock state, either once, or for a duration after which they get reset
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback lets you change the mouse cursor's texture (and hotspot), its visibility and its lock state.")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("UI/Cursor")]
	public partial class MMF_Cursor : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return !ModifyTexture && !ModifyVisibility && !ModifyLockState; }
		public override string RequiredTargetText { get { return ModifyTexture && (CursorTexture != null) ? CursorTexture.name : Mode.ToString(); } }
		public override string RequiresSetupText { get { return "This feedback requires that you enable at least one of ModifyTexture, ModifyVisibility or ModifyLockState to do something."; } }
		#endif

		/// the possible modes for this feedback
		/// - Instant : the cursor is changed once, and stays that way
		/// - InstantThenReset : the cursor is changed, then reset after Duration
		public enum Modes { Instant, InstantThenReset }
		/// the possible ways to pick the texture to set back when the cursor gets reset
		/// - ResetTexture : the ResetTexture (the system cursor if empty)
		/// - InitialTexture : the texture the cursor had when the feedback played, if it was different from CursorTexture
		public enum TextureResetModes { ResetTexture, InitialTexture }

		[MMFInspectorGroup("Cursor", true, 64)]
		/// whether to change the cursor once, or change it and reset it after a duration
		[Tooltip("whether to change the cursor once (Instant), or change it and reset it after a duration (InstantThenReset)")]
		public Modes Mode = Modes.InstantThenReset;
		/// in InstantThenReset mode, how long the cursor should stay changed, in seconds
		[Tooltip("in InstantThenReset mode, how long the cursor should stay changed, in seconds")]
		[MMFEnumCondition("Mode", (int)Modes.InstantThenReset)]
		public float Duration = 0.5f;
		/// if this is true, the cursor will be reset when the feedback gets stopped
		[Tooltip("if this is true, the cursor will be reset when the feedback gets stopped")]
		public bool ResetOnStop = true;

		[MMFInspectorGroup("Texture", true, 65)]
		/// whether or not to change the cursor's texture
		[Tooltip("whether or not to change the cursor's texture")]
		public bool ModifyTexture = true;
		/// the texture to use as the cursor (import it with its Texture Type set to Cursor). Leave it empty to use the system cursor
		[Tooltip("the texture to use as the cursor (import it with its Texture Type set to Cursor). Leave it empty to use the system cursor")]
		public Texture2D CursorTexture;
		/// the offset from the top left of the texture to use as the target point (the tip of an arrow, the center of a crosshair, etc)
		[Tooltip("the offset from the top left of the texture to use as the target point (the tip of an arrow, the center of a crosshair, etc)")]
		public Vector2 Hotspot = Vector2.zero;
		/// whether to let the platform render the cursor (Auto, hardware cursor where supported) or always render it in software (ForceSoftware)
		[Tooltip("whether to let the platform render the cursor (Auto, hardware cursor where supported) or always render it in software (ForceSoftware)")]
		public CursorMode CursorRenderMode = CursorMode.Auto;
		/// how to pick the texture to set back when the cursor gets reset : the ResetTexture, or the texture the cursor had when the feedback played (if it was different from CursorTexture).
		/// Unity can't read the cursor's current texture, so InitialTexture only knows about textures set through Feel (MMF_Cursor feedbacks, or MMF_Cursor.SetCursor() calls)
		[Tooltip("how to pick the texture to set back when the cursor gets reset : " +
		         "\n- ResetTexture : the ResetTexture below (the system cursor if empty)" +
		         "\n- InitialTexture : the texture the cursor had when the feedback played, if it was different from CursorTexture. If no such texture was found, ResetTexture is used instead. " +
		         "Unity can't read the cursor's current texture, so this only knows about textures set through Feel (MMF_Cursor feedbacks, or MMF_Cursor.SetCursor() calls from your code)")]
		public TextureResetModes TextureResetMode = TextureResetModes.InitialTexture;
		/// the texture to set back when the cursor gets reset (in InitialTexture mode, only if no initial texture was found). Leave it empty to go back to the system cursor
		[Tooltip("the texture to set back when the cursor gets reset (in InitialTexture mode, only if no initial texture was found). Leave it empty to go back to the system cursor")]
		public Texture2D ResetTexture;
		/// the hotspot to use with the ResetTexture
		[Tooltip("the hotspot to use with the ResetTexture")]
		public Vector2 ResetHotspot = Vector2.zero;

		[MMFInspectorGroup("Visibility & Lock", true, 66)]
		/// whether or not to change the cursor's visibility
		[Tooltip("whether or not to change the cursor's visibility")]
		public bool ModifyVisibility = false;
		/// whether the cursor should be visible or not
		[Tooltip("whether the cursor should be visible or not")]
		public bool Visible = true;
		/// whether or not to change the cursor's lock state
		[Tooltip("whether or not to change the cursor's lock state")]
		public bool ModifyLockState = false;
		/// the lock state to apply to the cursor (None : free, Locked : locked to the center of the view, Confined : confined to the game window)
		[Tooltip("the lock state to apply to the cursor (None : free, Locked : locked to the center of the view, Confined : confined to the game window)")]
		public CursorLockMode LockState = CursorLockMode.None;

		/// the duration of this feedback is the duration of the cursor change, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { if (Mode != Modes.Instant) { Duration = value; } } }

		protected Coroutine _coroutine;
		protected bool _initialVisible;
		protected CursorLockMode _initialLockState;
		protected bool _initialValuesStored = false;
		protected bool _initialTextureStored = false;
		protected Texture2D _initialTexture;
		protected Vector2 _initialHotspot;
		protected CursorMode _initialRenderMode;

		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static Texture2D _currentTexture;
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static Vector2 _currentHotspot;
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static CursorMode _currentRenderMode;

		/// <summary>
		/// Sets the cursor's texture, like Cursor.SetCursor does, and remembers it, so that MMF_Cursor feedbacks set to the InitialTexture reset mode can put it back later.
		/// Call this from your own code instead of Cursor.SetCursor if you want MMF_Cursor feedbacks to know about your cursor changes
		/// </summary>
		/// <param name="texture">the texture to use as the cursor, null for the system cursor</param>
		/// <param name="hotspot">the offset from the top left of the texture to use as the target point</param>
		/// <param name="cursorMode">whether to let the platform render the cursor, or always render it in software</param>
		public static void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode = CursorMode.Auto)
		{
			_currentTexture = texture;
			_currentHotspot = hotspot;
			_currentRenderMode = cursorMode;
			Cursor.SetCursor(texture, hotspot, cursorMode);
		}

		/// <summary>
		/// On play, changes the cursor, and resets it after a duration if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (!IsPlaying)
			{
				_initialVisible = Cursor.visible;
				_initialLockState = Cursor.lockState;
				_initialValuesStored = true;
			}

			if (!NormalPlayDirection)
			{
				ResetCursor();
				return;
			}

			switch (Mode)
			{
				case Modes.Instant:
					ApplyCursor();
					break;
				case Modes.InstantThenReset:
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(CursorSequence());
					break;
			}
		}

		/// <summary>
		/// Changes the cursor, waits for the duration, then resets it
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator CursorSequence()
		{
			IsPlaying = true;
			ApplyCursor();
			yield return WaitFor(FeedbackDuration);
			ResetCursor();
			_coroutine = null;
			IsPlaying = false;
		}

		/// <summary>
		/// Applies the texture, visibility and lock state changes
		/// </summary>
		protected virtual void ApplyCursor()
		{
			if (ModifyTexture)
			{
				StoreInitialTexture();
				SetCursor(CursorTexture, Hotspot, CursorRenderMode);
			}
			if (ModifyVisibility)
			{
				Cursor.visible = Visible;
			}
			if (ModifyLockState)
			{
				Cursor.lockState = LockState;
			}
		}

		/// <summary>
		/// Stores the texture the cursor has before this feedback changes it
		/// </summary>
		protected virtual void StoreInitialTexture()
		{
			if (_currentTexture == CursorTexture)
			{
				return;
			}
			_initialTexture = _currentTexture;
			_initialHotspot = _currentHotspot;
			_initialRenderMode = _currentRenderMode;
			_initialTextureStored = true;
		}

		/// <summary>
		/// Puts the reset texture (or the initial one, depending on the TextureResetMode) back, and restores the visibility and lock state the cursor had when the feedback played
		/// </summary>
		protected virtual void ResetCursor()
		{
			if (ModifyTexture)
			{
				if ((TextureResetMode == TextureResetModes.InitialTexture) && _initialTextureStored)
				{
					SetCursor(_initialTexture, _initialHotspot, _initialRenderMode);
				}
				else
				{
					SetCursor(ResetTexture, ResetHotspot, CursorRenderMode);
				}
			}
			if (!_initialValuesStored)
			{
				return;
			}
			if (ModifyVisibility)
			{
				Cursor.visible = _initialVisible;
			}
			if (ModifyLockState)
			{
				Cursor.lockState = _initialLockState;
			}
		}

		/// <summary>
		/// On stop, we stop the coroutine and resets the cursor if needed
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
			}
			if (ResetOnStop)
			{
				ResetCursor();
			}
			IsPlaying = false;
		}

		/// <summary>
		/// On restore, we reset the cursor
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized || !_initialValuesStored)
			{
				return;
			}
			ResetCursor();
		}
	}
}
