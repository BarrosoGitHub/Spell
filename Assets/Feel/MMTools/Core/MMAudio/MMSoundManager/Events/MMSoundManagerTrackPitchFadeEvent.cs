using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Tools
{
	/// <summary>
	/// This event will let you order the MMSoundManager to fade an entire track's pitch multiplier towards the specified FinalPitch.
	/// The track pitch multiplies the pitch of every sound playing (or played later) on that track, so it composes with each sound's own pitch.
	///
	/// Example : MMSoundManagerTrackPitchFadeEvent.Trigger(MMSoundManagerTrackPitchFadeEvent.Modes.PlayFade, MMSoundManager.MMSoundManagerTracks.Music, 2f, 0.5f, new MMTweenType(MMTween.MMTweenCurve.EaseInCubic));
	/// will fade the pitch of the music track towards 0.5, over 2 seconds, using an ease in cubic tween 
	/// </summary>
	public partial struct MMSoundManagerTrackPitchFadeEvent
	{
		public enum Modes { PlayFade, StopFade }

		/// whether we are fading a track's pitch, or stopping an existing pitch fade
		public Modes Mode;
		/// the track to fade the pitch of
		public MMSoundManager.MMSoundManagerTracks Track;
		/// if Track is Other, the optional custom track SO to target
		public MMSoundManagerCustomTrackSO CustomTrack;
		/// the duration of the fade, in seconds
		public float FadeDuration;
		/// the final pitch multiplier to fade towards
		public float FinalPitch;
		/// the tween to use when fading
		public MMTweenType FadeTween;
        
		public MMSoundManagerTrackPitchFadeEvent(Modes mode, MMSoundManager.MMSoundManagerTracks track, float fadeDuration, float finalPitch, MMTweenType fadeTween, MMSoundManagerCustomTrackSO customTrack = null)
		{
			Mode = mode;
			Track = track;
			CustomTrack = customTrack;
			FadeDuration = fadeDuration;
			FinalPitch = finalPitch;
			FadeTween = fadeTween;
		}

		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		static MMSoundManagerTrackPitchFadeEvent e;
		public static void Trigger(Modes mode, MMSoundManager.MMSoundManagerTracks track, float fadeDuration, float finalPitch, MMTweenType fadeTween, MMSoundManagerCustomTrackSO customTrack = null)
		{
			e.Mode = mode;
			e.Track = track;
			e.CustomTrack = customTrack;
			e.FadeDuration = fadeDuration;
			e.FinalPitch = finalPitch;
			e.FadeTween = fadeTween;
			MMEventManager.TriggerEvent(e);
		}
	}
}