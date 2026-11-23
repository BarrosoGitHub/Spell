---
name: feel-sound-manager
description: "Use this skill whenever the user wants to play or control sounds at runtime with Feel's MMSoundManager — e.g. 'play a sound when I jump', 'random pitch footsteps', 'pick a random hit sound', 'loop this ambience', 'fade the music out when the boss appears', 'play a 3D sound at the explosion', 'mute other tracks while this line plays', 'pause all sounds in the pause menu', 'slow down the pitch of all sound effects in slow motion', 'my sound feedback doesn't play'. Covers the MMSoundManager Sound and simple Sound feedbacks, play options, 2D/3D sounds, sound IDs, sound and track control and fade feedbacks, track pitch (set and fade), solo modes,and the event API (MMSoundManagerSoundPlayEvent and siblings). Do NOT use for installing the sound manager, mixers, custom tracks, volume slider UI, saved audio settings or playlists (see feel-sound-and-playlist-setup), for haptics (see feel-haptics) or for non-audio feedbacks (see feel-add-feedbacks). When in doubt whether an audio request could involve Feel, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Feel"
  publisher: "More Mountains"
  asset-version: "6.4"
  skill-version: "1.1.0"
  unity: "6000.5+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/particles-effects"
  asset-store-url: "https://assetstore.unity.com/packages/tools/particles-effects/feel-183370"
  documentation-url: "https://feel-docs.moremountains.com/"
  support-url: "https://feel.moremountains.com/"
  last-verified: "2026-11-20"
---

# Play and Control Sound with MMSoundManager

`MMSoundManager` (namespace `MoreMountains.Tools`) is Feel's audio manager. It plays sounds on pooled `AudioSource`s, routes them through an AudioMixer with four built-in tracks (Master, Music, Sfx, UI, plus custom tracks), and can be driven three ways: from MMF Player feedbacks (`Audio > MMSoundManager *`), through one-line events, or by direct calls on `MMSoundManager.Instance`. This skill lets an agent play one-shot, looping and 3D sounds, and control them by ID, by track or all at once, from feedbacks or code. Installing and configuring the managers themselves is covered by `feel-sound-and-playlist-setup`.

## When to use this skill

- "play a jump sound" / "random pitch on footsteps" / "pick a random hit sound"
- "loop the background music" / "play music on the music track"
- "pause all sounds when the pause menu opens" / "mute the music track from code"
- "fade the music out over 2 seconds" / "fade this sound in"
- "lower the pitch of every sound effect during slow motion" / "pitch the whole music track down when the player is hurt"
- "play the explosion sound at the explosion position, in 3D"
- "while the boss line plays, mute the other tracks" (solo)
- "stop that looping engine sound when the car is destroyed"
- "my MMSM sound feedback plays nothing"

Not for:
- Adding/configuring the MMSoundManager, mixer routing, custom tracks, volume slider UI, saving audio settings, playlists → `feel-sound-and-playlist-setup`
- Vibration / haptics → `feel-haptics`
- Sequencing non-audio feedbacks → `feel-add-feedbacks`
- Pitch/volume springs on an `AudioSource` → `feel-springs` (`MMSpringAudioSourcePitch`, `MMSpringAudioSourceVolume`)

## Prerequisites

- Feel 6.1, Unity 6000.5+, any render pipeline.
- **Install check:** `System.Type.GetType("MoreMountains.Tools.MMSoundManager, MoreMountains.Tools")` is not null, and the folder `Assets/Feel/MMTools/Core/MMAudio/MMSoundManager` exists. If not, tell the user Feel is not installed (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) and stop.
- Scripts need `using MoreMountains.Tools;` (and `using MoreMountains.Feedbacks;` for feedback classes).
- The default settings asset is `Assets/Feel/MMTools/Core/MMAudio/MMSoundManager/Settings/MMSoundManagerSettings.asset`, which uses the mixer `MMSoundManagerAudioMixer.mixer` in the same folder.

## Quick start

A jump sound with slight random pitch, through the manager:

