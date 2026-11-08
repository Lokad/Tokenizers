"""Generate compact Unicode 15.1 tables and the official offline conformance corpus."""
import argparse
import gzip
import hashlib
import io
from pathlib import Path
import struct
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parent.parent
VERSION = "15.1.0"
SHA256 = "cb1c663d053926500cd501229736045752713a066bd75802098598b7a7056177"


def records(text):
    for line in text.splitlines():
        line = line.split("#", 1)[0].strip()
        if line:
            yield [part.strip() for part in line.split(";")]


def points(value):
    bounds = value.split("..")
    return range(int(bounds[0], 16), int(bounds[-1], 16) + 1)


def generate(archive):
    properties, combining, quick = (bytearray(0x110000) for _ in range(3))
    lower, decomposition, canonical = {}, {}, {}
    first = None
    for row in records(archive.read("UnicodeData.txt").decode("utf-8")):
        code, name, category = int(row[0], 16), row[1], row[2]
        if name.endswith(", First>"):
            first = code
            continue
        start = first if name.endswith(", Last>") else code
        first = None
        flags = (1 if category in ("Cc", "Cf", "Co") else 0) | (2 if category.startswith("P") else 0) | (4 if category == "Mn" else 0)
        properties[start:code+1] = bytes([flags]) * (code + 1 - start)
        combining[start:code+1] = bytes([int(row[3])]) * (code + 1 - start)
        if row[13]:
            lower[code] = [int(row[13], 16)]
        if row[5]:
            parts = row[5].split()
            compatibility = parts[0].startswith("<")
            sequence = [int(value, 16) for value in parts[int(compatibility):]]
            decomposition[code] = (compatibility, sequence)
            if not compatibility:
                canonical[code] = sequence
    for row in records(archive.read("SpecialCasing.txt").decode("utf-8")):
        if not row[4]:  # Default, context-free casing; final sigma remains scalar casing.
            lower[int(row[0], 16)] = [int(value, 16) for value in row[1].split()]
    lower = {code:values for code,values in lower.items() if values != [code]}
    for row in records(archive.read("PropList.txt").decode("utf-8")):
        if row[1] == "White_Space":
            for code in points(row[0]):
                properties[code] |= 8
    exclusions = set()
    bits = {"NFD_QC":1, "NFKD_QC":2, "NFC_QC":4, "NFKC_QC":8}
    for row in records(archive.read("DerivedNormalizationProps.txt").decode("utf-8")):
        if row[1] == "Full_Composition_Exclusion":
            exclusions.update(points(row[0]))
        if row[1] in bits and row[2] in ("N", "M"):
            for code in points(row[0]):
                quick[code] |= bits[row[1]]
    compositions = {(values[0],values[1]):code for code,values in canonical.items() if len(values)==2 and code not in exclusions}
    result = io.BytesIO()
    result.write(b"LTUCD151")
    def integer(value):
        result.write(struct.pack("<i", value))
    for data in (properties, combining, quick):
        ranges = []
        start = 0
        for end in range(1, len(data)+1):
            if end == len(data) or data[end] != data[start]:
                if data[start]:
                    ranges.append((start,end-1,data[start]))
                start = end
        integer(len(ranges))
        for start,end,value in ranges:
            integer(start)
            integer(end)
            result.write(bytes([value]))
    integer(len(lower))
    for code,values in sorted(lower.items()):
        integer(code)
        result.write(bytes([len(values)]))
        for value in values:
            integer(value)
    integer(len(decomposition))
    for code,(compatibility,values) in sorted(decomposition.items()):
        integer(code)
        result.write(bytes([int(compatibility),len(values)]))
        for value in values:
            integer(value)
    integer(len(compositions))
    for (left,right),code in sorted(compositions.items()):
        integer(left)
        integer(right)
        integer(code)
    corpus = io.BytesIO()
    with gzip.GzipFile(fileobj=corpus, mode="wb", filename="", mtime=0) as compressed:
        compressed.write(archive.read("NormalizationTest.txt"))
    return {ROOT/"src/Unicode/Unicode15_1.bin":result.getvalue(),
            ROOT/"test/Unicode/NormalizationTest-15.1.0.txt.gz":corpus.getvalue()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    package = ROOT/"artifacts/unicode"/VERSION/"UCD.zip"
    package.parent.mkdir(parents=True, exist_ok=True)
    if not package.exists():
        urllib.request.urlretrieve(f"https://www.unicode.org/Public/zipped/{VERSION}/UCD.zip", package)
    if hashlib.sha256(package.read_bytes()).hexdigest() != SHA256:
        raise ValueError("Pinned Unicode archive checksum mismatch")
    with zipfile.ZipFile(package) as archive:
        outputs = generate(archive)
    for path,data in outputs.items():
        if args.check:
            existing = path.read_bytes() if path.exists() else None
            # gzip encoders can differ across zlib versions; qualify its exact
            # decompressed official corpus, while table bytes must match exactly.
            matches = existing is not None and (gzip.decompress(existing) == gzip.decompress(data) if path.suffix == ".gz" else existing == data)
            if not matches:
                raise ValueError("Unicode output differs: "+str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print(f"PASS Unicode {VERSION} {'generation check' if args.check else 'generation'}: "+", ".join(f"{p.name} ({len(d)} bytes)" for p,d in outputs.items()))


if __name__ == "__main__":
    main()
