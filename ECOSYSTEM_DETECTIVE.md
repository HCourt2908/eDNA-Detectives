# Ecosystem Detective — Detective Game Part

Documentation for the Ecosystem Detective part of the OceanX eDNA Detectives Unity project. This three-stage investigation asks players to compare survey results, test competing causes, and build an evidence-based report.

EDNA guides the investigation through short, contextual conversations. In Observe, a paper conversation beside the survey asks one species question at a time, with a transparent portrait on the right. The locator reads “Find EDNA at the top right.” Answer choices stay in a stable shuffled order for each question and session. In Simulate, her dialogue stays at the bottom and updates at each new task, combining the current investigation question, comparison feedback, optional hints and direct next actions. Dismissal lasts until the task changes; opening the Notebook temporarily hides the floating conversation. Report uses its own three-round EDNA conversation, with no second help overlay.
At the model prediction selection step, unchecked species cards have gently pulsing borders in either difficulty. Selecting a prediction stops those cues while the player chooses evidence; saved checks keep their checkmarks. After the first completed comparison, EDNA introduces the Notebook with a direct **Open notebook** action, and its usual icon glows until first opened. An optional first-visit note inside the Notebook explains scrolling through findings, **Compare causes**, and reopening a saved check. It does not replay on subsequent visits and resets with a new case. The cues do not change card dimensions or intercept clicks.

EDNA offers only relevant controls: **Run simulation**, **Guide my next check**, **Next check**, **Write first idea**, **Open notebook**, and optional **What next? / Why?**. A next-step action never chooses or submits the evidence answer for the player.

## Interactive investigation workbench

The playable prototype now adds hands-on operations to the existing case:

- **Observe:** slide a historical/current survey lens over aligned maps. Answer EDNA’s questions about non-detection, wider/fewer detection sites, or stable results. Correct answers record the authored finding; incorrect answers give a retry cue without a penalty. Optional organism facts are read-only. The notebook button appears after the first answer and returns to the same question when closed. The slide handle pulses until first used.
- **Simulate:** assemble the simplified food web by dragging a predator card onto its prey, or selecting the two cards. Diet notes explain the links. Place a cause on the model to apply a disturbance, and remove it to view the stable model baseline without deleting saved comparisons. Recorded food-web, benthic and pollution patterns can be dragged onto the evidence table or selected and tested. A grouped card checks its constituent findings through the existing scientific rules; the seven required objectives are preserved, but no longer require seven separate species-selection loops. Individual prediction details remain available for review. The saved food-web connections can also be reopened.
- **Report:** sweep a ROV camera across an inspection frame and pin the discovered finding to collect the ROV field-note set. Then connect a main clue and a distinct cross-check to the explanation using drag-and-drop or click-to-place. EDNA discusses the argument before the explanation is kept or revised. Clue emphasis is not separately scored.

This version uses the existing authored case and species. It does not yet wire the identification minigame's live results into the investigation. QA checkpoints retain fully prepared data for quick scene testing.

## Quick start

1. Open the `game` folder in Unity `6000.4.6f1`.
2. Open `Assets/Scenes/InvestigationScene.unity`.
3. Press Play.

## Current gameplay flow

### 1 · Observe

Players compare the same seamount **20 years ago** and **today** with a sliding lens, divided into shallow, mid, and deep depth bands. The two maps align spatially; moving the divider changes which survey is visible without scaling the maps.

Before the first recorded finding, the Notebook area shows a short **Select a
species on TODAY** prompt. Recording that first finding reveals the paper
Notebook in the same space. Historical facts do not create a finding; imported
findings show the Notebook immediately.

In Easy mode, every unrecorded case species on TODAY has a synchronised pulsing
frame. Players may start with any of them. Recording a finding removes that
species' cue while the remaining unrecorded species keep their cues. Historical
species and recorded-finding checks do not pulse.

Both maps share a synchronised 20-second background cycle: natural rock (A) →
brighter illustration (B) → bathymetric colours (C) → B → A. The mountain and
camera stay fixed, so the appearance cycle does not imply an ecological change.
Recording findings and refreshing the UI do not restart it. Full motion is used throughout the game.

