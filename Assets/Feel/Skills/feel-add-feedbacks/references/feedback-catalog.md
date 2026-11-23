# Feel feedback catalog (Feel 6.1)

Every feedback that can be added to an `MMF_Player`, as declared by its `[FeedbackPath]` attribute. Generated from the Feel 6.1 source.

Read this file when you need to pick a feedback for a request, find the C# class to pass to `MMF_Player.AddFeedback(typeof(...))`, or understand why a feedback is missing from the **Add new feedback** dropdown.

- **Menu path** is what appears in the MMF Player's **Add new feedback** dropdown (and what the search field matches).
- **Class** is the type to use in code. Add `using MoreMountains.Feedbacks;` and, for classes in `MoreMountains.FeedbacksForThirdParty`, `using MoreMountains.FeedbacksForThirdParty;`.
- **Requires** lists the scripting define that must be present for the feedback to exist. `MM_*` defines are set automatically by the `MoreMountains.Tools` assembly definition when the matching package is installed (`MM_URP` = Universal RP, `MM_HDRP` = High Definition RP, `MM_POSTPROCESSING` = Post Processing v2, `MM_CINEMACHINE`/`MM_CINEMACHINE3` = Cinemachine 2/3, `MM_UGUI2` = uGUI 2.x which includes TextMesh Pro, `MM_UI` = uGUI, `MM_VISUALEFFECTGRAPH` = Visual Effect Graph, `MM_PHYSICS2D` = Physics 2D module). `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` is added to the Player scripting defines automatically by Nice Vibrations.

To print the list actually available in the current project (respecting installed packages), run `Tools > More Mountains > MMFeedbacks > Output MMF_Feedbacks list`; it logs the list to the Console.


## Animation

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Animation/Animation Crossfade | `MMF_AnimationCrossfade` | `MoreMountains.Feedbacks` | none |
| Animation/Animation Parameter | `MMF_Animation` | `MoreMountains.Feedbacks` | none |
| Animation/Animator Play State | `MMF_AnimatorPlayState` | `MoreMountains.Feedbacks` | none |
| Animation/Animator Speed | `MMF_AnimatorSpeed` | `MoreMountains.Feedbacks` | none |
| Animation/Sprite Sheet Animation | `MMF_SpriteSheetAnimation` | `MoreMountains.Feedbacks` | none |

## Audio

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Audio/Audio Filter Distortion | `MMF_AudioFilterDistortion` | `MoreMountains.Feedbacks` | none |
| Audio/Audio Filter Echo | `MMF_AudioFilterEcho` | `MoreMountains.Feedbacks` | none |
| Audio/Audio Filter High Pass | `MMF_AudioFilterHighPass` | `MoreMountains.Feedbacks` | none |
| Audio/Audio Filter Low Pass | `MMF_AudioFilterLowPass` | `MoreMountains.Feedbacks` | none |
| Audio/Audio Filter Reverb | `MMF_AudioFilterReverb` | `MoreMountains.Feedbacks` | none |
| Audio/AudioMixer Snapshot Transition | `MMF_AudioMixerSnapshotTransition` | `MoreMountains.Feedbacks` | none |
| Audio/AudioSource | `MMF_AudioSource` | `MoreMountains.Feedbacks` | none |
| Audio/AudioSource Pitch | `MMF_AudioSourcePitch` | `MoreMountains.Feedbacks` | none |
| Audio/AudioSource Stereo Pan | `MMF_AudioSourceStereoPan` | `MoreMountains.Feedbacks` | none |
| Audio/AudioSource Volume | `MMF_AudioSourceVolume` | `MoreMountains.Feedbacks` | none |
| Audio/MMPlaylist | `MMF_Playlist` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager All Sounds Control | `MMF_MMSoundManagerAllSoundsControl` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Save and Load | `MMF_MMSoundManagerSaveLoad` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Sound | `MMF_MMSoundManagerSound` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Sound Control | `MMF_MMSoundManagerSoundControl` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Sound Fade | `MMF_MMSoundManagerSoundFade` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Track Control | `MMF_MMSoundManagerTrackControl` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Track Fade | `MMF_MMSoundManagerTrackFade` | `MoreMountains.Feedbacks` | none |
| Audio/MMSoundManager Track Pitch Fade | `MMF_MMSoundManagerTrackPitchFade` | `MoreMountains.Feedbacks` | none |
| Audio/Sound | `MMF_Sound` | `MoreMountains.Feedbacks` | none |

