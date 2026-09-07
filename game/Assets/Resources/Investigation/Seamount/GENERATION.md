# Seamount appearance cycle

These three in-house Blender renders come from the same authored terrain and
camera in `docs/art-experiments/seamount-v2/seamount-study.blend`:

- `natural.png`: A, deep-water rock and sediment.
- `illustrated.png`: B, brighter teal shading.
- `bathymetry.png`: C, elevation colour with contour lines.

The source generator and render settings are recorded in that study's README.
The three appearance PNG files here are byte-identical copies of its final 1800 × 1260 renders.
They represent the fictional case terrain, not a measured geographical site.

`surface-mask.png` is rendered from the same saved terrain and camera by
`render_surface_mask.py`. Height-based feathering removes the flat outer seabed
while retaining the mountain, apron and rocks, so the rectangular render does
not show through the UI. It is imported as linear mask data.

`InvestigationSeamountImportSettings` imports the images at a maximum dimension
of 1024 pixels, with sRGB colour, no mipmaps or CPU readback, and uncompressed
colour to preserve smooth gradients. All three retain the same aspect ratio.

`InvestigationSeamountBackdrop` owns one runtime material and an unscaled clock
for both Observe maps. `SeamountCrossfade.shader` blends the three textures in
one UI pass, then applies the shared terrain mask. Normal UI rectangle clipping
and pointer handling remain in place. The old `seamount_hero` sprite remains a
fallback for unavailable appearance assets.

The 20-second sequence matches the video preview: A 0–1 s, A→B 1–5 s,
B 5–5.5 s, B→C 5.5–9.5 s, C 9.5–10.5 s, C→B 10.5–14.5 s,
B 14.5–15 s, B→A 15–19 s, A 19–20 s. Transitions use cosine easing.
Ordinary UI refreshes reuse the material and clock; restarting the case resets
the cycle. Reduced Motion shows A without the appearance animation.
