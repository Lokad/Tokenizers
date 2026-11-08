## Overview

Lokad.Tokenizers provides managed Gemma BPE and XLM-R text encoding on .NET 10 and .NET 11. The NuGet package ships a `net10.0` assembly that .NET 11 applications can consume. Callers provide local tokenizer assets, model downloads, inference, and any task prefixes.

## Installation

The examples on this branch describe the **unreleased 0.2 API**. Installing the published package below provides the stable release; Gemma and the new vocabulary contracts require a local 0.2 package from this checkout. Build it with the Contributing commands, add `artifacts/nuget/` as a feed alongside nuget.org in the consuming project, and select version `0.2.0`. The package smoke script demonstrates a complete isolated restore and consumer.

```powershell
Install-Package Lokad.Tokenizers
```

## Usage

Load a Gemma tokenizer once and share the immutable encoder:

```csharp
using Lokad.Tokenizers.Tokenizer;

var gemma = new GemmaTokenizer("models/embeddinggemma-2/tokenizer.json");
long[] ids = gemma.Encode("Hello, world!"); // Includes BOS and EOS.
GemmaTokenizedBatch batch = gemma.EncodeBatch(new[] { "Hello!", "" }, maximumTokens: 512);
// Owned, flat, row-major InputIds and AttentionMask arrays.
// Shape: [batch.BatchSize, batch.SequenceLength]. Padding is on the right.
```

Gemma supports the space-replacing, byte-fallback BPE pipeline qualified for EmbeddingGemma 2. ASCII spaces become U+2581; other whitespace and Unicode distinctions remain intact. Literal added tokens are recognized before normalization. `Encode(text, false)` omits automatic BOS/EOS. Batches count special tokens toward the limit and reject overflow rather than truncate. Invalid UTF-16 and incompatible asset settings fail explicitly. This API encodes single sequences and batches; it does not expose decoding or paired sequences.

For XLM-R, load one local SentencePiece protobuf:

```csharp
var xlm = new XLMRobertaTokenizer("models/xlm-roberta/sentencepiece.bpe.model", lowerCase: false);
var encoded = xlm.Encode("Hello, world!", null, 128, TruncationStrategy.LongestFirst, 0);
string decoded = xlm.Decode(encoded.TokenIds, skipSpecialTokens: true, cleanUpTokenizationSpaces: false);
// decoded: "Hello, world!"
```

XLM-R accepts the qualified `nmt_nfkc` unigram profile with standard SentencePiece special IDs. Incompatible model types, normalization profiles, byte fallback, piece types and excessively large scores fail during loading. Its legacy preprocessing cleans control characters, applies Unicode NFKC, preserves repeated whitespace, and optionally applies scalar lowercase. Normalization, default scalar casing and Unicode character properties are pinned to **Unicode 15.1.0** in managed data, independent of host ICU/NLS or SDK Unicode updates. Characters introduced after that version remain unassigned; well-formed noncharacters are accepted, and invalid UTF-16 is rejected. The generic tokenizer uses the same Unicode profile with its existing CJK splitting ranges.

XLM-R encodes single or paired sequences. Offsets count Unicode scalars in the original string, including every contributing position when normalization composes characters. Segment IDs identify sequence 0/1 for paired inputs. Public template builders copy mutable offsets and reference lists so inputs and results can be changed independently. `SpecialTokensMask` marks inserted template tokens; `Mask` also describes literal special and unknown pieces.

Vocabulary dictionaries are read-only views. `AddTokens` and idempotent `AddExtraIds` add ordinary dictionary entries and support ID lookup/decoding; they do not register literal special tokens or change the XLM-R segmentation model. A basic tokenizer can emit a new entry only when its existing splitting rules produce that entire token. Adding `<extra_id_0>` does not make that punctuation-containing string a recognized sentinel. Finish dictionary additions before concurrent encoding. Construction copies caller-owned vocabulary and mapping data. Custom `IVocab` implementations expose read-only dictionary properties and can be passed to `BaseTokenizer<T>` directly. File constructors support the supplied flat-file and XLM-R vocabularies.

Use `xlm.WithLowerCase(true)` to create a lowercase encoder sharing the loaded vocabulary and segmentation model. Both instances can encode concurrently after vocabulary setup is complete.

Literal tokens use greedy longest matching. Unigram matching uses failure links and visits actual vocabulary matches while preserving legacy float-score tie-breaking. Loaded models compile this matcher at construction. Accessing the mutable raw `SentencePieceModel.Root` trie or returning mutable nodes from `CommonPrefixSearch` switches segmentation to fresh snapshots so subsequent edits remain visible; finish all edits before concurrent encoding. Trie children must form a tree.

Paired `LongestFirst` overflow retains reverse removal order. Stride comes from the sequence that lost tokens when only one side is trimmed; when both sides are trimmed, it uses the first sequence's retained suffix. Raw segmentation, like the high-level tokenizer, rejects malformed UTF-16 and accepts Unicode noncharacters. `Offset` equality requires the same runtime type and equal coordinates.

