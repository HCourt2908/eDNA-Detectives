# Seamount shape and material study 01

First Blender art study for the eDNA Detectives seamount, 7 September 2026.
This is a fictional, authored terrain concept, not a measured bathymetric site.
The three looks now drive the synchronised Observe background in Unity.

## Review files

- `comparison.png`: three looks with identical geometry and camera framing.
- `renders/natural.png`: restrained deep-water illumination and textured rock.
- `renders/illustrated.png`: brighter teal lighting and softer surface texture.
- `renders/bathymetry.png`: elevation colour and contour lines for map exploration.
- `seamount-study.blend`: editable source with Natural, Illustrated and Bathymetry scenes.
- `renders/surface-mask.png`: aligned terrain mask used to blend the mountain into Unity's water.

The shape study has an offset summit, unequal descending ridge arms, winding
gullies, a lower side shoulder, sediment on flatter surfaces and scattered talus.
The mesh continues into a seabed outside the rendered view. The scattered rocks
are restricted to gentler surfaces. Biological decoration is outside this study.

All surface patterns, geometry and lighting are generated locally in Blender;
no downloaded models or textures are required. The three scenes share the main
terrain mesh and repeat the same rock placements and camera transform.

## Reproduce

Runtime: Blender 5.2.1 LTS, Cycles CPU, 8 render threads, deterministic seed
`20260907`. Final images are 1800 × 1260, 72 samples with denoising. The sheet
is 2400 × 1008 and is also composed and rendered in Blender.

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b -t 8 -P docs/art-experiments/seamount-v2/render_seamount.py -- --styles natural illustrated bathymetry
/Applications/Blender.app/Contents/MacOS/Blender -b -t 8 -P docs/art-experiments/seamount-v2/render_comparison.py
```

For a 1000 × 700 draft, add `--preview` after the first command's `--`.
Preview mode writes into `preview/` and does not replace the `.blend` source.

The source mesh favours offline art iteration. The current Unity integration
blends the rendered textures; direct 3D mesh use would require a separate runtime
mesh and material pass.