1. Create an empty GameObject at the scene root named `MMSoundManager` and add `Add Component > More Mountains > Tools > Audio > MM Sound Manager`. Check that its **Settings** slot (`settingsSo`) references `MMSoundManagerSettings`; if it is empty, assign the default asset above. If the project has no sound manager yet, or needs more than this minimal setup (persistence across scenes, custom mixer), follow `feel-sound-and-playlist-setup` first.
2. On the jump's MMF Player (see `feel-add-feedbacks`), **Add new feedback > Audio > MMSoundManager Sound** (`MMF_MMSoundManagerSound`). Set **Sfx** to the clip, `MinPitch = 0.9`, `MaxPitch = 1.1`. The track defaults to `Sfx`. In edit mode, the feedback's test buttons let you audition the clip.
3. Editor-script equivalent:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelSoundQuickStart
{
    public static void AddJumpSound(MMF_Player player, AudioClip clip)
    {
        if (Object.FindAnyObjectByType<MMSoundManager>() == null)
        {
            GameObject sm = new GameObject("MMSoundManager");
            Undo.RegisterCreatedObjectUndo(sm, "Create MMSoundManager");
            sm.AddComponent<MMSoundManager>();
        }
        Undo.RecordObject(player, "Add MMSM Sound");
        MMF_MMSoundManagerSound sound = (MMF_MMSoundManagerSound)player.AddFeedback(typeof(MMF_MMSoundManagerSound));
        sound.Sfx = clip;
        sound.MinPitch = 0.9f;
        sound.MaxPitch = 1.1f;
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
    }
}
```

(The feedback's **Automatic Shaker Setup** button does the same `MMSoundManager` creation.)

4. Expected result: the scene contains exactly one `MMSoundManager`; the player has an `MMF_MMSoundManagerSound` with `Sfx` assigned; in Play mode, playing the player plays the clip on the Sfx track at a slightly different pitch each time, and the manager inspector's Sfx track shows a playing sound.

## Workflows

### Workflow: Play sounds from code

**Goal:** play a sound without an MMF Player.

**Steps:**
1. Simple call (2D, Sfx track, at a position):

```csharp
using MoreMountains.Tools;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public AudioClip ExplosionSfx;

    public void Explode()
    {
        MMSoundManagerSoundPlayEvent.Trigger(ExplosionSfx, MMSoundManager.MMSoundManagerTracks.Sfx, transform.position);
    }
}
```

2. With options (start from the defaults, override what you need):

```csharp
MMSoundManagerPlayOptions options = MMSoundManagerPlayOptions.Default; // Sfx track, volume 1, pitch 1, 2D
options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
options.Loop = true;
options.Volume = 0.5f;
options.ID = 50;                // lets you control this sound later
options.Persistent = true;      // survives scene loads
AudioSource source = MMSoundManagerSoundPlayEvent.Trigger(MusicClip, options);
```

3. Prefer events over `MMSoundManager.Instance` calls: events don't throw when no manager is listening. `MMSoundManager.Instance` auto-creates a manager if none exists.

**Expected result:** the clip plays; with `Loop = true` it repeats until stopped.

### Workflow: Play a 3D sound

**Goal:** positional audio that attenuates with distance.

**Steps:**
1. In the MMSoundManager Sound feedback (or in `MMSoundManagerPlayOptions`), set **Spatial Blend** to 1. Optionally set `MinDistance` / `MaxDistance` (1 and 500 by default) and `RolloffMode` (Logarithmic by default).
2. The sound plays at the position passed to `PlayFeedbacks(position, intensity)` (or at the player's position), or follows a transform via **Attach To Transform**.
3. Make sure an `AudioListener` exists (usually on the main camera).

**Expected result:** volume and panning change with the listener's distance and direction.

### Workflow: Control a sound by ID

**Goal:** stop, pause, fade or free a specific playing sound.

**Steps:**
1. Give the sound a non-zero, unique **ID** (feedback field `ID`, or `options.ID`).
2. Feedbacks: `Audio > MMSoundManager Sound Control` (pause / resume / stop / free) and `Audio > MMSoundManager Sound Fade`, with the same ID.
3. Code:

```csharp
MMSoundManagerSoundControlEvent.Trigger(MMSoundManagerSoundControlEventTypes.Stop, 50);
MMSoundManagerSoundFadeEvent.Trigger(MMSoundManagerSoundFadeEvent.Modes.PlayFade, 50, 2f, 0f,
    new MMTweenType(MMTween.MMTweenCurve.EaseInCubic)); // fade sound 50 to 0 over 2s
MMSoundManagerSoundControlEvent.Trigger(MMSoundManagerSoundControlEventTypes.Free, 50); // return its AudioSource to the pool
```

**Expected result:** only sounds with that ID react.

### Workflow: Track volume, mute and pause

**Goal:** mute, set volume, pause or stop whole tracks at runtime (pause menus, cutscenes, game over).

**Steps:**
1. No-code mute: on a UI Button's **On Click ()**, drag the `MMSoundManager` object and pick `MMSoundManager > MuteMaster` (or `UnmuteMaster`, `MuteMusic`, `MuteSfx`, `MuteUI`, ...).
2. Volume from code or UnityEvents: `SetVolumeMusic(float)`, `SetVolumeSfx(float)`, `SetVolumeUI(float)`, `SetVolumeMaster(float)`. For options-menu sliders that stay in sync with the tracks, use `MMSoundManagerTrackVolumeSlider` as described in `feel-sound-and-playlist-setup`.
3. Feedbacks: `Audio > MMSoundManager Track Control` (mute, unmute, set volume, play, pause, stop, free a track), `Audio > MMSoundManager Track Fade`, `Audio > MMSoundManager All Sounds Control`.
4. Code:

```csharp
MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.PauseTrack, MMSoundManager.MMSoundManagerTracks.Music);
MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Sfx, 0.5f);
MMSoundManagerTrackFadeEvent.Trigger(MMSoundManagerTrackFadeEvent.Modes.PlayFade, MMSoundManager.MMSoundManagerTracks.Music,
    2f, 0f, new MMTweenType(MMTween.MMTweenCurve.EaseInCubic));
MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Stop);
```

5. Check activity: `MMSoundManager.Instance.HasSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Sfx)`.

**Expected result:** the chosen track mutes, changes volume or pauses; other tracks are unaffected.

### Workflow: Track pitch

**Goal:** change the pitch of every sound on a track at once (slow motion, underwater, hurt state, tape-stop), instantly or over time.

**Steps:**
1. Track pitch is a **multiplier** on top of each sound's own pitch: final pitch = sound pitch × track pitch × Master pitch (× custom track pitch for `Other` sounds with a custom track). It applies to sounds already playing on the track and to sounds played on it later. The Master track's pitch affects every sound. It is clamped to `MMSoundManagerSettings._minimalTrackPitch` (0.01) – `_maxTrackPitch` (3), defaults to 1, is runtime only (not saved with the sound settings) and resets when the manager initializes. It does not use the AudioMixer, so it works with any mixer.
2. Feedbacks: `Audio > MMSoundManager Track Control` with `ControlMode = SetPitch` and `Pitch` (instant), or `Audio > MMSoundManager Track Pitch Fade` (`MMF_MMSoundManagerTrackPitchFade`: `Track`, `CustomTrack`, `FadeDuration` 1, `FinalPitch` 0.5, `FadeTween`) to tween from the current track pitch. Stopping the pitch fade feedback stops the fade where it is. To return to normal, play a second pitch fade (or Track Control) with a value of 1.
3. UnityEvents: `SetPitchMusic(float)`, `SetPitchSfx(float)`, `SetPitchUI(float)`, `SetPitchMaster(float)`.
4. Code:

```csharp
MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetPitchTrack, MMSoundManager.MMSoundManagerTracks.Sfx, pitch: 0.5f);
MMSoundManagerTrackPitchFadeEvent.Trigger(MMSoundManagerTrackPitchFadeEvent.Modes.PlayFade, MMSoundManager.MMSoundManagerTracks.Music,
    1.5f, 0.7f, new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic)); // fade the music track's pitch to 0.7 over 1.5s
MMSoundManagerTrackPitchFadeEvent.Trigger(MMSoundManagerTrackPitchFadeEvent.Modes.StopFade, MMSoundManager.MMSoundManagerTracks.Music, 0f, 1f, null);
// direct calls: SetTrackPitch(track, pitch), GetTrackPitch(track), FadeTrackPitch(track, duration, initialPitch, finalPitch, tween),
// StopFadeTrackPitch(track), ResetTrackPitch(track), ResetAllTrackPitches(), all with MMSoundManagerCustomTrackSO overloads
```

5. To change the pitch of a single sound played by the manager without losing its track multiplier, call `MMSoundManager.Instance.SetSoundPitch(audioSource, newPitch)` rather than setting `AudioSource.pitch` (the next track pitch change recomputes `AudioSource.pitch` from the sound's base pitch).
6. In Play mode, the MMSoundManager inspector shows a **Pitch** slider under each track's volume slider.

**Expected result:** every sound on the track (all sounds for Master) changes pitch together, relative to its own pitch; other tracks are unaffected.

### Workflow: Use the simple Sound feedback instead

**Goal:** play a clip without a sound manager (prototype, isolated object).

**Steps:** add `Audio > Sound` (`MMF_Sound`). `PlayMethod` defaults to `Event`, which needs an `MMSoundManager` to catch it; set it to `Cached` (AudioSource created at init), `OnDemand` (created on play) or `Pool` to play without a manager. Prefer `MMSoundManager Sound` for anything beyond prototypes: it has tracks, IDs, fades and solo modes.

**Expected result:** the clip plays when the player plays.

## Verification

- Exactly one `MMSoundManager` in the scene: `Object.FindObjectsByType<MoreMountains.Tools.MMSoundManager>().Length == 1`, and its `settingsSo != null`.
- The feedback exists with a clip: `player.GetFeedbackOfType<MMF_MMSoundManagerSound>().Sfx != null` (or `RandomSfx` has entries).
- The track is as intended: `sound.MmSoundManagerTrack == MMSoundManager.MMSoundManagerTracks.Sfx` (or the chosen track).
- In Play mode, right after playing, `MMSoundManager.Instance.HasSoundsPlaying(<track>)` is true; for a looping sound it stays true.
- For UI mute buttons, the button's `onClick.GetPersistentMethodName(0)` is the expected method (for example `MuteMaster`).
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MoreMountains.Tools.MMSoundManager` | Component, persistent singleton (`Instance`) | Plays and manages pooled sounds and tracks. |
| `MMSoundManager.MMSoundManagerTracks` | Enum | `Sfx`, `Music`, `UI`, `Master`, `Other` (custom). |
| `MMSoundManagerSoundPlayEvent.Trigger(clip, options)` / `(clip, track, position, ...)` | Static event | Play a sound, returns its `AudioSource`. |
| `MMSoundManagerPlayOptions.Default` | Struct | Default play options to override. |
| `MMSoundManagerSoundControlEvent.Trigger(type, id)` | Static event | Pause/Resume/Stop/Free a sound by ID. |
| `MMSoundManagerSoundFadeEvent.Trigger(mode, id, duration, volume, tween)` | Static event | Fade one sound. |
| `MMSoundManagerTrackEvent.Trigger(type, track, volume)` | Static event | Mute/unmute/volume/play/pause/stop/free a track. |
| `MMSoundManagerTrackFadeEvent.Trigger(mode, track, duration, volume, tween)` | Static event | Fade a track. |
| `MMSoundManagerTrackEvent.Trigger(SetPitchTrack, track, pitch: p)` | Static event | Set a track's pitch multiplier. |
| `MMSoundManagerTrackPitchFadeEvent.Trigger(mode, track, duration, pitch, tween)` | Static event | Fade (or stop fading) a track's pitch multiplier. |
| `MMSoundManager.Instance.SetTrackPitch / GetTrackPitch / FadeTrackPitch / ResetAllTrackPitches` | Methods | Track pitch from code (built-in and custom tracks). |
| `MMSoundManagerAllSoundsControlEvent.Trigger(type)` | Static event | Control all sounds. |
| `MMF_MMSoundManagerSound` (`Audio/MMSoundManager Sound`) | Feedback | Play a sound from an MMF Player. |
| `MMF_MMSoundManagerTrackPitchFade` (`Audio/MMSoundManager Track Pitch Fade`) | Feedback | Fade a track's pitch from an MMF Player. |

