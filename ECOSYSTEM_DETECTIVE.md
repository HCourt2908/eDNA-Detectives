# Ecosystem Detective — Figma food-chain activity

Current specification, updated 2026-09-10. The activity uses the team's
[Species List and Food chain examples](https://www.figma.com/board/m631tWbfQ8NajWmFN3X93q/Species-List?node-id=19-1912).
It is a standalone, two-act teaching prototype; the Figma relationships and
illustrative survey are not a validated population forecast or real field dataset.

## Start and scope

Open `game` in Unity **6000.4.6f1**, open
`Assets/Scenes/InvestigationScene.unity`, and enter Play Mode.
The active case ID is **`investigation_foodchain_02`**. The existing case asset path
and GUID are retained so scene references remain intact.

The complete main chain, with arrows meaning **eats**, is:

**Great Hammerhead Shark → Atlantic Bluefin Tuna → Atlantic Herring → Northern Krill → Phytoplankton**

Northern krill represent the zooplankton link in this catalog. Phytoplankton uses
the listed `Prochlorococcus marinus` identity. Organisms are not drawn to scale.
Sea star and mussel are no longer active species or case definitions.
The other Figma food webs are optional notebook references, not new observations.

## Act 1: Observe

The activity starts with **Today**, with the lens handle at the left. EDNA first
introduces the survey and recording controls. Only current detections fly into
the notebook when the player starts recording. Shark is absent from today's map
and is first recorded when the player views and records **20 years ago**.

The illustrative survey deliberately exercises both more and fewer detections:

| Species | Example survey change | Historical / Today symbols |
| --- | --- | --- |
| Shark | Not detected today | 1 / 0 |
| Tuna | Detected at more sites | 1 / 3 |
| Herring | Detected at fewer sites | 3 / 1 |
| Krill | Detected at more sites | 1 / 3 |
| Phytoplankton | Detected at fewer sites | 3 / 1 |

These are relative detection pictures, never a population census. Today's four
positive records and all five historical records are collected separately.
The same compositions are used on the map, in the flight animation and in the
notebook; the pictures must preserve the difference between more and fewer.

EDNA's comparison briefing introduces the notebook, neutral species tokens and
**More / Fewer / Not detected / Same** categories. Players drag or select a species
and then classify it. Wrong classifications keep the species available and do not
reveal the correct answer. The notebook stays beside the classification controls;
its seamount reference can be opened without losing the current selection.

All five findings are required. Players then summarize and save the dated picture
before moving to Investigate. Skipping an animation or guide does not invent an
answer. Full motion is used by default; no Easy/Hard or motion toggle is displayed.

## Act 2: Investigate

The notebook picture unfolds onto a fixed workbench. A compact label displays the
complete five-species chain. **Plastic pollution**, **Long-line fishing** and
**Bottom trawling** remain three separate cards, with explicit Play and Replay
buttons. Card headers and backgrounds do not start a simulation.

Both fishing models start from the same explicit **predator-removal assumption**.
The existing qualitative cascade follows the four Figma links to derive:

**Shark ↓ · Tuna ↑ · Herring ↓ · Krill ↑ · Phytoplankton ↓**

The simulator now uses `reference_main`, not the retired shark–tuna–krill shortcut.
Only the fishing models' shark seed is authored; the other four directions are
computed along that chain. This alternating rule is an illustrative model, not
proof that a real ecosystem must respond this way.

The plastic card is retained at the user's request while the team reviews the
third scenario. Its previous tuna-stable and krill-decrease assumptions remain
explicit prototype assumptions. Responses not supplied for shark, herring and
phytoplankton are **Unknown**. Unknown is not treated as Stable or as an automatic
contradiction. Known conflicting directions challenge this particular plastic
model; they do not rule out plastic pollution generally.

Every card starts with three model symbols. Increase ends with five, decrease
with one, and unknown symbols are subdued with an explicit Unknown label. The
five rows animate in food-chain order and hold their final pattern before the
run completes. Interrupted first runs do not count as completed; Replay starts
from the same baseline. All three runs are required before reviewing a cause.

The main workbench fits within the viewport without vertical scrolling. EDNA is
permanently available in a compact dock. The icon-only notebook opens the saved
survey picture. **Food-chain examples** expands the main, manta, deep-water and
seafloor reference networks inside the independently scrolling notebook. Viewing
these references does not discover evidence, finish trials or change a conclusion.

## Conclusion: main explanation and possible alternative

The case presents **Long-line fishing** as the main explanation and **Bottom
trawling** as a possible alternative, following the team's narrative direction.
These are authored case priorities, not likelihood scores inferred from identical
food-chain predictions. Both models remain compatible with the five-species
example, and reviewing either can open the conclusion.

The ending contains the recorded findings, the main explanation and the possible
alternative. EDNA acknowledges when the player reviewed the alternative. It does
not ask the player to obtain independent evidence or leave the activity unfinished.
**Record conclusion** completes the activity; **Compare again** returns to the
models. No ROV, new physical evidence or extra task is introduced. The compact
note still states that model agreement is not proof of cause and non-detection
does not prove absence.

The result retains `selectedHypothesisId` for the model the player reviewed and
`compatibleHypothesisIds` for matching models. `primaryHypothesisId` is `longline`;
`alternativeHypothesisIds` contains `bottom_trawling`. These priorities remain the
same whichever matching model was reviewed. Only the five gathered survey evidence
IDs are exported. The former `needsMoreEvidence` flag is removed. The legacy report
API retains its separate confirmation gates and is not the active completion path.

## Restart, input and handoff

A transparent top-right restart icon remains reachable above guides. Confirming
restart clears case progress and the result, then starts Observe using the same
session input. Cancelling preserves progress. Model playback pauses during the
confirmation. The player QA menu and checkpoint keyboard shortcuts are removed.

The shared catalog contains the 20 named Figma species. Canonical IDs, scientific
names, full display names and explicit aliases resolve consistently; case and
space/underscore/hyphen differences are normalized. Imported organisms must be
in the catalog. The map roster is capped at seven, preserving all five core
species. See [catalog details](docs/Species-Catalog.md).

Incoming records specify site, sample, era, depth, detection, confidence and quality.
The fixed example rejects incompatible core detection patterns before changing
state; a player can choose the standalone example instead. Old case IDs are not
silently accepted as the revised case. The actual identification producer and
cross-minigame navigation still need integration; an available contract is not
evidence of a completed end-to-end handoff.

## Development and validation

Unity menus provide **Run EditMode Tests** and **Run PlayMode Tests** under
`eDNA Detectives > Validation`. The assemblies are `EDNA.Investigation.EditModeTests`
and `EDNA.Investigation.PlayModeTests`. Internal test checkpoints use the current
case definitions, with no player-accessible QA controls.
The fixtures cover start, first finding, survey complete, model start, model
comparisons complete, conclusion ready and case closed. Retired ROV and evidence
picker checkpoints are removed. `LegacyReportCompatibility` marks tests for the
retained lower-level report API; these are not playable-screen requirements.

The old food-chain assembly desk, per-prediction evidence picker, ROV scan,
provisional modal and three-round report renderers are removed. Shared survey
lens and UI helpers live in `InvestigationRuntimeView.SurveyLens.cs` and
`InvestigationRuntimeView.UiHelpers.cs`. Act 2 renders the scenario cards and
integrated conclusion directly.

`eDNA Detectives > Update Figma Food Chain`, **Update Species Catalog** and
**Update Case Evidence** regenerate the same current case. **Update Visual Artwork**
retains the team images and the two added field-guide illustrations.
Test and review reports remain local and untracked. macOS and WebGL development
build commands exist; success on one platform does not establish device or WebGL
validation. Historical screenshots under `docs/images` are not the current UI spec.

## Artwork

Team artwork is documented in [TeamSpecies](game/Assets/Art/Investigation/TeamSpecies/README.md).
The herring and phytoplankton sprites are generated transparent illustrations;
see [FieldGuide](game/Assets/Art/Investigation/FieldGuide/README.md).
Threat icons use OpenMoji under the included CC BY-SA 4.0 licence. Restart and
status icons use Heroicons under the included MIT licence. Observe's seamount
retains the Blender-generated A → B → C → B → A background cycle.
