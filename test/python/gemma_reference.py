"""Pinned Python token IDs and right-padding oracle. No managed code is loaded."""
import argparse
import hashlib
import json
from pathlib import Path
import random
import subprocess
import sys
import sysconfig

from tokenizers import Tokenizer

REPOSITORY = "onnx-community/embeddinggemma-2-ONNX"
REVISION = "daa72c51243991dfcaf9f9137d2c573d8f7790c0"
ASSET_SHA256 = "4d777ef5bdc1aa36227abdfb77c3e49e7b9c892d16e1b6bda41c393504828be4"
SEED = 20261016


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def corpus(asset):
    added = [t["content"] for t in json.loads(asset.read_text(encoding="utf-8"))["added_tokens"]]
    texts = ["", " ", "  ", "a", "abc", "aaa", "aaaa", "Hello, world!", " task: search result | query: Mars? ",
             "title: none | text: The red planet is Mars.", "é", "e\u0301", "É", "A\u030a", "\u212b", "🧪👩🏽‍💻🌏", "東京 日本語",
             "عَرَبِيّ 中文 한국어", "\x00\x01\x1f\x7f", "\r\n\t\r\n", "\u00a0\u2003\u200b\u202f\ufeff",
             "\u0378\ue000\U0010ffff\U00010000", " a  b\tc\nd\r\ne ", "aaaa" * 2000, " " * 4097, "\t" * 129,
             "\n" * 130, "▁" * 65, *added, "".join(added), "a" + "b".join(added) + "c"]
    rng = random.Random(SEED)
    pieces = [*list("abcdeXYZ0123:/|<>_"), " ", "  ", "\t", "\r\n", "\0", "é", "e\u0301", "▁", "東京", "🧪", "\u00a0", *added]
    for i in range(700):
        if i % 3:
            text = "".join(rng.choice(pieces) for _ in range(rng.randrange(160)))
        else:
            scalars = []
            for _ in range(rng.randrange(120)):
                code = rng.randrange(0x110000)
                if 0xD800 <= code <= 0xDFFF:
                    code = 0xFFFD
                scalars.append(chr(code))
            text = "".join(scalars)
        texts.append(text)
    return texts


def generate(asset, output):
    tokenizer = Tokenizer.from_file(str(asset))
    texts = corpus(asset)
    cases = [{"id": f"case-{i:04}", "text": text, "ids": tokenizer.encode(text).ids,
              "rawIds": tokenizer.encode(text, add_special_tokens=False).ids} for i, text in enumerate(texts)]
    tokenizer.enable_padding(direction="right", pad_id=0, pad_token="<pad>")
    batches = []
    for start in range(0, len(cases), 7):
        indices = list(range(start, min(start + 7, len(cases))))
        encoded = tokenizer.encode_batch([texts[i] for i in indices])
        batches.append({"id": f"batch-{start:04}", "indices": indices, "batchSize": len(indices),
                        "sequenceLength": len(encoded[0].ids), "tokenCounts": [sum(e.attention_mask) for e in encoded],
                        "inputIds": [v for e in encoded for v in e.ids], "attentionMask": [v for e in encoded for v in e.attention_mask]})
    result = {"schemaVersion": 1, "repository": REPOSITORY, "revision": REVISION, "tokenizerSha256": ASSET_SHA256,
              "seed": SEED, "producerSha256": sha256(Path(__file__)), "cases": cases, "batches": batches}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=True) + "\n", encoding="utf-8")
    print(f"PASS Python oracle: {len(cases)} cases, {len(batches)} batches, seed {SEED}")


def verify(reference, actual_path):
    expected = json.loads(reference.read_text(encoding="utf-8"))
    actual = json.loads(actual_path.read_text(encoding="utf-8"))
    assert expected["tokenizerSha256"] == actual["tokenizerSha256"] == ASSET_SHA256
    assert actual["referenceSha256"] == sha256(reference), "Changed oracle manifest"
    for section, keys in (("cases", ("id", "ids", "rawIds")),
                          ("batches", ("id", "batchSize", "sequenceLength", "tokenCounts", "inputIds", "attentionMask"))):
        if len(actual[section]) != len(expected[section]):
            raise AssertionError(f"{section} coverage differs")
        for oracle, managed in zip(expected[section], actual[section]):
            for key in keys:
                if managed[key] != oracle[key]:
                    raise AssertionError(f"First mismatch: {oracle['id']} {key}")
    print(f"PASS exact Python/managed IDs: {len(actual['cases'])} cases, {len(actual['batches'])} padded batches")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--tokenizer", type=Path, default=Path("models/embeddinggemma-2/tokenizer.json"))
    parser.add_argument("--output", type=Path, default=Path("artifacts/gemma/reference.json"))
    parser.add_argument("--download", action="store_true")
    parser.add_argument("--verify", type=Path)
    args = parser.parse_args()
    if args.download:
        hf = Path(sysconfig.get_path("scripts")) / ("hf.exe" if sys.platform == "win32" else "hf")
        subprocess.run([str(hf), "download", REPOSITORY, "tokenizer.json", "--revision", REVISION,
                        "--local-dir", str(args.tokenizer.parent)], check=True)
    if not args.tokenizer.is_file() or sha256(args.tokenizer) != ASSET_SHA256:
        raise ValueError("Missing or changed pinned tokenizer: " + str(args.tokenizer))
    if args.verify:
        verify(args.output, args.verify)
    else:
        generate(args.tokenizer, args.output)


if __name__ == "__main__":
    main()
