# Shared Species Catalog

Source: the team's [Species List FigJam](https://www.figma.com/board/m631tWbfQ8NajWmFN3X93q/Species-List?node-id=0-1),
checked on 2026-09-10. It currently names **20 species**, rather than a confirmed
larger roster. The exact canonical ID, display name, scientific name and source
node for each entry are listed in [species-catalog.csv](species-catalog.csv).

## Identification Handoff

Use the canonical ID when possible. Scientific names, full display names and
explicit aliases also resolve to the same definition. Matching ignores case,
leading/trailing whitespace and space/underscore/hyphen differences; it does not
infer a species from a partial name or assign unrelated animals as aliases.

`FindCatalogSpecies` is the boundary for imported species records. Unknown or
non-catalog organisms are omitted, consistent with the existing tolerant import
contract. The five authored case findings retain their existing evidence rules;
contradictory imports still fail before modifying the session. The survey display
remains capped at seven organisms, including the five case-critical organisms.

Adding a species requires a team Species List update, a catalog definition with a
unique canonical identity, and matching roster coverage. A spelling change must
not silently become a second organism. Scientific and display-name collisions
are checked by the case validator.

## Current Case Exceptions

Sea star and filter-feeding mussel are still legacy controls in the playable case.
Neither appears in the updated Species List. They are excluded from the shared
import catalog and must not be relabelled as a different animal while retaining
unsupported predictions. Their replacements and affected comparisons are pending
team confirmation.

The three existing causes (plastic pollution, long-line fishing and bottom
trawling) are intentionally retained for this update. Frederick is reviewing the
food web and preparing a replacement for the plastic scenario; no new third
scenario or biological response has been invented here.

## Team Artwork

The supplied hammerhead, manta ray and bone-eating worm illustrations are wired
through their species definitions, so maps and notebooks share the same artwork.
See the [artwork mapping](../game/Assets/Art/Investigation/TeamSpecies/README.md).
The unlabelled fish illustration needs a confirmed species name before use.
