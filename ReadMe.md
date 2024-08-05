## Overview

Lokad.Tokenizers is a C#/.NET library that provides tokenization functionalities similar to the `rust-tokenizers` library. It is designed to work with various tokenization models, including the `XLMRobertaTokenizer` model used for `multilingual-e5-large` (text embedding).

## Installation

To install Lokad.Tokenizers, you can use the NuGet package manager:

```
> Install-Package Lokad.Tokenizers
```

## Usage

Here is an example of how to use the `XLMRobertaTokenizer`:

```csharp

using Lokad.Tokenizers.Tokenizer;

// ...

var vocab_path = TestUtils.DownloadFileToCache("https://cdn.huggingface.co/xlm-roberta-large-finetuned-conll03-english-sentencepiece.bpe.model");


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

Contributions are welcome! Please feel free to submit a pull request or open an issue on GitHub.

## License

MIT License

## References

- [rust-tokenizers](https://github.com/guillaume-be/rust-tokenizers)