## Common issues

- **Sound feedback plays nothing** → no clip assigned, the track is muted or at volume 0, or the clip is set on the wrong source mode (`Source = AudioResource` with only `Sfx` filled) → assign the clip for the selected `Source`, check track volumes in the MMSoundManager inspector.
- **Everything is silent after testing a mute button** → settings were saved while muted and `AutoLoad` restores them → unmute (`UnmuteMaster`), trigger `ResetSettings`, or delete saved data with `Tools > More Mountains > Delete all saved data` (ask first; it deletes all MMTools save files).
- **Two sound managers / duplicated sounds after loading a scene** → the manager is a persistent singleton and each scene has its own copy → keep a single manager in a bootstrap scene, or accept that duplicates destroy themselves.
- **Simple `Sound` feedback is silent** → `PlayMethod = Event` with no manager listening → add an `MMSoundManager`, or switch to `Cached`.
- **Sounds cut off when many play at once** → the audio source pool is too small (`AudioSourcePoolSize = 10` by default) → raise it on the manager; use `DoNotAutoRecycleIfNotDonePlaying` for sounds that must finish.
- **Same clip stacks too loudly** → set `DoNotPlayIfClipAlreadyPlaying`, or lower `MaximumConcurrentInstances` (3 by default) on the MMSoundManager Sound feedback.
- **Non-looping sounds get cut off after lowering a track's pitch** → the manager schedules a pooled source's recycling when the sound starts, from the pitch at that moment; lowering the pitch afterwards makes the sound outlast that schedule → set `DoNotAutoRecycleIfNotDonePlaying = true` on long one-shots that may be slowed mid-play (sounds started while the track pitch is already low are scheduled correctly).
- **Track pitch change is overridden on one sound / a sound loses its track pitch** → something sets `AudioSource.pitch` directly on a managed source → use `MMSoundManager.Instance.SetSoundPitch(source, pitch)`.
- **A sound stops on scene change** →it isn't persistent → set `Persistent = true` on the feedback or in `MMSoundManagerPlayOptions` (for playlists, see `feel-sound-and-playlist-setup`).

## Boundaries

- MMSoundManager is optional and coexists with other audio systems (FMOD, Wwise or plain `AudioSource`s); it does not drive middleware.
- It uses Unity's `AudioMixer`; mixer effects, snapshots and routing are Unity features configured in the mixer asset.
- Audio filters on individual sources (`Audio > Audio Filter *` feedbacks) and AudioSource property feedbacks don't go through the manager; see `feel-add-feedbacks`.
- Haptics synced to audio (Nice Vibrations clips converted from audio) are covered by `feel-haptics`.