## Camera

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Camera/Camera Shake | `MMF_CameraShake` | `MoreMountains.Feedbacks` | none |
| Camera/Camera Zoom | `MMF_CameraZoom` | `MoreMountains.Feedbacks` | none |
| Camera/Cinemachine Dutch | `MMF_CinemachineCameraDutch` | `MoreMountains.Feedbacks` | `MM_CINEMACHINE || MM_CINEMACHINE3` |
| Camera/Cinemachine Impulse | `MMF_CinemachineImpulse` | `MoreMountains.FeedbacksForThirdParty` | `MM_CINEMACHINE || MM_CINEMACHINE3` |
| Camera/Cinemachine Impulse Clear | `MMF_CinemachineImpulseClear` | `MoreMountains.FeedbacksForThirdParty` | `MM_CINEMACHINE || MM_CINEMACHINE3` |
| Camera/Cinemachine Impulse Source | `MMF_CinemachineImpulseSource` | `MoreMountains.FeedbacksForThirdParty` | `MM_CINEMACHINE || MM_CINEMACHINE3` |
| Camera/Cinemachine Transition | `MMF_CinemachineTransition` | `MoreMountains.FeedbacksForThirdParty` | `MM_CINEMACHINE || MM_CINEMACHINE3` |
| Camera/Clipping Planes | `MMF_CameraClippingPlanes` | `MoreMountains.Feedbacks` | none |
| Camera/Fade | `MMF_Fade` | `MoreMountains.Feedbacks` | `MM_UI` |
| Camera/Field of View | `MMF_CameraFieldOfView` | `MoreMountains.Feedbacks` | none |
| Camera/Flash | `MMF_Flash` | `MoreMountains.Feedbacks` | `MM_UI` |
| Camera/Orthographic Size | `MMF_CameraOrthographicSize` | `MoreMountains.Feedbacks` | none |

## Debug

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Debug/Break | `MMF_DebugBreak` | `MoreMountains.Feedbacks` | none |
| Debug/Comment | `MMF_DebugComment` | `MoreMountains.Feedbacks` | none |
| Debug/Log | `MMF_DebugLog` | `MoreMountains.Feedbacks` | none |

## Events

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Events/MMGameEvent | `MMF_MMGameEvent` | `MoreMountains.Feedbacks` | none |
| Events/Random Unity Events | `MMF_RandomEvents` | `MoreMountains.Feedbacks` | none |
| Events/Unity Events | `MMF_Events` | `MoreMountains.Feedbacks` | none |

## Feedbacks

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Feedbacks/Feedbacks Player | `MMF_Feedbacks` | `MoreMountains.Feedbacks` | none |
| Feedbacks/MMF Player Chain | `MMF_PlayerChain` | `MoreMountains.Feedbacks` | none |
| Feedbacks/MMF Player Control | `MMF_PlayerControl` | `MoreMountains.Feedbacks` | none |
| Feedbacks/MMF Reference Holder | `MMF_ReferenceHolder` | `MoreMountains.Feedbacks` | none |

