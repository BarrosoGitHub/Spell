---
name: feel-sound-and-playlist-setup
description: "Use this skill whenever the user wants to set up or configure Feel's audio infrastructure, the MMSoundManager and MMSM Playlist Manager — e.g. 'set up the sound manager', 'where do i put the MMSoundManager', 'make the sound manager persist across scenes', 'use my own audio mixer', 'add a dialogue track', 'volume sliders for music and sfx', 'save audio settings', 'background music playlist', 'shuffle the songs', 'menu music then gameplay music', 'my playlist doesn't play'. Covers placing MMSoundManager, settings assets and AudioMixer routing, custom tracks, audio-settings save/load, volume slider UI, playlist assets, MMSMPlaylistManager setup (persistence, fades, timescale pitch) and controlling playlists by events or feedback. Do NOT use for playing or controlling individual sounds (see feel-sound-manager) or haptics (see feel-haptics). When in doubt whether an audio setup request could involve Feel, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Feel"
  publisher: "More Mountains"
  asset-version: "6.4"
  skill-version: "1.0.1"
  unity: "6000.5+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/particles-effects"
  asset-store-url: "https://assetstore.unity.com/packages/tools/particles-effects/feel-183370"
  documentation-url: "https://feel-docs.moremountains.com/"
  support-url: "https://feel.moremountains.com/"
  last-verified: "2026-11-20"
---

# Set Up the Sound Manager and Playlist Manager

Feel's audio stack has two managers, both in namespace `MoreMountains.Tools`. `MMSoundManager` plays pooled sounds through an AudioMixer, with Master, Music, Sfx and UI tracks plus optional custom tracks; its configuration lives in an `MMSoundManagerSettingsSO` asset. `MMSMPlaylistManager` plays `MMSMPlaylist` assets (ordered lists of songs) through the sound manager, with fades, play orders and chaining. This skill lets an agent install both in a project, configure mixer routing, custom tracks, saved settings and volume UI, build playlists, and wire playlist control.

## When to use this skill

- "set up the sound manager in my project" / "where should the MMSoundManager go?"
- "the sound manager should survive scene changes"
- "use my own AudioMixer with feel" / "add a Dialogue track"
- "music and sfx volume sliders in the options menu"
- "remember volume settings between sessions"
- "set up background music that plays a list of songs, shuffled"
- "menu music playlist, then switch to the gameplay playlist"
- "crossfade between songs" / "music should slow down in slow motion"
- "next/previous song buttons" / "my playlist doesn't play"

Not for:
- Playing, stopping, fading individual sounds, sound feedbacks in an MMF Player, sound IDs → `feel-sound-manager`
- Building feedback sequences → `feel-add-feedbacks`
- Haptics → `feel-haptics`

## Prerequisites

- Feel 6.1, Unity 6000.5+, any render pipeline.
- **Install check:** `System.Type.GetType("MoreMountains.Tools.MMSoundManager, MoreMountains.Tools")` and `System.Type.GetType("MoreMountains.Tools.MMSMPlaylistManager, MoreMountains.Tools")` are both non-null, and the folder `Assets/Feel/MMTools/Core/MMAudio` exists. If not, tell the user Feel is not installed (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) and stop.
- Default assets shipped with Feel: `Assets/Feel/MMTools/Core/MMAudio/MMSoundManager/Settings/MMSoundManagerSettings.asset` (settings) and `MMSoundManagerAudioMixer.mixer` (mixer) in the same folder.
- Scripts need `using MoreMountains.Tools;`.
- Mixer and settings edits change project assets; confirm with the user before modifying Feel's default assets (prefer duplicating them, see below).

## Quick start

A persistent sound manager plus a shuffled background-music playlist that starts on load:

1. In the first scene the game loads (a bootstrap or main menu scene), create an empty root GameObject `MMSoundManager` and add `Add Component > More Mountains > Tools > Audio > MM Sound Manager`. Added through the inspector, its `settingsSo` field is filled automatically with the default `MMSoundManagerSettings` asset (script default reference). Added from code, assign it explicitly (see below).
2. Create a playlist asset: in the Project window, `Create > MoreMountains > Audio > MMSM Playlist`, name it `GameplayMusic`. Set `Track = Music` (default), `PlayOrder = RandomUnique`, `PlayMode = PlayForever` (default), and add one entry to **Songs** per clip (`Name` for readability, `Clip`; leave `Options` at defaults).
3. Create an empty root GameObject `MusicPlayer` and add `MMSMPlaylistManager` (search "MMSM Playlist Manager" in Add Component). Assign `Playlist = GameplayMusic`, tick `PlayOnStart`, and tick `Persistent` if the music must continue across scene loads.
4. Editor-script equivalent:

