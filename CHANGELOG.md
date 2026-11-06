# Changelog

## 0.2.0 (unreleased)

- Target .NET 10 and use the XML solution format.
- Verify NuGet consumption on .NET 10 and .NET 11 (initial qualification on RC3).
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
