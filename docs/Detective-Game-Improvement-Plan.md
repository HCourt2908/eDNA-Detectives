# Detective Game — Current Status and Next Steps

Updated 2026-09-09. The current gameplay specification is
[ECOSYSTEM_DETECTIVE.md](../ECOSYSTEM_DETECTIVE.md); implementation details for
Act 2 are in [Act2-Scenario-Comparison.md](Act2-Scenario-Comparison.md).

## Completed

- Observe starts with Today and records the current and historical surveys separately. EDNA introduces the notebook, species and change categories with highlights before classification.
- Players drag species or select a category to compare findings, including stable records. Incorrect attempts do not reveal the answer or add a penalty.
- Players explicitly summarize and save both dated surveys. Recording and guide animations can be skipped without skipping the comparison decisions.
- Investigate keeps three cause cards in a consistent layout, with title icons and separate Play/Replay controls.
- Relative imagery changes from 3 to 5, 3 to 1 or 3 to 3, supported by progress and local highlights. An interrupted first playback does not count as completed.
- EDNA remains available, and the notebook uses an icon button. The main screen stays within the viewport; the notebook scrolls independently.
- The ending summarizes the five existing findings and model comparisons. Players record their conclusion or return to compare; it adds no ROV, fishing-line or seabed evidence.
- The conclusion identifies the best match among the three tested models. It does not claim proof of a cause or that a species is completely absent.
- QA completion checkpoints, exported evidence and conclusion logic use the survey/model path.

## Maintenance

Startup, navigation, input, mode, notebook and completion tests follow the current
flow. Superseded interaction checks have been removed or replaced, while legacy
domain report rules retain compatibility coverage. Test reports, review notes and
superseded planning history stay local. See the
[development instructions](../ECOSYSTEM_DETECTIVE.md#development-and-validation)
for the test suites and Unity validation commands.

## Follow-up Work

1. Validate input/output with the actual identification minigame producer. Standalone examples and contract tests do not establish completed integration.
2. Run player studies to check understanding of detection changes versus animal counts, and why stable species matter.
3. Validate WebGL, touch input and performance on the intended deployment devices. A macOS development build covers only one target.
4. Review scientific content and update rule and flow coverage before adding ecological relationships, survey cases or changes to the conclusion.