Special-token mapping JSON accepts snake_case and legacy PascalCase keys. Values can be strings or token objects with `content`. Unsupported matching flags and unknown fields fail explicitly. Optional XLM-R names retain standard defaults. BOS/CLS and EOS/SEP must share names, and additional special tokens must already exist in the vocabulary.

## Contributing

Use a stable .NET 10 SDK. `global.json` allows any stable .NET 10 SDK from 10.0.100 onward; CI installs the latest `10.0.x`. Builds treat warnings as errors:

```powershell
dotnet restore Lokad.Tokenizers.slnx --locked-mode --tl:off -v minimal
dotnet build Lokad.Tokenizers.slnx -c Release --no-restore --tl:off --nologo -v minimal
dotnet test Lokad.Tokenizers.slnx -c Release --no-build --no-restore --tl:off --nologo -v minimal
```

Ordinary tests verify checked-in fixture hashes, the official Unicode normalization corpus, and run offline. Independent qualification uses **Python 3.13 (Unicode 15.1)** and pinned dependencies:

```powershell
python -m venv artifacts/python
$taskPython = if ($IsWindows) { "artifacts/python/Scripts/python.exe" } else { "artifacts/python/bin/python" }
& $taskPython -m pip install -r test/python/requirements.txt
./eng/test-gemma.ps1 -Python $taskPython -Download
./eng/test-xlm.ps1 -Python $taskPython
./eng/test-unicode.ps1 -Python $taskPython
```

Gemma qualification checks 754 single-sequence cases with and without automatic special tokens and 108 padded batches. XLM-R qualification checks 344 legacy/Unicode cases, including lowercase, complete source spans and full reference-position lists. Its modes share one Rust model; Python's explicitly versioned Unicode implementation supplies independent normalization/casing and prefix-diff alignment. Exhaustive qualification compares all 1,112,064 Unicode scalars for casing, character properties and four normalization forms. The reference producers load no managed code. Results remain under ignored `artifacts/gemma/`, `artifacts/xlm/` and `artifacts/unicode/`. [Fixture provenance and regeneration](test/Fixture/README.md) describe the pinned model and legacy expectations.

Generate the compact managed Unicode tables and official offline normalization corpus with `python eng/generate-unicode.py`; use `--check` for verification. The script downloads a checksum-pinned Unicode 15.1 archive, and the package includes the Unicode data license. [Unicode data provenance](src/Unicode/README.md) records the format and qualification policy.

Generate protobuf bindings from the single schema with `./eng/generate-protobuf.ps1`. Pass `-Check` to verify the checked-in output using a pinned compiler and checksum. Both the compiler and the minimum Google.Protobuf runtime version are 3.33.5. Run the allocation and special-token benchmark with:

```powershell
dotnet run --project tools/TokenizerBenchmarks -c Release -- models/embeddinggemma-2/tokenizer.json test/Fixture/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model
```

The benchmark emits JSON lines for construction/retained memory, ordinary text, Unicode normalization, offsets, ID conversion, paired truncation, batches, literals and vocabulary additions. Inputs are prepared outside steady-state measurements; construction is a separate single sample. Allocation counts and local timings describe those workloads, not a general throughput guarantee.

Create packages explicitly in Release; ordinary builds do not pack:

```powershell
dotnet pack src/Lokad.Tokenizers.csproj -c Release --no-build --no-restore --tl:off --nologo -v minimal
./tools/PackageSmokeTest/verify-package.ps1
```

Local packages and portable symbols go under `artifacts/nuget/`. The smoke check inspects dependencies, symbols, SourceLink, and the 128x128 icon, then runs a consumer outside the checkout with a fresh NuGet cache and an exact Google.Protobuf 3.33.5 reference. It verifies that this exact runtime is loaded while exercising XLM-R and Gemma. With both SDKs/runtimes installed, pass `-ConsumerFrameworks net10.0,net11.0` in PowerShell to test both. Initial .NET 11 qualification used RC3; CI resolves the latest SDK from Microsoft's .NET 11 release metadata, including prereleases.

The Windows/Ubuntu CI workflow skips Markdown-only changes and runs locked restore, Release build, tests, explicit packaging, both independent tokenization lanes, and both package consumers. CI packages go under `artifacts/dev/nuget/`. The consumer check uses `-SkipPack` to verify the package produced by the pack stage, including DLL/PDB identity and SourceLink.

## API compatibility

The unreleased 0.2 release changes `IVocab`/vocabulary dictionary properties to `IReadOnlyDictionary` and returns a copied special-token configuration. Update custom implementations and use controlled additions instead of mutating dictionary views. Invalid or unsupported mappings now fail during loading. Existing callable tokenizer constructors remain available. Legacy public character tables and unused exception types are deprecated for compatibility; encoders no longer depend on their mutable tables.

## License

BSD 3-Clause; see [LICENSE.txt](LICENSE.txt).

## References

- [rust-tokenizers](https://github.com/guillaume-be/rust-tokenizers)