- Five case-critical organisms are included in the historical baseline, and up to two additional species can be selected from the shared 20-species catalog when upstream survey results are supplied.
- Visible organisms use depth-aware scattering rather than fixed slots, so larger survey rosters can fill each depth band without leaving authored gaps. Each new case or Restart gets a fresh arrangement, while positions remain stable throughout that run.
- Detected and confidently not-detected species from an upstream eDNA result can join the survey roster automatically. The deterministic roster ranks food-web relevance, unusual results, confidence and sample quality, caps the screen at seven species, and falls back to the five authored case organisms when no input is supplied.
- Detailed handoff records carry species ID, detection state, survey era, depth, confidence, sample quality, site, sample and source. Legacy `detectedSpeciesIds` inputs remain supported.
- Imported catalog species appear only in the survey era supplied by the input. Their depth controls map placement, and current non-detections are omitted from the map, including imported non-detections.
- The seamount maps show organism artwork only; names, survey status and species facts appear on hover, keyboard focus or tap and are recorded in the Notebook.
- The organism in EDNA’s current question is highlighted wherever it is detected. The slide handle has a non-blocking pulse that stops after its first meaningful movement.
- EDNA asks one question at a time. A correct answer records the finding and advances to the next question; prior imported findings are skipped. Feedback distinguishes detections from animal counts, and non-detection from disappearance.
- Current non-detections leave empty space: no ghost, dashed cross, or invisible clickable historical organism. The opaque current layer fully covers historical artwork, and the remaining organisms retain their aligned positions. Historical water is brighter and historical organisms use full opacity.
- Wider detection appears as a small group.
- Hovering, keyboard-focusing, or tapping a marker opens its species facts.
- Tapping a marker opens optional facts on either map. Only answering EDNA records a finding.
- Recorded case findings show a check on their current marker. Viewing a species highlights its corresponding markers on both maps. Details identify the survey era, source and whether a finding has been recorded.
- Clicked species details and their paired-map highlight disappear after 1.5 seconds; another tap restarts that interval. The recorded-finding check remains visible. Hover and keyboard focus retain their transient behavior, and Close can dismiss details sooner. Compact Notebook entries use the case's short gameplay names; species details retain the full name.
- Stable species are explicitly part of the Observe task. Supplementary imported species are labelled as background survey records and do not count towards the required findings.
- Authored case findings remain the source of truth for the five core species. Incompatible upstream detection patterns are rejected before any input is applied; players may explicitly choose **Start standalone case** to use the authored survey instead. Additional species use the same resolved result in map styling and tooltips, including historical non-detections.
- Before the findings are complete, EDNA shows the current question number; the Notebook is available after the first recorded finding. The `Test possible causes` action appears only after all five initial findings are recorded.
- Selecting a historical organism opens its facts and explains that the 20-year survey is a read-only reference.
- All five initial findings must be recorded before Simulate unlocks.
- The domain and UI share the same required-finding calculation; method notes and repeated discoveries cannot substitute for a missing case finding.
- The field notebook records each finding in plain language with its evidence source and confidence, grouped into **Food-web pattern** and **Stable controls**.

The same Notebook remains available in Simulate and Report as a non-destructive side drawer in landscape and a bottom sheet in portrait. Opening it does not change phase or clear the current selection, and its reading position is preserved when it is closed and reopened. Each entry shows whether it is `NEW`, `USED` in a committed comparison, still an `OPEN` question, or selected for the final `REPORT`; evidence related to the currently selected prediction is highlighted without being auto-selected.

The Notebook's **Compare causes** section shows three compact cards with Support, Challenge and Open counts. Only the selected cause expands its short comparison links. These counts come from the player's checks, not probabilities or a ranking of the correct answer. Unreviewed ROV clues and locked follow-up comparisons remain hidden. Keyboard focus scrolls the controls into view.

Closing the Notebook resumes EDNA's previous conversation and reply; an explicitly dismissed conversation stays dismissed. Normal Notebook openings preserve the reading position. The Report dialogue's **Review comparisons** action instead expands and reveals the current explanation's saved checks. EDNA's guided next-check action reveals the evidence choices when they would fall below the viewport, including stacked layouts, without submitting an answer.

During Observe questions after the first finding, and in Simulate and Report, the collapsed Notebook is a small illustrated book with a teal spine, bookmark and finding-count badge. It has no persistent text caption. Hover or keyboard focus identifies its open/close action; activating it opens the existing drawer with a brief transition. Closing the drawer returns focus to the book and preserves the reading position.

