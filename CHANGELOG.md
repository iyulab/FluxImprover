# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

## [0.18.0] - Unreleased

### Added
- **Contextual enrichment can ask about several chunks in one call.** `ContextualEnrichmentOptions.ChunksPerCall` (default
  1, as before) groups adjacent chunks into windows: one prompt carries the document context once and the window's chunks,
  and the model answers with a JSON array of one summary per chunk. A window whose answer is not such an array, has the
  wrong count or is cut off is asked again chunk by chunk. `MaxTokens` applies per chunk.

### Changed
- **The document profile is built once per document** in `EnrichBatchAsync` instead of once per chunk. Prompts are the
  same.

## [0.17.0] - 2026-10-06

### Changed
- **Contextual enrichment no longer sends the whole document with every chunk.** A document longer than
  `ContextualEnrichmentOptions.MaxDocumentContextLength` (new, default 6000 characters) goes to the model as a profile —
  its opening and heading outline, the same for every chunk — plus the text around the chunk. A long document used to
  overflow a small serving context on every chunk, and the cost grew as chunks times document length. Set it to 0 to
  send the whole document, as before (models whose context holds your longest documents).
- Re-pinned sibling package(s) `LMSupply.Generator` 0.107.0 -> 0.108.0, `LMSupply.Generator.Onnx` 0.107.0 -> 0.108.0.

## [0.16.3] - 2026-10-06

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.106.1 -> 0.107.0, `LMSupply.Generator.Onnx` 0.106.1 -> 0.107.0. No source changes.

## [0.16.2] - 2026-10-06

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.106.0 -> 0.106.1, `LMSupply.Generator.Onnx` 0.106.0 -> 0.106.1.

### Fixed
- **Breaking** (released as a patch) — **cancelling a call now cancels it.** 6 method(s) that take a `CancellationToken` caught every exception to
  return a fallback (`null`, an empty result, a failure value) or to log and continue, and treated the caller's own
  cancellation the same way. They now let the caller's `OperationCanceledException` through; other failures behave
  as before.
  Migration: code that relied on a cancelled call returning `null`, an empty result or a failure value now
  receives `OperationCanceledException` — catch it where a cancellation is expected.

## [0.16.1] - 2026-10-05

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.105.2 -> 0.106.0, `LMSupply.Generator.Onnx` 0.105.2 -> 0.106.0. No source changes.

## [0.16.0] - 2026-10-05

### Removed
- **Breaking: `FluxImprover.Utilities.CollectionExtensions`** (`Batch`, `Shuffle`, `ForEachAsync`, `SelectAsync`,
  `TakeRandom`, `SafeGet`, `IsNullOrEmpty`). Nothing in FluxImprover used them, and two of them started asynchronous
  work a caller could not cancel. Migration: `Enumerable.Chunk` for `Batch`, `Parallel.ForEachAsync` (which takes a
  `CancellationToken`) for `ForEachAsync`/`SelectAsync`, and plain LINQ for the rest.

## [0.15.12] - 2026-10-05

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.105.1 -> 0.105.2, `LMSupply.Generator.Onnx` 0.105.1 -> 0.105.2. No source changes.

## [0.15.11] - 2026-10-05

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.105.0 -> 0.105.1, `LMSupply.Generator.Onnx` 0.105.0 -> 0.105.1. No source changes.

## [0.15.10] - 2026-10-04

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.104.0 -> 0.105.0, `LMSupply.Generator.Onnx` 0.104.0 -> 0.105.0. No source changes.

## [0.15.9] - 2026-10-04

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.103.0 -> 0.104.0, `LMSupply.Generator.Onnx` 0.103.0 -> 0.104.0. No source changes.

## [0.15.8] - 2026-10-04

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.102.0 -> 0.103.0, `LMSupply.Generator.Onnx` 0.102.0 -> 0.103.0. No source changes.

