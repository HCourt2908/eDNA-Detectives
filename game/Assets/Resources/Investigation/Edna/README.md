# EDNA guide artwork

The team supplied two updated right-facing illustrations on 2026-09-10: `edna clean.png` and `edna hand up.png`. The original files are retained unchanged outside the repository.

- `edna.png`: transparent neutral half-body pose (1295 × 1215 RGBA), used for ordinary dialogue and conclusions. Its existing Unity asset GUID is preserved.
- `edna-speaking.png`: transparent raised-hand pose (1334 × 1179 RGBA), used for spotlight explanations. The full hand and finger are included.

The built-in image-generation tool prepared the production copies by removing empty margins and stray color swatches around the head and hand. These are AI-cleaned derivatives, not pixel-identical exports of the source drawings. See `PREPARATION.json` for source names and cleanup instructions. The older `docs/art-experiments/edna/prepare_edna.py` script describes the previous artwork pipeline and was not used for these replacements.

Both sprites have genuine alpha transparency. Unity imports them at up to 1024 pixels without mipmaps. Portraits sit on the left of dialogue text, facing inward; the images are not mirrored. The raised-hand pose has extra horizontal room, while the Act 2 dock keeps its fixed height so opening guidance does not resize the workbench.

Compact head-and-shoulders avatars are derived from the neutral sprite at runtime, using the upper 60% and central 68% of its texture rectangle. The view owns and disposes this derived sprite without destroying the shared portrait texture. There is no separate legacy avatar asset.