```csharp
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelAudioSetup
{
    const string DefaultSettingsPath = "Assets/Feel/MMTools/Core/MMAudio/MMSoundManager/Settings/MMSoundManagerSettings.asset";

    public static void SetupAudio(AudioClip[] songs, string playlistPath = "Assets/Audio/GameplayMusic.asset")
    {
        // sound manager (one per game)
        MMSoundManager manager = Object.FindAnyObjectByType<MMSoundManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("MMSoundManager");
            Undo.RegisterCreatedObjectUndo(go, "Create MMSoundManager");
            manager = go.AddComponent<MMSoundManager>();
        }
        if (manager.settingsSo == null)
        {
            manager.settingsSo = AssetDatabase.LoadAssetAtPath<MMSoundManagerSettingsSO>(DefaultSettingsPath);
        }

        // playlist asset (the target folder must already exist)
        MMSMPlaylist playlist = ScriptableObject.CreateInstance<MMSMPlaylist>();
        playlist.Track = MMSoundManager.MMSoundManagerTracks.Music;
        playlist.PlayOrder = MMSMPlaylist.PlayOrders.RandomUnique;
        playlist.Songs = new System.Collections.Generic.List<MMSMPlaylistSong>();
        foreach (AudioClip clip in songs)
        {
            MMSMPlaylistSong song = new MMSMPlaylistSong();
            song.Name = clip.name;
            song.Clip = clip;
            song.Options = MMSoundManagerPlayOptions.Default;
            song.Options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
            playlist.Songs.Add(song);
        }
        AssetDatabase.CreateAsset(playlist, playlistPath);
        AssetDatabase.SaveAssets();

        // playlist manager
        GameObject musicGo = new GameObject("MusicPlayer");
        Undo.RegisterCreatedObjectUndo(musicGo, "Create MusicPlayer");
        MMSMPlaylistManager playlistManager = musicGo.AddComponent<MMSMPlaylistManager>();
        playlistManager.Playlist = playlist;
        playlistManager.PlayOnStart = true;
        playlistManager.Persistent = true;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(playlistManager);
        EditorSceneManager.MarkSceneDirty(musicGo.scene);
    }
}
```

5. Expected result: the scene has exactly one `MMSoundManager` (with `settingsSo` set) and one `MMSMPlaylistManager` referencing a playlist asset whose songs all have clips. In Play mode, music starts on the Music track; the playlist manager's **Status** group shows `CurrentSongName` and `CurrentProgress` advancing, and the sound manager inspector shows a sound playing on Music.

## Workflows

### Workflow: Place the MMSoundManager correctly

**Goal:** exactly one working sound manager for the whole game.

**Steps:**
1. Put it on a root GameObject in the first loaded scene. `MMSoundManager` is an `MMPersistentSingleton`: the first instance calls `DontDestroyOnLoad`, and any later duplicate (for example the same object in another scene) destroys itself. Copies in other scenes are therefore harmless and let those scenes be tested on their own.
2. Make sure `settingsSo` is assigned. Without it, settings-dependent calls (track volume changes, saving, custom tracks) fail.
3. Pool: `AudioSourcePoolSize` (10 by default) pre-creates audio sources; set it above the maximum number of simultaneous sounds. `PoolCanExpand` (true by default) adds sources when the pool runs out.
4. Calls to `MMSoundManager.Instance` create a bare manager on the fly if none exists. Don't rely on that: it has no settings asset.

**Expected result:** in Play mode, after loading a second scene, `Object.FindObjectsByType<MMSoundManager>().Length == 1`.

### Workflow: Use your own settings asset and AudioMixer

**Goal:** project-owned audio configuration that a Feel update won't overwrite.

