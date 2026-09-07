# Investigation field-guide illustrations

Five transparent illustrations created for this project with the built-in `image_gen` tool on 2026-09-05. The exact prompts are recorded in [GENERATION.json](GENERATION.json).

| File | Case species |
| --- | --- |
| hammerhead.png | Great Hammerhead Shark / shark |
| tuna.png | Atlantic Bluefin Tuna / tuna |
| krill.png | Northern Krill / krill |
| sea-star.png | Sea star / sea_star |
| mussel.png | Filter-feeding mussel / mussel |

The source PNGs retain their generated alpha channels. Unity imports them as single sprites with a 512-pixel maximum texture size, high-quality texture compression, clamped wrapping and no mipmaps. Source files are retained at their original resolution.

`eDNA Detectives > Update Visual Artwork` configures the importers and updates only the five case species' icon references. The main Investigation builder also prefers these illustrations when creating the core species. The original OpenMoji assets and their licence remain in their own folder; threat and scenario icons still use their existing sources.

These are illustrative game assets, not taxonomic reference photographs. Gameplay predictions and evidence remain defined by the case data.
