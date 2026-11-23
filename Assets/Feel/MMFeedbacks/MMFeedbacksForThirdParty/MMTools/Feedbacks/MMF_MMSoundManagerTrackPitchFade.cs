using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you fade the pitch of all the sounds on a specific track at once (master, UI, music, sfx, or a custom track).
	/// The track pitch is a multiplier applied on top of each sound's own pitch, to sounds currently playing on that track as well as to sounds played on it later.
	/// The Master track's pitch impacts all sounds. You will need a MMSoundManager in your scene for this to work.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackPath("Audio/MMSoundManager Track Pitch Fade")]
	[FeedbackHelp("This feedback will let you fade the pitch of all the sounds on a specific track at once (master, UI, music, sfx, or a custom track). " +
	              "The track pitch is a multiplier applied on top of each sound's own pitch, to sounds currently playing on that track as well as to sounds played on it later. " +
	              "The Master track's pitch impacts all sounds. Set a fade duration of 0 to set the pitch instantly. You will need a MMSoundManager in your scene for this to work.")]
	public partial class MMF_MMSoundManagerTrackPitchFade : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SoundsColor; } }
		public override string RequiredTargetText { get { return Track.ToString();  } }
		#endif

		/// the duration of this feedback is the duration of the fade
		public override float FeedbackDuration { get { return FadeDuration; } }

		[MMFInspectorGroup("MMSoundManager Track Pitch Fade", true, 30)]
		/// the track to fade the pitch on
		[Tooltip("the track to fade the pitch on")]
		public MMSoundManager.MMSoundManagerTracks Track;
		/// if Track is set to Other, the custom track SO to target
		[Tooltip("if Track is set to Other, the custom track SO to target")]
		[MMFEnumCondition("Track", (int)MMSoundManager.MMSoundManagerTracks.Other)]
		public MMSoundManagerCustomTrackSO CustomTrack;
		/// the duration of the fade, in seconds
		[Tooltip("the duration of the fade, in seconds")]
		public float FadeDuration = 1f;
		/// the pitch multiplier to reach at the end of the fade (1 is the normal pitch)
		[Tooltip("the pitch multiplier to reach at the end of the fade (1 is the normal pitch)")]
		[Range(MMSoundManagerSettings._minimalTrackPitch, MMSoundManagerSettings._maxTrackPitch)]
		public float FinalPitch = 0.5f;
		/// the tween to operate the fade on
		[Tooltip("the tween to operate the fade on")]
		public MMTweenType FadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutQuartic);

		/// <summary>
		/// On Play, triggers a pitch fade event, meant to be caught by the MMSoundManager
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			MMSoundManagerTrackPitchFadeEvent.Trigger(MMSoundManagerTrackPitchFadeEvent.Modes.PlayFade, Track, FadeDuration, FinalPitch, FadeTween, CustomTrack);
		}

		/// <summary>
		/// On stop, we stop our pitch fade via a pitch fade event
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			MMSoundManagerTrackPitchFadeEvent.Trigger(MMSoundManagerTrackPitchFadeEvent.Modes.StopFade, Track, FadeDuration, FinalPitch, FadeTween, CustomTrack);
		}
	}
}