**Steps:**
1. Duplicate `MMSoundManagerSettings.asset` and `MMSoundManagerAudioMixer.mixer` (select, Ctrl+D) into a project folder such as `Assets/Audio/`. Alternatively create a fresh settings asset with `Create > MoreMountains > Audio > MMSoundManagerSettings`.
2. On the copied settings asset, assign `TargetAudioMixer` to the copied mixer, and `MasterAudioMixerGroup`, `MusicAudioMixerGroup`, `SfxAudioMixerGroup`, `UIAudioMixerGroup` to its groups.
3. The mixer must expose volume parameters named as in **Settings**: `MasterVolumeParameter = "MasterVolume"`, `MusicVolumeParameter = "MusicVolume"`, `SfxVolumeParameter = "SfxVolume"`, `UIVolumeParameter = "UIVolume"` (defaults). The copied mixer already does; a custom mixer must expose parameters with these names, or the names in Settings must be changed to match.
4. Assign the new settings asset to the manager's `settingsSo`.
5. Other systems can route their own `AudioSource`s to the same mixer groups so the track volumes apply to them too.

**Expected result:** `manager.settingsSo` points to the project's asset; changing Music volume through the manager moves the copied mixer's `MusicVolume` parameter.

### Workflow: Add a custom track

**Goal:** a track beyond Master/Music/Sfx/UI (for example Dialogue).

**Steps:**
1. Open the settings asset's `TargetAudioMixer`. Add a group `Dialogue`. With the group selected, right-click **Volume** in the inspector and choose **Expose 'Volume (of Dialogue)' to script**. In the mixer's **Exposed Parameters**, rename it `Dialogue`.
2. `Create > MoreMountains > Audio > MMSoundManagerTrack` → `DialogueTrack`. Set `VolumeParameter = "Dialogue"` and `AudioMixerGroup` to the Dialogue group. `DefaultVolume` (1) and `DefaultOn` (true) set its startup state.
3. Add `DialogueTrack` to the settings asset's `CustomTracks` list.
4. Sounds and playlists use it by setting the track to `Other` and assigning the custom track (`MMSoundManagerPlayOptions.CustomTrack`, or the **Custom Track** field on feedbacks and volume sliders).

**Expected result:** `settingsSo.CustomTracks` contains the track; `MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Other, 0.5f, dialogueTrack)` changes the Dialogue group's volume.

### Workflow: Save and restore audio settings

**Goal:** volumes and mutes persist between sessions.

**Steps:**
1. In the settings asset's **Settings**, `AutoLoad` is on by default (saved settings load at startup) and `AutoSave` is off. Turn on `AutoSave` to save whenever settings change, or save explicitly.
2. Explicit calls: `MMSoundManagerEvent.Trigger(MMSoundManagerEventTypes.SaveSettings)`, `LoadSettings`, `ResetSettings`; or the `Audio > MMSoundManager Save and Load` feedback.
3. Saves go through `MMSaveLoadManager` (folder `MMSoundManager/`), using its default Json method unless an `MMSaveLoadManagerMethod` component or `MMSaveLoadManager.SaveLoadMethod` picks another. During development, `Tools > More Mountains > Delete all saved data` clears them. That deletes every MMTools save file, so ask first.

**Expected result:** after saving a lowered Music volume and restarting Play mode, Music starts at the saved volume.

### Workflow: Build volume and mute controls

**Goal:** an options menu with per-track sliders and mute toggles.

**Steps:**
1. For each UI `Slider` (0–1 range), add the `MMSoundManagerTrackVolumeSlider` component on the same GameObject, set `Track` (or `Track = Other` + `CustomTrack`), keep `MinVolume = 0`, `MaxVolume = 1`.
2. Bind the slider's **On Value Changed (Single)** to `MMSoundManagerTrackVolumeSlider > UpdateVolume` in its **dynamic float** section (that is how the `FeelMMSoundManagerTrackControl` demo wires it). The component does not subscribe by itself.
3. The component keeps the slider in sync with the track. It starts in `Write` mode and switches to `Read` briefly when it catches a mute, unmute or fade event, so the slider follows changes made elsewhere.
4. Mute buttons or toggles: bind to `MMSoundManager` methods such as `MuteMusic` / `UnmuteMusic`, `MuteSfx` / `UnmuteSfx`, `MuteMaster` / `UnmuteMaster`.
5. Editor-script wiring of a slider:

```csharp
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

public static class FeelVolumeSliderSetup
{
    public static void Wire(Slider slider, MMSoundManager.MMSoundManagerTracks track)
    {
        MMSoundManagerTrackVolumeSlider volume = Undo.AddComponent<MMSoundManagerTrackVolumeSlider>(slider.gameObject);
        volume.Track = track;
        Undo.RecordObject(slider, "Wire volume slider");
        UnityEventTools.AddPersistentListener(slider.onValueChanged, volume.UpdateVolume);
        EditorUtility.SetDirty(slider);
    }
}
```