The expanded Notebook uses a punched paper margin and six shaded binding rings along its left edge. The same binding appears on the Observe notebook. Text and scrolling content are inset beyond the binding, and the rings remain fixed while the entries scroll; all binding artwork is non-interactive.

### 2 · Simulate

Players test three competing explanations:

- Plastic pollution
- Long-line fishing
- Bottom trawling

The temperature/warming scenario has been removed from this prototype. All seven comparisons, including Sea star under both fishing models, are completed before the first idea. Observe still requires all five core findings.

Running a model reveals its seafloor prediction, physical signs to look for, Shark → Tuna → Krill food-web response, and a separate benthic check for Sea star and Filter-feeding mussel.

Model cards separate check progress (**NOT RUN**, **TO CHECK**,
**CHECKED**, with completed/required counts) from the recorded evidence's
relationship (**SUPPORTS**, **CHALLENGES**, **MIXED EVIDENCE**, or **UNRESOLVED**).
A green check appears only when all required comparisons for that model are
complete. These labels use recorded comparisons rather than the authored answer.

During cause selection, Easy mode gives all available unfinished models equal
pulsing arrows. Choosing a model switches to the **Run simulation** arrow.
Selecting a prediction moves the written instruction into EDNA and adds a down
arrow and a pulsing border to **WHAT WE FOUND**. The evidence cue remains after
an inconclusive selection and disappears when the comparison is settled. All
cues share a stronger, slow pulse; dismissing EDNA hides the decorative cues.

Food-web relationships are stored as explicit predator → prey edges grouped into named networks. The current mystery uses a deliberately simplified `case_simplified` network so its existing evidence and comparison rules remain valid. A separate five-node `reference_main` network and the storyboard's manta, deep-water and benthic branches are also authored. Missing model predictions can be propagated through the selected network, while explicit threat predictions always take precedence.

Evidence choices are shuffled once per model/prediction in each investigation. The relevant option remains in the candidate set, but is not pinned to the first row. Reopening EDNA, revisiting a prediction, or submitting a comparison preserves its order; restarting creates fresh orders.

The simulator’s model column has its own fixed heights, independent of the evidence column. The separate Case Question panel, evidence instruction block and report-gate hint strip have been removed. A reserved scrollbar gutter and pixel-based scroll preservation prevent help from stretching, narrowing or shifting the model cards.

The comparison workspace uses progressive disclosure: players choose a cause, run it, select a prediction, and choose a recorded observation directly under **WHAT WE FOUND**. Selecting the observation immediately checks and records its relationship to the prediction.

The result explains whether the selected finding:

- **Supports the model**
- **Challenges the model**
- **Cannot settle the prediction**

A relevant supporting or challenging finding is saved and locked. Inconclusive or unrelated evidence stays open for another selection and does not advance an objective. The check uses the authored scientific rules and preserves their caveats. Keyboard and pointer selection share this same action; keyboard focus returns to the prediction controls after a comparison is saved.

A compact comparison result remains beside the selected prediction and observation. EDNA explains the result, and **Why?** reveals the authored scientific feedback. Her dialogue header contains the investigation question; its text wraps independently of the model controls. Hints follow the species actually selected by the player, including choices outside the suggested route.

The case contains seven required investigation objectives grouped into four Case Questions. The first five screen plastic, establish the Shark → Tuna → Krill cascade, and show why the two fishing models partly overlap. Sea star is available alongside the other species from the first model run. ROV findings remain separate physical evidence revealed in Report. EDNA follows the selected cause and presents the current investigation question in her dialogue. Easy includes and marks the directly related `GUIDE` clue and can open the next required prediction. Hard keeps the same scientific rules and feedback, offers more evidence candidates without a `GUIDE` marker, and leaves prediction selection to the player.

### 3 · Report

After all seven model checks, players save an initial explanation and enter a three-round conversation:

1. **Look at the clues.** Choose **Inspect former shark habitat** or **Inspect the seafloor**, then sweep the camera frame. Pin the located finding to collect the ROV field-note set and hear the selected note first. The action starts an inspection rather than immediately revealing a result.
2. **Weigh our explanation.** Connect a main clue and a different cross-check to the explanation, choosing from: the shark–tuna–krill pattern, stable sea star and intact seafloor, or ROV fishing line. EDNA explains the value and limits of that clue before offering to keep or change the cause. These choices are not scored and do not change the player's cause or completed checks. **Another clue** revisits the discussion; Notebook comparisons remain available. Looking back at Simulate and returning preserves the selected clue, conversation and draft.
3. **Send our report.** Review the assembled findings, food-web model and scientific caution, then send. Players can return to the explanation or ROV clues before submitting.

The report carries forward the player's explanation, accepted comparisons and both ROV findings. The draft shows the main clue and cross-check linked by the player; the case-closed summary retains the highlighted main clue. It includes the tested food-web mechanism and a scientific limitation, without substituting the correct cause. The former quiz, evidence quotas and repeated mechanism/limitation choices remain absent from the player flow. QA's already prepared report can still be reviewed and sent directly without inventing a player's key-clue choice.

The domain evaluator still checks completeness and scientific support. If a conclusion conflicts with a saved comparison, EDNA explains that specific finding in round two and offers revision or another look. Draft evidence and completed checks remain intact. A correct report opens the case-closed summary and publishes the investigation result. QA **Report** opens the fully prepared third round without submitting it.

## The Missing Predator case

The current vertical slice investigates a repeated shark non-detection, tuna detected at more sites, repeated krill non-detection, and stable benthic indicators.

Long-line fishing and bottom trawling deliberately share the same Shark ↓ / Tuna ↑ / Krill ↓ prediction. Players must use the stable Sea star evidence and intact seafloor to distinguish them. Fishing line near historical shark habitat then strengthens the best-supported Long-line explanation without treating eDNA non-detection as proof of complete absence.

The case currently includes:

- five case-critical species plus a seven-species runtime roster cap;
- a 20-species shared catalog with canonical IDs, scientific names, aliases, trophic roles, depths and habitat tags;
- twelve explicit food-web edges across the current simplified network, the five-node reference chain and three alternative branches;
- three threat models;
- a complete 15-cell Threat × Species prediction matrix;
- seven required comparison objectives;
- two post-provisional ROV observations; and
- an evidence-category-gated final report.

## Difficulty, input, and accessibility

- Easy and Hard change guidance, not the scientific rules or correct answer.
- EDNA introduces Observe milestones, follows each Simulate task, and leads the three-round Report conversation. Her avatar reopens help or returns to the current report conversation.
- EDNA’s dialogue offers What next?, Why? and a dismiss button; the avatar remains available after dismissal. Opening the Notebook temporarily removes the character overlay.
- Restarting a case preserves the selected difficulty; a new scene session starts at Easy.
- Easy/Hard is the only player-facing mode control. Motion stays Full across all stages; old saved Reduced preferences are ignored.
- Pointer, keyboard, and touch interactions share the same gameplay path.
- Species markers use generous hit targets and visible keyboard focus rings.
- Selected, suggested, correct, incorrect, and uncertain states use shape or text in addition to colour.
- The canvas supports adaptive landscape and portrait reference resolutions, Safe Area fitting, pixel-perfect rendering, responsive grids, and preserved scroll/focus state.
- Invalid actions use a temporary warning toast. Incomplete-report checks use neutral guidance instead of failure styling.

## Future mini-game integration

The Investigation remains fully playable as a standalone case while exposing a small integration boundary through `EDNA.Core`:

- optional survey and site metadata can replace the authored labels;
- structured eDNA species observations can specify detected/not detected, depth, historical/current era, confidence, sample quality, source, sample and site;
- canonical species IDs and aliases are normalised to the case's stable internal IDs;
- a deterministic roster selects no more than seven organisms while preserving all case-critical species;
- mapped environmental and physical observation IDs are imported only when their authored unlock stage permits it;
- an input for the wrong case stops with a visible configuration error;
- successful results return the case, survey, site, selected hypothesis, evidence IDs, selected survey-species roster, revisions, and completion status; and
- the legacy flat detected-species list remains accepted while the other mini-games migrate to the richer handoff record.