## [0.15.7] - 2026-10-04

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.101.0 -> 0.102.0, `LMSupply.Generator.Onnx` 0.101.0 -> 0.102.0. No source changes.

## [0.15.6] - 2026-10-03

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.100.0 -> 0.101.0, `LMSupply.Generator.Onnx` 0.100.0 -> 0.101.0. No source changes.

## [0.15.5] - 2026-10-03

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.99.0 -> 0.100.0, `LMSupply.Generator.Onnx` 0.99.0 -> 0.100.0. No source changes.

## [0.15.4] - 2026-10-03

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.98.2 -> 0.99.0, `LMSupply.Generator.Onnx` 0.98.2 -> 0.99.0. No source changes.

## [0.15.3] - 2026-10-02

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.98.1 -> 0.98.2, `LMSupply.Generator.Onnx` 0.98.1 -> 0.98.2. No source changes.

## [0.15.2] - 2026-10-02

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.98.0 -> 0.98.1, `LMSupply.Generator.Onnx` 0.98.0 -> 0.98.1. No source changes.

## [0.15.1] - 2026-10-01

### Changed
- Re-pinned sibling package(s) `Flux.Abstractions` 0.26.0 -> 0.27.0. No source changes.

## [0.15.0] - 2026-10-01

### Changed
- **Chunk summaries and keywords ask the model not to reason** — `EnrichmentOptions.Thinking` (new, default `Off`) is sent
  with the summary and keyword calls. A reasoning model on its template default could spend the whole `MaxTokens` budget
  (512) thinking; the cut-off summary was then dropped and the chunk stored no summary. Set `Thinking = Auto` (and raise
  `MaxTokens`) to let it reason. Same choice as `ContextualEnrichmentOptions.Thinking` (0.14.0).
- Every package now carries the `LICENSE` text, not only the MIT expression.

## [0.14.24] - 2026-10-01

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.97.0 -> 0.98.0, `LMSupply.Generator.Onnx` 0.97.0 -> 0.98.0. No source changes.

## [0.14.23] - 2026-10-01

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.96.0 -> 0.97.0, `LMSupply.Generator.Onnx` 0.96.0 -> 0.97.0. No source changes.

## [0.14.22] - 2026-09-30

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.95.0 -> 0.96.0, `LMSupply.Generator.Onnx` 0.95.0 -> 0.96.0. No source changes.

## [0.14.21] - 2026-09-30

### Changed
- **Documentation comments describe behaviour only.** Code comments and test descriptions no longer carry internal references.
- Re-pinned sibling package(s) `LMSupply.Generator` 0.94.0 -> 0.95.0, `LMSupply.Generator.Onnx` 0.94.0 -> 0.95.0.

## [0.14.20] - 2026-09-30

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.93.1 -> 0.94.0, `LMSupply.Generator.Onnx` 0.93.1 -> 0.94.0. No source changes.