**Expected result:** moving the slider in Play mode changes that track's volume; muting the track elsewhere moves the slider to 0.

### Workflow: Configure a playlist asset

**Goal:** define what plays and in which order.

**Steps:**
1. `Create > MoreMountains > Audio > MMSM Playlist`.
2. Fields: `Track` (Music by default), `PlayOrder` (`Normal`, `ReverseOrder`, `Random`, `RandomUnique`), `RandomizeOrderSeed` (true), `PlayMode` (`PlayForever`, `PlayOnce`, `PlayXTimes` with `MaxAmountOfPlays`, 10 by default), `NextPlaylist` (switches to another playlist when this one ends).
3. **Songs**: each `MMSMPlaylistSong` has `Name` (organizational), `Clip`, and `Options` (an `MMSoundManagerPlayOptions`: volume, pitch, fade and spatial settings per song).

**Expected result:** every song has a `Clip`; the playlist order and repeat behaviour match the request.

### Workflow: Configure the playlist manager

**Goal:** play playlists with the right lifetime, fades and timing.

**Steps:**
1. **Settings**: `Channel` (0 by default, used by events and feedbacks to address this manager), `Playlist`, `PlayOnStart`, `VolumeMultiplier` / `PitchMultiplier` (1), `Persistent` (survive scene loads: the first instance calls `DontDestroyOnLoad` and later duplicates destroy themselves; `AutomaticallyUnparentOnAwake`, true by default, moves it to the root first), `AutoHandleApplicationPause` (true).
2. **Fade**: `FadeIn`, `FadeOut`, `FadeDuration` (1 s) for transitions between songs.
3. **Time**: `BindPitchToTimeScale` maps `Time.timeScale` (`TimescaleRemapFrom`, 0–2 by default) to pitch (`TimescaleRemapTo`, 0.8–1.2), so music slows with slow motion.
4. **Events**: `OnSongStart`, `OnSongEnd`, `OnPause`, `OnStop`, `OnPlaylistChange`, `OnPlaylistEnd` for UI such as "now playing".
5. A `MMSoundManager` must also be in the scene, ideally on its own GameObject.

**Expected result:** in Play mode, **Status** shows the current song, and its **Test Controls** buttons (play, pause, next, previous) work.

### Workflow: Control playlists from gameplay

**Goal:** next/previous buttons, switching between menu and gameplay music.

**Steps:**
1. Direct calls on the manager: `Play()`, `Pause()`, `Stop()`, `StopWithFade()`, `PlayNextSong()`, `PlayPreviousSong()`, `PlaySongAt(index)`, `QueueSongAtIndex(index)`, `SetCurrentSongLoop(bool)`, `ChangePlaylist(playlist)`, `ChangePlaylistAndPlay(playlist)`, `SetVolumeMultiplier(float)`, `SetPitchMultiplier(float)`. These can also be bound to UI buttons.
2. Events (no reference needed; argument = channel):

```csharp
using MoreMountains.Tools;

MMPlaylistPlayEvent.Trigger(0);
MMPlaylistPauseEvent.Trigger(0);
MMPlaylistPlayNextEvent.Trigger(0);
MMPlaylistPlayPreviousEvent.Trigger(0);
MMPlaylistPlayIndexEvent.Trigger(0, 3);
MMPlaylistVolumeMultiplierEvent.Trigger(0, 0.5f, true);
MMPlaylistChangeEvent.Trigger(0, menuPlaylist, true);   // switch playlist and play it
MMPlaylistStopEvent.Trigger(0);
```

3. From an MMF Player: `Audio > MMPlaylist` feedback (`MMF_Playlist`), `Mode` = `Play`, `PlayNext` (default), `PlayPrevious`, `Stop`, `Pause`, `PlaySongAt` (`SongIndex`), `SetVolumeMultiplier`, `ChangePlaylist` (`NewPlaylist`, `ChangePlaylistAndPlay`). Its **Channel** must match the manager's.

**Expected result:** the music responds to the commands; `CurrentSongIndex` updates.

## Verification

