## Overview

Lokad.Tokenizers provides managed Gemma BPE and XLM-R text encoding on .NET 10 and .NET 11. The NuGet package ships a `net10.0` assembly that .NET 11 applications can consume. Callers provide local tokenizer assets, model downloads, inference, and any task prefixes.

## Installation

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

XLM-R accepts the qualified `nmt_nfkc` unigram profile with standard SentencePiece special IDs. Incompatible model types, normalization profiles, byte fallback and piece types fail during loading. Its legacy preprocessing cleans control characters, applies Unicode NFKC, preserves repeated whitespace, and optionally applies invariant lowercase. It encodes single or paired sequences. Offsets count Unicode scalars in the original string, including complete spans that compose during normalization. Segment IDs identify sequence 0/1 for paired inputs. `SpecialTokensMask` marks inserted template tokens; `Mask` also describes literal special and unknown pieces.

Vocabulary dictionaries are read-only views. Use `AddTokens` or idempotent `AddExtraIds` for controlled additions, and finish additions before concurrent encoding. Construction copies caller-owned vocabulary and mapping data. Custom `IVocab` implementations expose read-only dictionary properties and can be passed to `BaseTokenizer<T>` directly. File constructors support the supplied flat-file and XLM-R vocabularies.

Special-token mapping JSON accepts snake_case and legacy PascalCase keys. Values can be strings or token objects with `content`. Unsupported matching flags and unknown fields fail explicitly. Optional XLM-R names retain standard defaults. BOS/CLS and EOS/SEP must share names, and additional special tokens must already exist in the vocabulary.

## Contributing

Use a stable .NET 10 SDK. `global.json` allows any stable .NET 10 SDK from 10.0.100 onward; CI installs the latest `10.0.x`. Builds treat warnings as errors:

```powershell
dotnet restore Lokad.Tokenizers.slnx --locked-mode --tl:off -v minimal
dotnet build Lokad.Tokenizers.slnx -c Release --no-restore --tl:off --nologo -v minimal
dotnet test Lokad.Tokenizers.slnx -c Release --no-build --no-restore --tl:off --nologo -v minimal
```

Ordinary tests verify checked-in fixture hashes and run offline. Independent Python qualification uses pinned dependencies:

```powershell
python -m venv artifacts/python
$taskPython = if ($IsWindows) { "artifacts/python/Scripts/python.exe" } else { "artifacts/python/bin/python" }
& $taskPython -m pip install -r test/python/requirements.txt
./eng/test-gemma.ps1 -Python $taskPython -Download
./eng/test-xlm.ps1 -Python $taskPython
```

Gemma qualification checks 754 single-sequence cases with and without automatic special tokens and 108 padded batches. XLM-R qualification checks 310 legacy/Unicode cases, including lowercase and complete source spans. The oracles load no managed code. Results remain under ignored `artifacts/gemma/` and `artifacts/xlm/`. [Fixture provenance and regeneration](test/Fixture/README.md) describe the pinned model and legacy expectations.

Generate protobuf bindings from the single schema with `./eng/generate-protobuf.ps1`. Pass `-Check` to verify the checked-in output using a pinned compiler and checksum. Both the compiler and the minimum Google.Protobuf runtime version are 3.33.5. Run the allocation and special-token benchmark with:

```powershell
dotnet run --project tools/TokenizerBenchmarks -c Release -- models/embeddinggemma-2/tokenizer.json
```

Create packages explicitly in Release; ordinary builds do not pack:

```powershell
dotnet pack src/Lokad.Tokenizers.csproj -c Release --no-build --no-restore --tl:off --nologo -v minimal
./tools/PackageSmokeTest/verify-package.ps1
```

Local packages and portable symbols go under `artifacts/nuget/`. The smoke check inspects dependencies, symbols, SourceLink, and the 128x128 icon, then runs a consumer outside the checkout with a fresh NuGet cache and an exact Google.Protobuf 3.33.5 reference. It verifies that this exact runtime is loaded while exercising XLM-R and Gemma. With both SDKs/runtimes installed, pass `-ConsumerFrameworks net10.0,net11.0` in PowerShell to test both. Initial .NET 11 qualification used RC3; CI resolves the latest SDK from Microsoft's .NET 11 release metadata, including prereleases.

The Windows/Ubuntu CI workflow skips Markdown-only changes and runs locked restore, Release build, tests, explicit packaging, both independent tokenization lanes, and both package consumers. CI packages go under `artifacts/dev/nuget/`. The consumer check uses `-SkipPack` to verify the package produced by the pack stage.

## API compatibility

The unreleased 0.2 release changes `IVocab`/vocabulary dictionary properties to `IReadOnlyDictionary` and returns a copied special-token configuration. Update custom implementations and use controlled additions instead of mutating dictionary views. Invalid or unsupported mappings now fail during loading. Existing callable tokenizer constructors remain available. Legacy public character tables and unused exception types are deprecated for compatibility; encoders no longer depend on their mutable tables.

## License

BSD 3-Clause; see [LICENSE.txt](LICENSE.txt).

## References

- [rust-tokenizers](https://github.com/guillaume-be/rust-tokenizers)
