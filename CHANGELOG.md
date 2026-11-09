# Changelog

## 0.2.0

### Compatibility and migration

- Target .NET 10; .NET 11 applications can consume the same `net10.0` assembly.
- Change `IVocab` and vocabulary dictionaries to `IReadOnlyDictionary`, copy construction inputs and special-token configuration, and require controlled dictionary additions. Update custom `IVocab` implementations accordingly.
- Make `Node.ScalarReferences` the authoritative read-only source mapping. `ReferenceOffsets` returns a copy; use `Node.FromScalarReferences` to reconstruct mappings that include composition or expansion.
- Require derived templates to insert a content-independent number of tokens and derived XLM-R tokenizers to override `WithLowerCase` when preserving additional behavior.
- Reject malformed UTF-16, invalid tokenizer assets, unsupported model profiles and malformed flat-file byte sequences explicitly. Flat files support strict UTF-8 and BOM-identified UTF-8, UTF-16 and UTF-32.
- Retain callable tokenizer constructors and deprecate legacy public character tables and unused exception types for compatibility.

### Added

- Managed Gemma BPE encoding from local `tokenizer.json` assets, with literal added tokens, UTF-8 byte fallback, optional BOS/EOS and right-padded batch inputs with attention masks.
- Managed Unicode 15.1 normalization, default scalar casing and character properties, independent of host ICU/NLS and SDK Unicode updates.
- Complete source-position mappings through normalization, including composition, expansion, reordering, supplementary scalars and paired inputs.
- Shared XLM-R vocabulary/model instances through casing variants, immutable piece inspection through `EnumeratePieces`, and explicit owned model snapshots through `Freeze`/`IsFrozen`.
- Generic tokenizer interface support and supplied-vocabulary file constructors.

### Fixed and improved

- Preserve unknown model IDs independently of diagnostic text and dictionary additions. Apply consistent empty-input behavior after cleanup while recognizing literal specials before normalization.
- Correct Unicode literal matching, invariant casing, SentencePiece decoding, unknown runs, Japanese punctuation masks, continuation masks and typed/object `Offset` equality.
- Keep paired truncation, stride ownership, overflow order and all parallel metadata aligned. Public template builders and cloned tokens own their mutable metadata.
- Preserve vocabulary IDs and inverse views across idempotent additions. Evaluate caller iterators outside mutation locks and retain exception/overflow atomicity.
- Replace repeated prefix scans with shared failure-link matching, bound sparse literal indexing by occurrences, reject impossible token budgets early and reduce model/metadata allocations and retention. Custom trie comparers retain their lookup semantics.
- Validate Gemma tagged alternatives, duplicate vocabulary entries, special-token mappings, predecessor chains and cumulative score ranges with contextual diagnostics.

### Packaging and qualification

- Keep the Google.Protobuf dependency floor and generated bindings at 3.33.5; verify isolated .NET 10 and .NET 11 consumers explicitly pinned to that runtime.
- Ship explicit Release packages, portable symbols, typed SourceLink mappings, XML API documentation, a 128×128 icon, release notes and Unicode data licensing.
- Verify offline fixture integrity, the official Unicode normalization corpus, independent Gemma/XLM-R results, exhaustive Unicode scalars, matching-work budgets and symbol/generation negative controls in Windows/Ubuntu CI.
- Use a stable .NET 10 SDK with feature-band roll-forward, locked dependencies and warning-free builds. Keep CI development packages separate from release artifacts.

## 0.1.1

- Existing XLM-R/SentencePiece tokenizer API.
