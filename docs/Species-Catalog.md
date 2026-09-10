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

## Active Food-chain Case

The active case is `investigation_foodchain_02`: shark, tuna, Atlantic herring,
krill and phytoplankton. Every active definition is a member of the approved
catalog. Sea star and mussel definitions have been removed; neither is treated
as an alias for another animal.

The two fishing scenarios use the same predator-removal premise on the Figma
network. Both remain compatible with the illustrative survey. The conclusion presents long-line fishing as the authored main explanation and
bottom trawling as a possible alternative, exporting `primaryHypothesisId` and
`alternativeHypothesisIds` alongside the reviewed and compatible model IDs. The plastic scenario is retained provisionally while Frederick
reviews the food-web responses and third scenario.

The example survey is authored for the teaching activity, not imported from
Figma as real measured data. Incoming producers must supply the correct case ID
and era; incompatible core detection patterns remain rejected.

## Team Artwork

The supplied hammerhead, manta ray and bone-eating worm illustrations are wired
through their species definitions, so maps and notebooks share the same artwork.
See the [artwork mapping](../game/Assets/Art/Investigation/TeamSpecies/README.md).
Generated herring and phytoplankton illustrations complete the active five-species set.
The unlabelled supplied fish illustration still needs a confirmed species name before assignment.
