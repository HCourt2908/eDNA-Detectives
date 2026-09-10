# Act 2 — Five-species Figma comparison

The active game uses `reference_main`: shark → tuna → herring → krill →
phytoplankton. Arrows represent the predator-to-food relation. The five actors
remain in this order in the saved survey and in every prediction card.

Long-line fishing and bottom trawling retain the same predator-removal seed.
`FoodWebCascadeEvaluator.FillMissingPredictions` now derives the remaining four
responses along the actual five-node network. Both resulting patterns match the
illustrative survey. The authored `supportedModelThreatIds` therefore includes
both fishing IDs; no unobserved benthic or physical clue selects one over the other.

Plastic remains a provisional comparison at the user's request. Unknown responses
are explicit and do not resolve to a directional Match or Mismatch. Its retained
known directions can still conflict with the example survey.

Play/Replay is explicit. Each 7.2-second animation starts at a common three-symbol
baseline, sequentially highlights all five links and holds the final result.
Only completed runs enable the comparison action. Header/card clicks are passive.
EDNA and the notebook remain available in a fixed-height dock; the workspace fits
its viewport and does not scroll vertically. Notebook reading scrolls separately.
The right-facing EDNA portrait sits to the left of her dialogue: neutral during
ordinary play and conclusions, and raised-hand during spotlight guidance. The
notebook and continuation control share the lower-right part of the dock.

The notebook offers an optional **Food-chain examples** reference containing the
other Figma networks. These are explanatory relationships, not collected survey
evidence. Expanding or closing them never advances progress.

After either matching model is reviewed, the ending names long-line fishing as
the main explanation and bottom trawling as a possible alternative. This order is
the team's authored case direction, not a confidence calculation from the shared
pattern. EDNA acknowledges a reviewed alternative. Record conclusion completes
the activity without an independent-evidence request or additional task.

Exports retain the reviewed model and compatible models, and explicitly include
`primaryHypothesisId=longline` and `alternativeHypothesisIds=[bottom_trawling]`.
The former `needsMoreEvidence` flag is removed. Only the five Observe records are
exported; legacy ROV fields remain excluded from this route.

See [the current specification](../ECOSYSTEM_DETECTIVE.md) for the authored survey,
limitations, integration contract and development commands. Test reports stay local.
