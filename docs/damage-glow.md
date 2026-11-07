# Damage glow

`DamageGlow` listens to `EntityManager.OnInteraction` while enabled. Arguments are `(source, target, context)`; the glow filters by target. It unsubscribes when disabled.
Only an Attack interaction for its configured Controller with positive post-defense
damage triggers the effect. Healing, zero damage, and other targets are ignored.

The Player 1 and Normal, Tank, and Boss enemy prefabs have the component enabled.
The configured visual root includes their character meshes. At setup, the component
discovers skinned mesh renderers, falling back to mesh renderers for objects without
skinned meshes. Explicit Target Renderers can override discovery, for example to
exclude props or effects.

On damage, cached red-tinted, emissive material copies replace the originals for
0.12 scaled seconds on the character prefabs, with red emission intensity 0.03.
The component's defaults for newly added instances remain 0.18 seconds and intensity 3.
Repeated hits extend the effect. Duration Seconds and HDR Glow
Color are configurable in the Inspector; emission requires a shader supporting
`_EmissionColor` and `_EMISSION`. The current URP Lit character materials support
these. A bloom halo additionally depends on the scene's rendering settings.

Material copies are created once per renderer material slot during setup and retained
until destruction, trading additional material memory for reuse across hits and pool
spawns. Shared material assets are never edited. The effect restores original materials
and unsubscribes on disable, including despawn; destruction releases its owned copies.
Do not swap materials through another component during a flash: DamageGlow owns those
renderer material assignments until restoration. Existing property blocks remain in place.

Validation in Unity: damage each character, verify a red flash and restoration, hit
repeatedly, check healing and blocked damage, then disable/re-enable and pool/reuse
during a flash. Verify scene teardown and that indicators and shared assets remain unchanged.