Detailed current records take precedence over legacy detections for the same species within a result. Imports reject invalid eras/enums, records outside an explicitly supplied site, and conflicting detections for the same named sample; distinct samples may carry different results. These checks run before survey metadata, findings or the roster are applied. Core case presentation continues to identify its authored survey rather than treating one imported sample as the full case evidence.

## Screenshots

### Observe — compare the baseline and current survey

![Ecosystem Detective Observe screen showing historical and current seamount surveys beside the field notebook](docs/images/investigation-observe-seamount.png)

### Simulate — choose and run a cause

![Ecosystem Detective Simulate screen before running the selected Long-line fishing model](docs/images/investigation-simulate-start.png)

### Simulate — compare model predictions with survey evidence

![Ecosystem Detective Simulate screen showing completed cause investigations, food-web predictions, benthic checks, and prediction-versus-survey controls](docs/images/investigation-simulate-analysis.png)

### Report — assemble the final evidence-based explanation

![Ecosystem Detective Report screen showing cause, reasoning, evidence, ROV follow-up, and scientific uncertainty sections](docs/images/investigation-report.png)

## Development and validation

Development builds and the Unity Editor expose an expandable **QA tools** menu
at the bottom-left, with one fully filled checkpoint per stage. Selecting a
checkpoint closes it and restores gameplay input.
It preserves the selected difficulty and uses the real domain API to construct
valid data. Non-development players do not include these controls.

| Checkpoint | State | Ctrl+Shift key |
| --- | --- | --- |
| Observe | All five findings recorded; ready to continue | 1 |
| Simulate | All seven comparisons done; ready to choose a provisional explanation | 2 |
| Report | ROV reviewed, all seven checks done, valid final draft ready to send | 3 |

The case authoring commands generate the three-cause configuration. `eDNA Detectives > Remove Warming Scenario` upgrades older four-cause case assets to this configuration.

Validation commands are available from the Unity menu:

- `eDNA Detectives > Validation > Run EditMode Tests`
- `eDNA Detectives > Validation > Run PlayMode Tests`

The test assemblies are:

- `EDNA.Investigation.EditModeTests`
- `EDNA.Investigation.PlayModeTests`

The project also includes command-line development build validation for the standalone Investigation scene.

## Project structure

- `game/Assets/Scripts/Core` — shared mini-game contracts and enums
- `game/Assets/Scripts/Investigation/Domain` — deterministic case rules and evaluation
- `game/Assets/Scripts/Investigation/Integration` — controller, session bridge, and QA state factory
- `game/Assets/Scripts/Investigation/Presentation` — runtime uGUI, responsive layouts, icons, and animation
- `game/Assets/Data/Investigation/LongLineCase` — species, threats, evidence, objectives, and report configuration
- `game/Assets/Tests/EditMode/Investigation` — domain and authoring validation
- `game/Assets/Tests/PlayMode/Investigation` — scene, UI, accessibility, and full-flow regression tests

## Artwork and licences

The five core organisms use a coordinated set of transparent field-guide illustrations generated for this project. Source files, import settings and the prompt record are documented in [FieldGuide/README.md](game/Assets/Art/Investigation/FieldGuide/README.md).

Threat and scenario artwork continues to use [OpenMoji](https://openmoji.org/), licensed under [CC BY-SA 4.0](game/Assets/Art/Investigation/OpenMoji/LICENSE.txt). The original organism icons are retained in the same folder. Per-file source codes and attribution are recorded in [ATTRIBUTION.md](game/Assets/Art/Investigation/OpenMoji/ATTRIBUTION.md).

Check, cross, question, lock, and Report evidence marks use transparent Heroicons assets under the included MIT licence.

The Observe seamount uses three in-house Blender renders blended in Unity. Its
source and playback settings are recorded in [GENERATION.md](game/Assets/Resources/Investigation/Seamount/GENERATION.md).
The original static fallback retains its [generation record](game/Assets/Art/Investigation/Seamount/GENERATION.md).

The interface uses a compact phase navigation with completion checks, restrained blue surfaces, teal selections, coral primary actions and warm paper for investigation records. Prediction direction is carried by labels and arrows; red and green remain available for comparison feedback. Keyboard focus uses a thin hollow stroke. Water particles remain behind the interface, historical specimens are subdued, and both maps use aligned depth guides. Case Closed adds a brief review-stamp animation.
