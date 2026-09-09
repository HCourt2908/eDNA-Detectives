# Act 2: compare possible explanations

The current second-act flow is: notebook arrival → play three prediction cards
from the same stable baseline → compare all predictions with the recorded survey
→ choose the explanation that fits the whole pattern → summarise the existing findings
→ record a qualified conclusion, all within the same workbench. The player sees only Observe and
Investigate in the navigation.

The observed strip is a notebook sheet: on first entry it unfolds from the notebook
button before the card walkthrough. Its read-only imagery uses the same
grouped detection symbols as Act 1's saved survey picture, including the grouped
current tuna imagery and explicit non-detections. The main trial scene includes
shark, tuna, krill, sea star and mussel. All three cards remain on screen throughout
playback and comparison; there is no separate large seamount model view.
Sea star and mussel remain necessary controls for distinguishing explanations.

A cause starts only from its dedicated Play button, which becomes Replay after a
completed run. Card backgrounds and icon/title headers are passive. Each title
includes the corresponding bottle, fishing-pole or trawling icon. Unplayed and
interrupted cards show a common baseline, not an unfinished prediction. Playback
runs directly in the card while the other cards keep their results. It can be skipped, interrupted or
replayed; an interrupted trial is not counted as finished. After all three runs,
the explanation buttons unlock without automatic match scores. An incompatible choice
highlights a conflicting survey record and gives EDNA feedback without a penalty.
A compatible choice automatically records the authored aggregate evidence checks
and carries that chosen explanation into the integrated summary.
These are system-computed audit records, replacing seven manual UI submissions;
the case is not marked solved until the player explicitly records the conclusion
after reviewing the survey and model comparisons. Model-conclusion validation
requires the completed survey, all tested models and all required comparison
objectives. The existing result publication is reused, with only observed survey
evidence in the result.

Model populations begin with three organism symbols. An increase brings two new
symbols into the group (five total); a decrease sends two away (one remains).
Stable and unknown predictions retain three symbols; unknown is not presented as
an observed loss. Symbol counts are relative model illustrations, not animal counts.
Each symbol retains the same size. A 7.2-second playback holds the baseline, pulses
the affected groups in sequence, animates arrivals/departures, and holds the final
pattern before advancing. A progress track shows that the trial is running.
Result cards use the same one/three/five visual vocabulary. The skip control is
labelled "Skip animation" so it cannot be mistaken for the control that plays it.

The Act 2 notebook reuses Act 1's survey picture without the old text evidence
list below it, the per-prediction "Compare causes" controls, or a duplicate EDNA
introduction. No ROV pictures or unseen clues are added by the ending. The notebook
count reflects the five initial survey findings. Observe retains its existing content.

## Single-screen ending

The chosen model stays visible above three short summary rows: what the survey
found, how the unchanged sea star/mussel findings distinguish the models, and which
tested model best fits the full pattern. The ending introduces no new evidence.
Fishing-line and intact-seabed claims, ROV field notes, and camera interactions are
absent from the active two-act flow.

The two actions are Record conclusion and Compare again. Recording calls the
explicit SubmitModelConclusion domain path. It never calls TryReviewConfirmation,
never discovers confirmation observations and never sets ConfirmationReviewed.
Only discovered initial findings are selected and exported. The legacy SubmitFinal
path still enforces its original field-confirmed-report requirements; those rules
were not weakened to make this model comparison complete.

Returning to compare preserves prior work without submitting anything. QA conclusion
and case-closed checkpoints use the same survey-only route. Existing legacy evidence
is not erased, but it is excluded from a newly recorded model conclusion. Completion
displays CONCLUSION RECORDED and describes the best fit among the tested models,
not proof of cause or proof that a non-detected species is absent.

The internal Report phase is retained for existing state and controller contracts;
its separate navigation tab, report form, repeated explanation questions, argument
slots and footer are no longer part of the player-facing flow. QA ReportReady and
FinalReportReady checkpoints enter the appropriate point of this integrated ending.

The main workbench is fitted to a fixed viewport and its vertical scrolling is
disabled. Resizing fits the content inside the available space. The notebook drawer
can still scroll independently. EDNA uses a smaller portrait and bar, and the
notebook is an icon with its finding-count badge, without an adjacent caption.

## EDNA spotlight walkthrough

