# Investigation field-guide illustrations

The active Figma food-chain case uses `tuna.png`, `krill.png`,
`atlantic-herring.png` and `phytoplankton.png` from this folder. Its hammerhead
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
five case references and supplied catalog illustrations. Biology and prediction
assumptions are defined in the case data, not inferred from these drawings.
