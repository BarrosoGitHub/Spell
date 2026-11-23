using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Tools
{
	public enum MMSoundManagerTrackEventTypes
	{
		MuteTrack,
		UnmuteTrack,
		SetVolumeTrack,
		PlayTrack,
		PauseTrack,
		StopTrack,
		FreeTrack,
		SetPitchTrack
	}
    
	/// <summary>
	/// This feedback will let you mute, unmute, play, pause, stop, free or set the volume or pitch of a selected track
	///
	/// Example :  MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.PauseTrack,MMSoundManager.MMSoundManagerTracks.UI);
	/// will pause the entire UI track
	///
	/// Example :  MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetPitchTrack, MMSoundManager.MMSoundManagerTracks.Sfx, pitch: 0.5f);
	/// will set the pitch multiplier of the entire Sfx track to 0.5 (every sound on it will play at half its own pitch)
	/// </summary>
	public partial struct MMSoundManagerTrackEvent
	{
		/// the order to pass to the track
		public MMSoundManagerTrackEventTypes TrackEventType;
		/// the track to pass the order to
		public MMSoundManager.MMSoundManagerTracks Track;
		/// if Track is Other, the optional custom track SO to target
		public MMSoundManagerCustomTrackSO CustomTrack;
		/// if in SetVolume mode, the volume to which to set the track to
		public float Volume;
		/// if in SetPitch mode, the pitch multiplier to apply to the track (1 is the normal pitch)
		public float Pitch;
        
		public MMSoundManagerTrackEvent(MMSoundManagerTrackEventTypes trackEventType, MMSoundManager.MMSoundManagerTracks track = MMSoundManager.MMSoundManagerTracks.Master, float volume = 1f, MMSoundManagerCustomTrackSO customTrack = null, float pitch = 1f)
		{
			TrackEventType = trackEventType;
			Track = track;
			CustomTrack = customTrack;
			Volume = volume;
			Pitch = pitch;
		}

		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		static MMSoundManagerTrackEvent e;
		public static void Trigger(MMSoundManagerTrackEventTypes trackEventType, MMSoundManager.MMSoundManagerTracks track = MMSoundManager.MMSoundManagerTracks.Master, float volume = 1f, MMSoundManagerCustomTrackSO customTrack = null, float pitch = 1f)
		{
			e.TrackEventType = trackEventType;
			e.Track = track;
			e.CustomTrack = customTrack;
			e.Volume = volume;
			e.Pitch = pitch;
			MMEventManager.TriggerEvent(e);
		}
	}
}