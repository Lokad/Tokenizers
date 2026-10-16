## Overview

Lokad.Tokenizers provides managed Gemma BPE and XLM-R/SentencePiece text encoding on .NET 10. Load tokenizer assets from local files; inference, task prefixes, and model downloads stay with the caller.

## Installation

To install Lokad.Tokenizers, you can use the NuGet package manager:

```
> Install-Package Lokad.Tokenizers
```

## Usage

For Gemma, load the supported `tokenizer.json` once and share the immutable encoder:

```csharp
using Lokad.Tokenizers.Tokenizer;

var gemma = new GemmaTokenizer("models/embeddinggemma-2/tokenizer.json");
long[] ids = gemma.Encode("Hello, world!"); // Includes BOS and EOS.
GemmaTokenizedBatch batch = gemma.EncodeBatch(new[] { "Hello!", "" }, maximumTokens: 512);
// InputIds and AttentionMask are owned, flat, row-major long arrays.
// Shape: [batch.BatchSize, batch.SequenceLength]. Padding is on the right.
```

Gemma support covers the space-replacing, byte-fallback BPE pipeline qualified for EmbeddingGemma 2. IDs match the pinned Python tokenizer exactly; unsupported JSON settings fail explicitly. ASCII spaces become U+2581, while other whitespace and Unicode distinctions remain intact. Literal added tokens are recognized. `Encode(text, false)` omits automatic BOS/EOS. Batches include special tokens in the limit and reject overflow rather than truncate. Invalid UTF-16 fails explicitly. This API encodes single sequences and batches; it does not expose decoding or paired sequences.

Here is an example of how to use the `XLMRobertaTokenizer`:

```csharp

using Lokad.Tokenizers.Tokenizer;

// ...

var vocab_path = "models/xlm-roberta/sentencepiece.bpe.model";


// Create an instance of the XLMRobertaTokenizer
var xlmRobertaTokenizer = new XLMRobertaTokenizer(vocab_path, false);

// Define the input text to be tokenized
var inputText = "Hello, world!";

// Tokenize the input text
var result = xlmRobertaTokenizer.Encode(inputText, null, 128, TruncationStrategy.LongestFirst, 0);

// Access the tokenized output
var tokenIds = result.TokenIds;
var tokenOffsets = result.TokenOffsets;

// Process the tokenized output as needed
// ...

```



## Contributing

Use the stable SDK selected by `global.json`:

```powershell
dotnet restore Lokad.Tokenizers.slnx --locked-mode --tl:off -v minimal
dotnet build Lokad.Tokenizers.slnx -c Release --tl:off --nologo -v minimal --no-restore
dotnet test Lokad.Tokenizers.slnx -c Release --tl:off --nologo -v minimal --no-build
```

Ordinary tests use the checked-in XLM-R fixture and a tiny synthetic Gemma vocabulary. Full Gemma qualification downloads only the pinned tokenizer JSON, with Python as an independent oracle:

```powershell
python -m venv artifacts/python
# Windows: artifacts/python/Scripts/python.exe; Linux: artifacts/python/bin/python
artifacts/python/Scripts/python.exe -m pip install -r test/python/requirements.txt
pwsh -NoProfile -File eng/test-gemma.ps1 -Python artifacts/python/Scripts/python.exe -Download
```

The lane checks 754 single-sequence cases with and without special tokens and 108 padded batches, including seeded Unicode fuzzing and exact token-limit rejection. Its revision and SHA-256 checks prevent silently changing tokenizer assets. Results remain under ignored `artifacts/gemma/`.

Create packages explicitly in Release; ordinary builds do not pack:

```powershell
dotnet pack src/Lokad.Tokenizers.csproj -c Release --tl:off --nologo -v minimal --no-build --no-restore
pwsh -NoProfile -File tools/PackageSmokeTest/verify-package.ps1
```

Packages and portable symbols go under `artifacts/nuget/`. The smoke check inspects package contents/dependencies and runs a consumer outside the checkout with a fresh NuGet cache. GitHub CI runs these checks on Windows and Ubuntu.

Contributions are welcome! Please feel free to submit a pull request or open an issue on GitHub.

## License

BSD 3-Clause; see [LICENSE.txt](LICENSE.txt).

## References

- [rust-tokenizers](https://github.com/guillaume-be/rust-tokenizers)
