# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

## [0.14.0] - 2026-09-26

### Added
- **Contextual enrichment no longer lets the model reason by default.** `ContextualEnrichmentOptions.Thinking` (default `Off`) is sent with every context request. A reasoning model left on its template default spent most of the 512-token budget thinking about a one-sentence summary, and roughly one generation in ten was cut off and discarded, leaving that chunk without context. Set `Thinking = ThinkingMode.Auto` to restore the previous behaviour.

## [0.13.3] - 2026-09-25

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.75.0 -> 0.76.0, `LMSupply.Generator.Onnx` 0.75.0 -> 0.76.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.13.2] - 2026-09-24

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.74.0 -> 0.75.0, `LMSupply.Generator.Onnx` 0.74.0 -> 0.75.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.13.1] - 2026-09-24

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.73.0 -> 0.74.0, `LMSupply.Generator.Onnx` 0.73.0 -> 0.74.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.13.0] - 2026-09-23

### Added
- **`CompletionOptions.ThrowOnTruncation`: a cut-off answer can be reported instead of returned.** When set, a service
  that can see why the model stopped throws `Flux.Abstractions.TextCompletionTruncatedException` for an answer cut off
  at `MaxTokens`. The meaning and the exception type match `TextCompletionOptions.ThrowOnTruncation`, so one `catch`
  covers both contracts. `OpenAICompatibleCompletionService` (from `finish_reason`) and `LMSupplyCompletionService` (from
  the generator's finish reason) honour it on `CompleteAsync`. The default is `false`.

### Changed
- **A summary or context cut off at `MaxTokens` is no longer stored.** `SummarizationService.SummarizeAsync` now asks
  for the signal and throws `TextCompletionTruncatedException`. `ChunkEnrichmentService` then leaves the chunk's
  summary empty and keeps its keywords, and `ContextualEnrichmentService` leaves `ContextSummary` empty. Before, the
  cut-off text was stored as if it were complete. **Breaking** for a direct caller of `SummarizeAsync` whose
  `MaxTokens` is too small for its `MaxSummaryLength`: catch the exception or raise `MaxTokens`. `SummarizeBatchAsync`
  calls it per text and does not catch, so one cut-off summary fails the batch.
- `LMSupplyCompletionService.CompleteAsync` uses `IGeneratorModel.GenerateChatCompleteResultAsync` (LMSupply.Generator
  0.73.0). It returns the same text, and it reports the finish reason.

### Fixed
- `EnrichmentOptions.MaxSummaryLength` is documented as a number of **words**: the prompt asks for "approximately N
  words". The docs said characters.

## [0.12.20] - 2026-09-23

### Changed
- Re-pinned sibling package(s) `Flux.Abstractions` 0.25.0 -> 0.26.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.12.19] - 2026-09-23

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.72.0 -> 0.72.1 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.12.18] - 2026-09-23

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.71.0 -> 0.72.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.12.17] - 2026-09-22

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.70.0 -> 0.71.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.12.16] - 2026-09-21

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.69.0 -> 0.70.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.12.15] - 2026-09-21

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.68.3 -> 0.69.0 — re-consumption of already-consumed iyulab packages via `check-pin-drift.ps1 -Fix`. No source changes.

## [0.12.14] - 2026-09-19

This file starts at 0.12.14. Changes in earlier releases were not recorded here; the commit history is
the record for them.