## [0.14.19] - 2026-09-30

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.93.0 -> 0.93.1, `LMSupply.Generator.Onnx` 0.93.0 -> 0.93.1 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.18] - 2026-09-29

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.92.1 -> 0.93.0, `LMSupply.Generator.Onnx` 0.92.1 -> 0.93.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.17] - 2026-09-29

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.92.0 -> 0.92.1, `LMSupply.Generator.Onnx` 0.92.0 -> 0.92.1 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.16] - 2026-09-29

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.91.0 -> 0.92.0, `LMSupply.Generator.Onnx` 0.91.0 -> 0.92.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.15] - 2026-09-29

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.90.0 -> 0.91.0, `LMSupply.Generator.Onnx` 0.90.0 -> 0.91.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.14] - 2026-09-29

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.89.0 -> 0.90.0, `LMSupply.Generator.Onnx` 0.89.0 -> 0.90.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.13] - 2026-09-28

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.88.0 -> 0.89.0, `LMSupply.Generator.Onnx` 0.88.0 -> 0.89.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.12] - 2026-09-28

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.87.0 -> 0.88.0, `LMSupply.Generator.Onnx` 0.87.0 -> 0.88.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.11] - 2026-09-28

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.86.0 -> 0.87.0, `LMSupply.Generator.Onnx` 0.86.0 -> 0.87.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.10] - 2026-09-27

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.85.0 -> 0.86.0, `LMSupply.Generator.Onnx` 0.85.0 -> 0.86.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.9] - 2026-09-27

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.84.0 -> 0.85.0, `LMSupply.Generator.Onnx` 0.84.0 -> 0.85.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.8] - 2026-09-27

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.83.0 -> 0.84.0, `LMSupply.Generator.Onnx` 0.83.0 -> 0.84.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.7] - 2026-09-27

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.81.1 -> 0.83.0, `LMSupply.Generator.Onnx` 0.81.1 -> 0.83.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.6] - 2026-09-27

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.80.0 -> 0.81.1, `LMSupply.Generator.Onnx` 0.80.0 -> 0.81.1 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.5] - 2026-09-26

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.79.1 -> 0.80.0, `LMSupply.Generator.Onnx` 0.79.1 -> 0.80.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.4] - 2026-09-26

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.79.0 -> 0.79.1, `LMSupply.Generator.Onnx` 0.79.0 -> 0.79.1 — re-consumption of already-consumed iyulab packages.

### Fixed
- **`using var scope = provider.CreateScope()` no longer throws on dispose with `AddFluxImproverWithLMSupply`.** The completion service is registered Scoped by default and implemented only `IAsyncDisposable`, and a container or scope disposed with `Dispose()` throws on such a service ("type only implements IAsyncDisposable"); it now implements `IDisposable` too.

## [0.14.3] - 2026-09-26

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.78.0 -> 0.79.0, `LMSupply.Generator.Onnx` 0.78.0 -> 0.79.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.2] - 2026-09-26

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.77.0 -> 0.78.0, `LMSupply.Generator.Onnx` 0.77.0 -> 0.78.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.1] - 2026-09-26

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.76.0 -> 0.77.0, `LMSupply.Generator.Onnx` 0.76.0 -> 0.77.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.14.0] - 2026-09-26

### Added
- **Contextual enrichment no longer lets the model reason by default.** `ContextualEnrichmentOptions.Thinking` (default `Off`) is sent with every context request. A reasoning model left on its template default spent most of the 512-token budget thinking about a one-sentence summary, and roughly one generation in ten was cut off and discarded, leaving that chunk without context. Set `Thinking = ThinkingMode.Auto` to restore the previous behaviour.

## [0.13.3] - 2026-09-25

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.75.0 -> 0.76.0, `LMSupply.Generator.Onnx` 0.75.0 -> 0.76.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.13.2] - 2026-09-24

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.74.0 -> 0.75.0, `LMSupply.Generator.Onnx` 0.74.0 -> 0.75.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.13.1] - 2026-09-24

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.73.0 -> 0.74.0, `LMSupply.Generator.Onnx` 0.73.0 -> 0.74.0 — re-consumption of already-consumed iyulab packages. No source changes.

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
- Re-pinned sibling package(s) `Flux.Abstractions` 0.25.0 -> 0.26.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.12.19] - 2026-09-23

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.72.0 -> 0.72.1 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.12.18] - 2026-09-23

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.71.0 -> 0.72.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.12.17] - 2026-09-22

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.70.0 -> 0.71.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.12.16] - 2026-09-21

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.69.0 -> 0.70.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.12.15] - 2026-09-21

### Changed
- Re-pinned sibling package(s) `LMSupply.Generator` 0.68.3 -> 0.69.0 — re-consumption of already-consumed iyulab packages. No source changes.

## [0.12.14] - 2026-09-19

This file starts at 0.12.14. Changes in earlier releases were not recorded here; the commit history is
the record for them.
