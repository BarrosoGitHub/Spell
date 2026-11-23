# Post-processing feedback to shaker map (Feel 6.1)

Generated from the Feel 6.1 source. Read this file when you need to know which shaker component must sit on the Volume for a given post-processing feedback, which volume override it drives, or which define gates it.

Every feedback below broadcasts an event. Only a shaker of the matching type, on a GameObject that also has the Volume (URP/HDRP `Volume`, Built-in `PostProcessVolume`), and on a matching channel, will react. The feedback's **Automatic Shaker Setup** button adds the volume override and the shaker for you (edit mode only).


## URP (`MM_URP`)

| Add new feedback path | Feedback class | Volume override | Shaker component |
|---|---|---|---|
| PostProcess/Bloom URP | `MMF_Bloom_URP` | `Bloom` | `MMBloomShaker_URP` |
| PostProcess/Channel Mixer URP | `MMF_ChannelMixer_URP` | `ChannelMixer` | `MMChannelMixerShaker_URP` |
| PostProcess/Chromatic Aberration URP | `MMF_ChromaticAberration_URP` | `ChromaticAberration` | `MMChromaticAberrationShaker_URP` |
| PostProcess/Color Adjustments URP | `MMF_ColorAdjustments_URP` | `ColorAdjustments` | `MMColorAdjustmentsShaker_URP` |
| PostProcess/Depth Of Field URP | `MMF_DepthOfField_URP` | `DepthOfField` | `MMDepthOfFieldShaker_URP` |
| PostProcess/Film Grain URP | `MMF_FilmGrain_URP` | `FilmGrain` | `MMFilmGrainShaker_URP` |
| PostProcess/Global PP Volume Auto Blend URP | `MMF_GlobalPPVolumeAutoBlend_URP` | n/a | n/a (see note) |
| PostProcess/Lens Distortion URP | `MMF_LensDistortion_URP` | `LensDistortion` | `MMLensDistortionShaker_URP` |
| PostProcess/Motion Blur URP | `MMF_MotionBlur_URP` | `MotionBlur` | `MMMotionBlurShaker_URP` |
| PostProcess/Panini Projection URP | `MMF_PaniniProjection_URP` | `PaniniProjection` | `MMPaniniProjectionShaker_URP` |
| PostProcess/Vignette URP | `MMF_Vignette_URP` | `Vignette` | `MMVignetteShaker_URP` |
| PostProcess/White Balance URP | `MMF_WhiteBalance_URP` | `WhiteBalance` | `MMWhiteBalanceShaker_URP` |

## HDRP (`MM_HDRP`)

| Add new feedback path | Feedback class | Volume override | Shaker component |
|---|---|---|---|
| PostProcess/Bloom HDRP | `MMF_Bloom_HDRP` | `Bloom` | `MMBloomShaker_HDRP` |
| PostProcess/Channel Mixer HDRP | `MMF_ChannelMixer_HDRP` | `ChannelMixer` | `MMChannelMixerShaker_HDRP` |
| PostProcess/Chromatic Aberration HDRP | `MMF_ChromaticAberration_HDRP` | `ChromaticAberration` | `MMChromaticAberrationShaker_HDRP` |
| PostProcess/Color Adjustments HDRP | `MMF_ColorAdjustments_HDRP` | `ColorAdjustments` | `MMColorAdjustmentsShaker_HDRP` |
| PostProcess/Depth of Field HDRP | `MMF_DepthOfField_HDRP` | `DepthOfField` | `MMDepthOfFieldShaker_HDRP` |
| PostProcess/Exposure HDRP | `MMF_Exposure_HDRP` | `Exposure` | `MMExposureShaker_HDRP` |
| PostProcess/Film Grain HDRP | `MMF_FilmGrain_HDRP` | `FilmGrain` | `MMFilmGrainShaker_HDRP` |
| PostProcess/Lens Distortion HDRP | `MMF_LensDistortion_HDRP` | `LensDistortion` | `MMLensDistortionShaker_HDRP` |
| PostProcess/Motion Blur HDRP | `MMF_MotionBlur_HDRP` | `MotionBlur` | `MMMotionBlurShaker_HDRP` |
| PostProcess/Panini Projection HDRP | `MMF_PaniniProjection_HDRP` | `PaniniProjection` | `MMPaniniProjectionShaker_HDRP` |
| PostProcess/Vignette HDRP | `MMF_Vignette_HDRP` | `Vignette` | `MMVignetteShaker_HDRP` |
| PostProcess/White Balance HDRP | `MMF_WhiteBalance_HDRP` | `WhiteBalance` | `MMWhiteBalanceShaker_HDRP` |

## Built-in RP, Post Processing v2 (`MM_POSTPROCESSING`)

| Add new feedback path | Feedback class | Volume override | Shaker component |
|---|---|---|---|
| PostProcess/Bloom | `MMF_Bloom` | `Bloom` | `MMBloomShaker` |
| PostProcess/Chromatic Aberration | `MMF_ChromaticAberration` | `ChromaticAberration` | `MMChromaticAberrationShaker` |
| PostProcess/Color Grading | `MMF_ColorGrading` | `ColorGrading` | `MMColorGradingShaker` |
| PostProcess/Depth Of Field | `MMF_DepthOfField` | `DepthOfField` | `MMDepthOfFieldShaker` |
| PostProcess/Global PP Volume Auto Blend | `MMF_GlobalPPVolumeAutoBlend` | n/a | n/a (see note) |
| PostProcess/Lens Distortion | `MMF_LensDistortion` | `LensDistortion` | `MMLensDistortionShaker` |
| PostProcess/Vignette | `MMF_Vignette` | `Vignette` | `MMVignetteShaker` |

## Notes

- `MMF_GlobalPPVolumeAutoBlend` / `MMF_GlobalPPVolumeAutoBlend_URP` (`PostProcess/Global PP Volume Auto Blend*`) do not use a shaker: they blend a whole volume's weight over time through an `MMGlobalPostProcessingVolumeAutoBlend` / `MMGlobalPostProcessingVolumeAutoBlend_URP` component placed on that volume and assigned to the feedback's `TargetAutoBlend`.
- `MMF_PPMovingFilter` (`PostProcess/PPMovingFilter`) triggers a moving-filter event caught by an `MMPostProcessingMovableFilter` object; it has no pipeline-specific shaker.
- Shaker namespaces: URP and HDRP shakers and feedbacks are in `MoreMountains.FeedbacksForThirdParty`. All compile into the `MoreMountains.Tools` assembly.
