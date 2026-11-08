# Changelog

## 0.2.0 (unreleased)

- Preserve paired stride ownership and Japanese punctuation masks; add discriminating continuation-mask goldens.
- Replace long-prefix rescanning with shared failure-link matching while retaining unigram ties and live public trie edits.
- Reject impossible XLM-R template limits early; transfer internal metadata and defer normalization maps until text changes.
- Align typed/object Offset equality and raw segmentation UTF-16 policy; identify malformed escaped JSON strings by asset path.
- Pin managed normalization, scalar casing and character properties to Unicode 15.1; qualify every scalar independently and run the official normalization conformance corpus offline.
- Copy mutable template metadata and qualify full source-reference vectors, including composition and sequence B.
- Reject extreme cumulative-score ranges and duplicate JSON vocabulary entries with explicit diagnostics.
- Avoid rune boxing and unchanged preprocessing copies; share the Python reference model, separate independent Gemma tests and refresh DTO documentation.
- Reject malformed tokenizer assets, duplicate token fields, misaligned template inputs and invalid predecessor chains with explicit diagnostics.
- Preserve whitespace-only literal specials, qualify exact paired templates, and verify that package symbols match the shipped DLL.
- Reduce model retention, prefix construction and metadata allocations; share loaded assets between casing configurations.
- Clarify dictionary-only additions and unreleased installation, and expand independent performance workloads.
- Target .NET 10 and use the XML solution format.
- Verify NuGet consumption on .NET 10 and .NET 11 (initial qualification on RC3).
- Keep the Google.Protobuf dependency floor and generated bindings at 3.33.5, verified by an isolated consumer pinned to that runtime.
- Replace the package icon with a simpler tokenizer motif at 128×128.
- Add local-file Gemma BPE encoding and right-padded batch inputs.
- Qualify Gemma IDs against the pinned Python tokenizer.
- Adopt explicit Release NuGet packaging with symbols, SourceLink, and a tokenizer-specific icon.
- Add locked dependencies, offline XLM-R fixtures, and Windows/Linux CI.
- Fix Unicode special-token scanning, invariant lowercase, decoding, normalization offsets, unknown runs, and clone ownership.
- Make vocabulary views read-only, copy construction inputs, preserve IDs across repeated additions, and validate mapping JSON.
- Implement the generic tokenizer interface and supplied-vocabulary file constructors; consolidate aligned truncation.
- Parse XLM-R protobufs once, reject unsupported model settings, and remove quadratic suffix allocations.
- Recognize literal tokens in one pass and reject impossible Gemma limits before allocating BPE state.
- Consolidate and pin protobuf generation; refresh SDK/dependencies and enforce warning-free builds.
- Allow stable .NET 10 SDK feature-band roll-forward and install the latest 10.0.x in CI.
- Skip Markdown-only CI changes, pack explicitly, and keep CI packages under artifacts/dev/nuget/.
- Add offline fixture integrity checks, malformed-asset regressions, independent XLM-R qualification, and reproducible benchmarks.
- Deprecate legacy character tables and unused exceptions while retaining their public compatibility surface.

## 0.1.1

- Existing XLM-R/SentencePiece tokenizer API.