## GameObject

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| GameObject/Broadcast | `MMF_Broadcast` | `MoreMountains.Feedbacks` | none |
| GameObject/Collider | `MMF_Collider` | `MoreMountains.Feedbacks` | none |
| GameObject/Collider2D | `MMF_Collider2D` | `MoreMountains.Feedbacks` | `MM_PHYSICS2D` |
| GameObject/Destroy | `MMF_Destroy` | `MoreMountains.Feedbacks` | none |
| GameObject/Enable Behaviour | `MMF_Enable` | `MoreMountains.Feedbacks` | none |
| GameObject/FloatController | `MMF_FloatController` | `MoreMountains.Feedbacks` | none |
| GameObject/Instantiate Object | `MMF_InstantiateObject` | `MoreMountains.Feedbacks` | none |
| GameObject/Layer | `MMF_Layer` | `MoreMountains.Feedbacks` | `MM_UI` |
| GameObject/MMRadioSignal | `MMF_RadioSignal` | `MoreMountains.Feedbacks` | none |
| GameObject/Property | `MMF_Property` | `MoreMountains.Feedbacks` | none |
| GameObject/Set Active | `MMF_SetActive` | `MoreMountains.Feedbacks` | none |

## Haptics

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Haptics/Haptic Clip | `MMF_NVClip` | `MoreMountains.FeedbacksForThirdParty` | `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` |
| Haptics/Haptic Continuous | `MMF_NVContinuous` | `MoreMountains.FeedbacksForThirdParty` | `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` |
| Haptics/Haptic Control | `MMF_NVControl` | `MoreMountains.FeedbacksForThirdParty` | `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` |
| Haptics/Haptic Emphasis | `MMF_NVEmphasis` | `MoreMountains.FeedbacksForThirdParty` | `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` |
| Haptics/Haptic Preset | `MMF_NVPreset` | `MoreMountains.FeedbacksForThirdParty` | `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` |
| Haptics/Haptics DEPRECATED! (deprecated, use the Haptic * feedbacks) | `MMF_Haptics` | `MoreMountains.FeedbacksForThirdParty` | none |

