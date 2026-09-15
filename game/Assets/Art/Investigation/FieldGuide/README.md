# Investigation field-guide illustrations

The active Figma food-chain case uses `tuna.png`, `krill.png`,
`atlantic-herring.png`, `phytoplankton.png` and `tree-bubblegum-coral.png` from this folder. Its hammerhead
uses the newer [team artwork](../TeamSpecies/README.md).

Herring and phytoplankton were generated with the built-in `image_gen` tool on
2026-09-10. Exact prompts are recorded in [GENERATION-FIGMA.json](GENERATION-FIGMA.json).
They represent Atlantic herring (*Clupea harengus*) and an enlarged illustrative
*Prochlorococcus marinus* cell, respectively. The latter is labelled phytoplankton
in the shared catalog. It is not a microscopy photograph or a scale comparison.

Prompts for the earlier active artwork are retained in
[GENERATION.json](GENERATION.json). The unused sea-star and mussel illustrations
have been removed; neither animal belongs to the approved case catalog.

All PNGs preserve their generated alpha channels. Unity uses single sprites,
a 512-pixel maximum texture size, clamped wrapping and no mipmaps. The original
resolution is retained on disk. `Update Visual Artwork` maintains the current
six case references and supplied catalog illustrations. Biology and prediction
assumptions are defined in the case data, not inferred from these drawings.

The historical survey and trawling model both use `tree-bubblegum-coral.png`,
a generated illustrative cutout of *Paragorgia arborea*. Its absence from Today
is part of the authored case; the image is not a field photograph.
The built-in image-generation prompt and provenance are in
[GENERATION-CORAL.json](GENERATION-CORAL.json). The original 1254 × 1254 RGBA
image retains genuine transparency; Unity imports it at up to 512 pixels.