- Exactly one `MMSoundManager` in the scene, with `settingsSo != null` and `settingsSo.TargetAudioMixer != null`.
- If custom tracks were requested: `settingsSo.CustomTracks` contains them, each with non-empty `VolumeParameter` and an `AudioMixerGroup`.
- Playlist asset: `AssetDatabase.LoadAssetAtPath<MoreMountains.Tools.MMSMPlaylist>(path)` is not null, `Songs.Count > 0`, every `song.Clip != null`, and `Track`, `PlayOrder`, `PlayMode` match the request.
- Playlist manager: `Object.FindAnyObjectByType<MoreMountains.Tools.MMSMPlaylistManager>()` has `Playlist` assigned, and `PlayOnStart` / `Persistent` as requested.
- Volume sliders: each has `MMSoundManagerTrackVolumeSlider` with the intended `Track`, and `slider.onValueChanged.GetPersistentMethodName(0) == "UpdateVolume"`.
- In Play mode: `MMSoundManager.Instance.HasSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Music)` is true once the playlist starts, and `CurrentSongName` is not empty.
- The Console has no errors.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MMSoundManager` (`settingsSo`, `AudioSourcePoolSize`, `PoolCanExpand`) | Component, persistent singleton | Plays pooled sounds through the mixer. |
| `MMSoundManagerSettingsSO` (`TargetAudioMixer`, `*AudioMixerGroup`, `CustomTracks`, `Settings`) | ScriptableObject | Mixer routing and saved settings. |
| `MMSoundManagerSettings` (`AutoLoad`, `AutoSave`, `*VolumeParameter`) | Serializable class | Track volumes, mutes, save behaviour. |
| `MMSoundManagerCustomTrackSO` (`VolumeParameter`, `AudioMixerGroup`) | ScriptableObject | A custom track. |
| `MMSoundManagerTrackVolumeSlider.UpdateVolume(float)` | Component method | Slider → track volume. |
| `MMSoundManagerEvent.Trigger(MMSoundManagerEventTypes)` | Static event | Save, load, reset settings. |
| `MMSMPlaylist` (`Track`, `PlayOrder`, `PlayMode`, `NextPlaylist`, `Songs`) | ScriptableObject | A playlist. |
| `MMSMPlaylistManager` (`Channel`, `Playlist`, `PlayOnStart`, `Persistent`, fades, `BindPitchToTimeScale`) | Component | Plays playlists. |
| `MMPlaylistPlayEvent` / `...PlayNextEvent` / `...ChangeEvent` / ... `.Trigger(channel, ...)` | Static events | Control playlist managers by channel. |
| `MMF_Playlist` (`Audio/MMPlaylist`) | Feedback | Control playlists from an MMF Player. |

## Common issues

- **NullReferenceException from MMSoundManager when changing track volumes or saving** → `settingsSo` is not assigned (typical when the component was added from code) → assign a settings asset.
- **Music stops when a new scene loads** → the playlist manager isn't `Persistent` → tick `Persistent` on the manager (songs it plays are then marked persistent too), and keep the sound manager in the first scene.
- **Two songs play at once after a scene change** → each scene has its own non-persistent playlist manager → make one persistent, or remove the duplicates.
- **Playlist manager plays nothing** → no `Playlist`, `PlayOnStart` off with no play call, empty `Songs` or null clips, Music track muted or at 0, or a channel mismatch with the events/feedback → check each.
- **Volume slider does nothing** → `onValueChanged` isn't bound to `UpdateVolume`, or `Track` is wrong → bind it as a dynamic float.
- **Custom track volume never changes** → the mixer parameter isn't exposed, or its name differs from `VolumeParameter` → expose it and match the names exactly.
- **Everything is silent after testing** → saved muted settings are reloaded (`AutoLoad`) → unmute, `ResetSettings`, or delete saved data (with permission).
- **Saved audio settings are ignored after updating Feel, with an `[MMSaveLoadManager]` warning** → the file was written by an older version (binary default, BinaryFormatter format), which can't be read anymore → save again (or `ResetSettings`) to write a new file.
- **Feel update reverted my mixer changes** → Feel's default assets were edited → use duplicated settings and mixer assets in a project folder.

## Boundaries

- The managers drive Unity's AudioMixer; they don't integrate with FMOD or Wwise.
- Each playlist manager plays one playlist at a time; give simultaneous managers (for example music and ambience) distinct channels so events address the right one.
- Playlists play `AudioClip`s; for streaming or adaptive music systems use dedicated tools.
- Individual sound playback, sound feedbacks and runtime sound control are covered by `feel-sound-manager`.
