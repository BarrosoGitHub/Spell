using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Tools
{
	public enum MMSoundManagerAllSoundsControlEventTypes
	{
		Pause, Play, Stop, Free, FreeAllButPersistent, FreeAllLooping
	}
    
	/// <summary>
	/// This event will let you pause/play/stop/free all sounds playing through the MMSoundManager at once
	///
	/// Example : MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Stop);
	/// will stop all sounds playing at once
	/// </summary>
	public partial struct MMSoundManagerAllSoundsControlEvent
	{
		public MMSoundManagerAllSoundsControlEventTypes EventType;
        
		public MMSoundManagerAllSoundsControlEvent(MMSoundManagerAllSoundsControlEventTypes eventType)
		{
			EventType = eventType;
		}

		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		static MMSoundManagerAllSoundsControlEvent e;
		public static void Trigger(MMSoundManagerAllSoundsControlEventTypes eventType)
		{
			e.EventType = eventType;
			MMEventManager.TriggerEvent(e);
		}
	}
}