## Lights

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Lights/Light | `MMF_Light` | `MoreMountains.Feedbacks` | none |
| Lights/Light2D_URP | `MMF_Light2D_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |

## Loop

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Loop/Looper | `MMF_Looper` | `MoreMountains.Feedbacks` | none |
| Loop/Looper Start | `MMF_LooperStart` | `MoreMountains.Feedbacks` | none |

## Particles

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Particles/Particles Instantiation | `MMF_ParticlesInstantiation` | `MoreMountains.Feedbacks` | none |
| Particles/Particles Play | `MMF_Particles` | `MoreMountains.Feedbacks` | none |
| Particles/VisualEffect | `MMF_VisualEffect` | `MoreMountains.FeedbacksForThirdParty` | `MM_VISUALEFFECTGRAPH` |
| Particles/VisualEffectSetProperty | `MMF_VisualEffectSetProperty` | `MoreMountains.FeedbacksForThirdParty` | `MM_VISUALEFFECTGRAPH` |

## Pause

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Pause/Holding Pause | `MMF_HoldingPause` | `MoreMountains.Feedbacks` | none |
| Pause/Pause | `MMF_Pause` | `MoreMountains.Feedbacks` | none |

## Physics

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Physics/Explosion Force | `MMF_ExplosionForce` | `MoreMountains.Feedbacks` | none |
| Physics/Explosion Force 2D | `MMF_ExplosionForce2D` | `MoreMountains.Feedbacks` | `MM_PHYSICS2D` |
| Physics/Gravity | `MMF_Gravity` | `MoreMountains.Feedbacks` | none (its 2D option needs `MM_PHYSICS2D`) |
| Physics/Rigidbody | `MMF_Rigidbody` | `MoreMountains.Feedbacks` | none |
| Physics/Rigidbody2D | `MMF_Rigidbody2D` | `MoreMountains.Feedbacks` | `MM_PHYSICS2D` |

`MMF_ExplosionForce` calls `Rigidbody.AddExplosionForce` on its `TargetRigidbody` and `ExtraTargetRigidbodies` (`TargetMode = Rigidbodies`) or on every rigidbody found within `Radius` (`TargetMode = Overlap`, filtered by `OverlapLayerMask`). `MMF_ExplosionForce2D` does the same for 2D (Unity has no 2D explosion API, so it pushes each body away from the center, scaled by its `Falloff` curve, with an optional random torque). Both use the `ExplosionCenter` transform, or the position passed to `PlayFeedbacks(position)` when it's empty, plus `ExplosionCenterOffset`. `MMF_Gravity` changes `Physics.gravity` and/or `Physics2D.gravity` (`Dimension`) by a `Multiplier` or towards a `TargetGravity`, `Instant`, `InstantThenReset` (default) or `OverTime`. It does nothing outside Play mode (physics doesn't simulate there), and puts the project's gravity back when exiting Play mode. `MMF_Rigidbody` and `MMF_Rigidbody2D` used to be under `GameObject/`: only their menu path changed, so players that already contain them are unaffected.

## PostProcess

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| PostProcess/Bloom | `MMF_Bloom` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Bloom HDRP | `MMF_Bloom_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Bloom URP | `MMF_Bloom_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Channel Mixer HDRP | `MMF_ChannelMixer_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Channel Mixer URP | `MMF_ChannelMixer_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Chromatic Aberration | `MMF_ChromaticAberration` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Chromatic Aberration HDRP | `MMF_ChromaticAberration_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Chromatic Aberration URP | `MMF_ChromaticAberration_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Color Adjustments HDRP | `MMF_ColorAdjustments_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Color Adjustments URP | `MMF_ColorAdjustments_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Color Grading | `MMF_ColorGrading` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Depth Of Field | `MMF_DepthOfField` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Depth of Field HDRP | `MMF_DepthOfField_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Depth Of Field URP | `MMF_DepthOfField_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Exposure HDRP | `MMF_Exposure_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Film Grain HDRP | `MMF_FilmGrain_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Film Grain URP | `MMF_FilmGrain_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Global PP Volume Auto Blend | `MMF_GlobalPPVolumeAutoBlend` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Global PP Volume Auto Blend URP | `MMF_GlobalPPVolumeAutoBlend_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Lens Distortion | `MMF_LensDistortion` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Lens Distortion HDRP | `MMF_LensDistortion_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Lens Distortion URP | `MMF_LensDistortion_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Motion Blur HDRP | `MMF_MotionBlur_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Motion Blur URP | `MMF_MotionBlur_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/Panini Projection HDRP | `MMF_PaniniProjection_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Panini Projection URP | `MMF_PaniniProjection_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/PPMovingFilter | `MMF_PPMovingFilter` | `MoreMountains.Feedbacks` | none |
| PostProcess/Vignette | `MMF_Vignette` | `MoreMountains.FeedbacksForThirdParty` | `MM_POSTPROCESSING` |
| PostProcess/Vignette HDRP | `MMF_Vignette_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/Vignette URP | `MMF_Vignette_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |
| PostProcess/White Balance HDRP | `MMF_WhiteBalance_HDRP` | `MoreMountains.FeedbacksForThirdParty` | `MM_HDRP` |
| PostProcess/White Balance URP | `MMF_WhiteBalance_URP` | `MoreMountains.FeedbacksForThirdParty` | `MM_URP` |

## Renderer

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Renderer/Flicker | `MMF_Flicker` | `MoreMountains.Feedbacks` | none |
| Renderer/Fog | `MMF_Fog` | `MoreMountains.Feedbacks` | none |
| Renderer/Line Renderer | `MMF_LineRenderer` | `MoreMountains.Feedbacks` | none |
| Renderer/Material | `MMF_Material` | `MoreMountains.Feedbacks` | none |
| Renderer/Material Set Property | `MMF_MaterialSetProperty` | `MoreMountains.Feedbacks` | none |
| Renderer/MMBlink | `MMF_Blink` | `MoreMountains.Feedbacks` | none |
| Renderer/Shader Global | `MMF_ShaderGlobal` | `MoreMountains.Feedbacks` | none |
| Renderer/ShaderController | `MMF_ShaderController` | `MoreMountains.Feedbacks` | `MM_UI` |
| Renderer/Skybox | `MMF_Skybox` | `MoreMountains.Feedbacks` | none |
| Renderer/Sprite | `MMF_Sprite` | `MoreMountains.Feedbacks` | none |
| Renderer/SpriteRenderer | `MMF_SpriteRenderer` | `MoreMountains.Feedbacks` | none |
| Renderer/SpriteRenderer Alpha | `MMF_SpriteRendererAlpha` | `MoreMountains.Feedbacks` | none |
| Renderer/Texture Offset | `MMF_TextureOffset` | `MoreMountains.Feedbacks` | none |
| Renderer/Texture Scale | `MMF_TextureScale` | `MoreMountains.Feedbacks` | none |
| Renderer/Trail Renderer | `MMF_TrailRenderer` | `MoreMountains.Feedbacks` | none |

## Scene

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Scene/Load Scene | `MMF_LoadScene` | `MoreMountains.Feedbacks` | none |
| Scene/Unload Scene | `MMF_UnloadScene` | `MoreMountains.Feedbacks` | none |

## Springs

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Springs/Spring Color | `MMF_SpringColor` | `MoreMountains.Feedbacks` | none |
| Springs/Spring Float | `MMF_SpringFloat` | `MoreMountains.Feedbacks` | none |
| Springs/Spring Vector2 | `MMF_SpringVector2` | `MoreMountains.Feedbacks` | none |
| Springs/Spring Vector3 | `MMF_SpringVector3` | `MoreMountains.Feedbacks` | none |
| Springs/Spring Vector4 | `MMF_SpringVector4` | `MoreMountains.Feedbacks` | none |

## TextMesh Pro

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| TextMesh Pro/TMP Alpha | `MMF_TMPAlpha` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Character Spacing | `MMF_TMPCharacterSpacing` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Color | `MMF_TMPColor` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Count To | `MMF_TMPCountTo` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Count To Long | `MMF_TMPCountToLong` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Dilate | `MMF_TMPDilate` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Font Size | `MMF_TMPFontSize` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Line Spacing | `MMF_TMPLineSpacing` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Outline Color | `MMF_TMPOutlineColor` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Outline Width | `MMF_TMPOutlineWidth` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Paragraph Spacing | `MMF_TMPParagraphSpacing` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Softness | `MMF_TMPSoftness` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Text | `MMF_TMPText` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Text Reveal | `MMF_TMPTextReveal` | `MoreMountains.Feedbacks` | `MM_UGUI2` |
| TextMesh Pro/TMP Word Spacing | `MMF_TMPWordSpacing` | `MoreMountains.Feedbacks` | `MM_UGUI2` |

## Time

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Time/Freeze Frame | `MMF_FreezeFrame` | `MoreMountains.Feedbacks` | none |
| Time/Timescale Modifier | `MMF_TimescaleModifier` | `MoreMountains.Feedbacks` | none |

## Transform

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| Transform/Destination | `MMF_DestinationTransform` | `MoreMountains.Feedbacks` | none |
| Transform/LookAt | `MMF_LookAt` | `MoreMountains.Feedbacks` | none |
| Transform/Position | `MMF_Position` | `MoreMountains.Feedbacks` | none |
| Transform/Position Shake | `MMF_PositionShake` | `MoreMountains.Feedbacks` | none |
| Transform/Position Spring | `MMF_PositionSpring` | `MoreMountains.Feedbacks` | none |
| Transform/Rotate Position Around | `MMF_RotatePositionAround` | `MoreMountains.Feedbacks` | none |
| Transform/Rotation | `MMF_Rotation` | `MoreMountains.Feedbacks` | none |
| Transform/Rotation Shake | `MMF_RotationShake` | `MoreMountains.Feedbacks` | none |
| Transform/Rotation Spring | `MMF_RotationSpring` | `MoreMountains.Feedbacks` | none |
| Transform/Scale | `MMF_Scale` | `MoreMountains.Feedbacks` | none |
| Transform/Scale Shake | `MMF_ScaleShake` | `MoreMountains.Feedbacks` | none |
| Transform/Scale Spring | `MMF_ScaleSpring` | `MoreMountains.Feedbacks` | none |
| Transform/Set Parent | `MMF_SetParent` | `MoreMountains.Feedbacks` | none |
| Transform/Squash and Stretch | `MMF_SquashAndStretch` | `MoreMountains.Feedbacks` | none |
| Transform/Squash and Stretch Spring | `MMF_SquashAndStretchSpring` | `MoreMountains.Feedbacks` | none |
| Transform/Wiggle | `MMF_Wiggle` | `MoreMountains.Feedbacks` | none |

## UI Toolkit

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| UI Toolkit/UITK Background Color | `MMF_UIToolkitBackgroundColor` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Border Color | `MMF_UIToolkitBorderColor` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Border Radius | `MMF_UIToolkitBorderRadius` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Border Width | `MMF_UIToolkitBorderWidth` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Class | `MMF_UIToolkitClass` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Font Size | `MMF_UIToolkitFontSize` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Image Tint | `MMF_UIToolkitImageTint` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Opacity | `MMF_UIToolkitOpacity` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Rotate | `MMF_UIToolkitRotate` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Scale | `MMF_UIToolkitScale` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Size | `MMF_UIToolkitSize` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Stylesheet | `MMF_UIToolkitStylesheet` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Text | `MMF_UIToolkitText` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Text Color | `MMF_UIToolkitTextColor` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Transform Origin | `MMF_UIToolkitTransformOrigin` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Translate | `MMF_UIToolkitTranslate` | `MoreMountains.FeedbacksForThirdParty` | none |
| UI Toolkit/UITK Visible | `MMF_UIToolkitVisible` | `MoreMountains.FeedbacksForThirdParty` | none |

## UI

| Menu path | Class | Namespace | Requires |
|---|---|---|---|
| UI/CanvasGroup | `MMF_CanvasGroup` | `MoreMountains.Feedbacks` | none |
| UI/CanvasGroup BlocksRaycasts | `MMF_CanvasGroupBlocksRaycasts` | `MoreMountains.Feedbacks` | none |
| UI/Cursor | `MMF_Cursor` | `MoreMountains.Feedbacks` | none |
| UI/Floating Text | `MMF_FloatingText` | `MoreMountains.Feedbacks` | none |
| UI/Graphic | `MMF_Graphic` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Graphic CrossFade | `MMF_GraphicCrossFade` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image | `MMF_Image` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image Alpha | `MMF_ImageAlpha` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image Fill | `MMF_ImageFill` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image Material | `MMF_ImageMaterial` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image RaycastTarget | `MMF_ImageRaycastTarget` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image Sprite | `MMF_ImageSprite` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image Texture Offset | `MMF_ImageTextureOffset` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Image Texture Scale | `MMF_ImageTextureScale` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/RectTransform Anchor | `MMF_RectTransformAnchor` | `MoreMountains.Feedbacks` | none |
| UI/RectTransform Offset | `MMF_RectTransformOffset` | `MoreMountains.Feedbacks` | none |
| UI/RectTransform Pivot | `MMF_RectTransformPivot` | `MoreMountains.Feedbacks` | none |
| UI/RectTransformSizeDelta | `MMF_RectTransformSizeDelta` | `MoreMountains.Feedbacks` | none |
| UI/Text | `MMF_Text` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Text Color | `MMF_TextColor` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Text Font Size | `MMF_TextFontSize` | `MoreMountains.Feedbacks` | `MM_UI` |
| UI/Video Player | `MMF_VideoPlayer` | `MoreMountains.Feedbacks` | `!UNITY_WEBGL` |