After the notebook arrives, a two-step introduction highlights the recorded
survey and the prediction cards' Play controls. EDNA appears in a paper dialogue
with Next and Skip guide. Only the first completed prediction gets a results
explanation; the full comparison gets its own prompt. A conflicting choice
highlights the relevant observed species beside EDNA's feedback.

EDNA stays in a fixed bottom bar throughout Act 2. The notebook and current action
(including the Record conclusion action) sit between her text and her
portrait. They remain visible throughout the investigation. The bar also remains
available while the notebook is open. "Show me where" reopens help for the current
stage. Opening it during a
trial pauses that trial's presentation clock; closing it resumes the same moment.
Reading or skipping help never changes investigation progress. The dimmer blocks
underlying pointer input and explicit keyboard navigation keeps focus in the guide.
The guide dims the surroundings and reveals the real panel through a clear window.
A transparent input shield prevents unintended actions while EDNA explains it. For the model and results, EDNA moves to the top; the workbench never moves
or rescales to accommodate the guide. Bottom space stays reserved during this
transition, so opening or closing help does not change the page layout. The live
UI remains in its original hierarchy and never needs to be hidden or copied for a guide.

The walkthrough is owned by `InvestigationRuntimeView.ScenarioBriefing.cs`,
`InvestigationRuntimeView.ScenarioDock.cs` and `InvestigationScenarioBriefingHost.cs`.
It does not reuse or modify the first-act
briefing, which is being developed independently.

## Ownership and integration

Implementation is isolated in `InvestigationRuntimeView.ScenarioFlow.cs`, `InvestigationRuntimeView.ScenarioCards.cs`,
`InvestigationRuntimeView.ScenarioGraphics.cs`, `InvestigationScenarioPlayback.cs`,
and the separately owned frozen material in `InvestigationScenarioTerrain.cs`.
`InvestigationRuntimeView.ScenarioEnding.cs` owns the fitted workspace and ending.
Shared integration edits cover the phase dispatch, two-stage navigation, layout,
EDNA routing, and the notebook's Investigate/ending branch. The controller binds an
explicit model-conclusion action to the new domain evaluator/updater entry point.
Observe gameplay, case assets and the legacy report evaluator are unchanged.
The old manual-linking desk is no longer the second-act entry point.

Animation state is scoped to the current InvestigationState instance. A rendered
playback component owns callbacks, while presenter timestamps survive UI rebuilds.
The three-node teaching model and current authored cause predictions are retained;
this does not introduce the five-node Figma reference chain as a new simulation.

## Validation

Use `InvestigationScenarioFlowTests` for the revised player contract: auto-assembly,
common baseline, interruption/replay, immutable records, wrong-choice feedback,
aggregate audit, ROV/report handoff, Observe return and restart/reduced-motion paths.
Superseded manual-linking, per-prediction and ROV UI checks have been retired or
migrated with explicit replacement coverage. See [validation and migration](Investigation-Validation.md).
Test snapshots and render previews should use a separate Unity copy.

`InvestigationScenarioBriefingTests` covers introduction order and stale callbacks,
unchanged progress, contextual results/comparison/conflict prompts, manual-help
pause/resume, compact layout, pointer input blocking and cleanup when leaving Act 2.
`InvestigationScenarioDockTests` additionally verifies the notebook flight, constant
workbench bounds, permanent access to the notebook, and pinned ROV controls.
`InvestigationScenarioPopulationTests` verifies visible arrivals/departures, the
final hold, a common baseline on replay, and matching population imagery on cards.
`InvestigationScenarioEndingTests` verifies the finding-only summary, no new ROV
discoveries or exported evidence, explicit submission, stale-click protection, re-comparison, QA restart and
single-screen bounds throughout the ending.
`InvestigationScenarioCardsTests` checks passive titles/backgrounds, explicit Play
and Replay controls, fixed card bounds, and unfinished-result/choice gating.
On 2026-09-09, the dedicated Unity 6000.4.6f1 validation copy passed all 22 tests
across these six suites. Runtime screenshots also exercise the guide at 1280×720
and at a 720-unit logical canvas width. This validates the isolated Act 2 change;
This historical targeted run is not the full-suite release gate; current whole-project
results and build coverage are recorded in [Investigation-Validation.md](Investigation-Validation.md).

The 59 EditMode domain tests also pass, including the new model-only completion,
incomplete/wrong-model rejection, export filtering and unchanged legacy report gate.
