# Tokenizer fixtures

`fixtures.json` records file lengths and SHA-256 checksums. Ordinary .NET tests verify these files offline; they never download assets. Git attributes fix JSON fixtures to LF line endings, and regeneration writes LF explicitly, so checksums remain identical across Windows and Linux checkouts.

The checked-in SentencePiece model is byte-identical to `sentencepiece.bpe.model` in `FacebookAI/xlm-roberta-large-finetuned-conll03-english` at revision `18f95e9924f3f452df09cc90945073906ef18f1e`. This source was verified on 2026-11-06. The original acquisition revision is unknown. To independently retrieve the verified copy without model weights:

```powershell
hf download FacebookAI/xlm-roberta-large-finetuned-conll03-english sentencepiece.bpe.model --revision 18f95e9924f3f452df09cc90945073906ef18f1e --local-dir artifacts/xlm-provenance
```

`xlmr-cases.json` imports the original 12 directly asserted cases and five multilingual input corpora from repository commit `912c4c923376928bcc3d7ba28734b2d2c9af88c3`. Their expected IDs and offsets were preserved. The five existing JSON files hold the corpus IDs. Test code checks every row and validates all parallel metadata arrays.

`test/python/xlm_reference.py` independently verifies these IDs with one shared Rust unigram backend. Python 3.13's Unicode 15.1 data supplies independent cleaning, NFKC and scalar casing; Rust's differing Unicode normalizers are not used. Literal specials are recognized before normalization. The library's legacy profile preserves repeated whitespace and uses right truncation; it does not promise arbitrary native SentencePiece preprocessing. A separate Python prefix-diff alignment verifies complete source spans and every reference position. Versioned edge cases, including noncharacters, are included without changing the original fixture IDs.

From the repository root, install `test/python/requirements.txt` in a virtual environment and run `eng/test-xlm.ps1 -Python <venv-python>`. To intentionally regenerate expected data, run `python test/python/xlm_reference.py --regenerate-legacy`, review every change, and rerun the .NET tests and oracle. The regeneration command updates checksums only after computing expectations independently of managed code.

`test/Unicode/NormalizationTest-15.1.0.txt.gz` is the official Unicode normalization conformance corpus, generated from a checksum-pinned archive by `eng/generate-unicode.py`. Offline tests verify its decompressed checksum and all five-column relationships for NFC, NFD, NFKC and NFKD. `eng/test-unicode.ps1` additionally compares all scalars with independent Python 3.13 results. See `src/Unicode/README.md` for provenance and license details.

The C# protobuf source is generated from the single schema in `src/Vocab/proto/` by `eng/generate-protobuf.ps1`. The compiler version and package checksum are pinned; `-Check` verifies that regeneration reproduces the checked-in source.
