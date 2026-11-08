"""Independent Rust-tokenizers oracle for the library's documented XLM-R pipeline."""
import argparse
from collections.abc import Mapping
from dataclasses import dataclass
import hashlib
import json
from pathlib import Path
import random
import re
import unicodedata
from types import MappingProxyType

from sentencepiece import sentencepiece_model_pb2 as protobuf
from tokenizers import Tokenizer, models, pre_tokenizers

FIXTURE_DIRECTORY = Path("test/Fixture")
MANIFEST = FIXTURE_DIRECTORY/"fixtures.json"
METADATA = json.loads(MANIFEST.read_text(encoding="utf-8"))
FIXTURE = FIXTURE_DIRECTORY/METADATA["model"]["file"]
ASSET = next(item["sha256"] for item in METADATA["files"] if item["path"] == FIXTURE.name)
SPACE = frozenset(" \t\r\n\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000")
ALL_SPACE = SPACE | frozenset("\v\f\u0085\u2028\u2029")
UNICODE_VERSION = "15.1.0"
LITERAL_PATTERN = re.compile(r"<s>|</s>|<unk>|<pad>|<mask>")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


@dataclass(frozen=True)
class Backend:
    tokenizer: Tokenizer
    ids: Mapping[int,int]
    literals: Mapping[str,int]


def backend():
    if unicodedata.unidata_version != UNICODE_VERSION:
        raise RuntimeError("XLM-R qualification requires Unicode 15.1.0 (Python 3.13)")
    if digest(FIXTURE) != ASSET:
        raise ValueError("Changed XLM-R fixture")
    proto = protobuf.ModelProto.FromString(FIXTURE.read_bytes())
    pieces = [(index,p) for index,p in enumerate(proto.pieces) if p.type != protobuf.ModelProto.SentencePiece.CONTROL]
    result = Tokenizer(models.Unigram([(p.piece,p.score) for _,p in pieces],unk_id=0,byte_fallback=False))
    ids = {native:(3 if original == 0 else original+1) for native,(original,_) in enumerate(pieces)}
    # Normalize with the explicitly versioned, independent Python UCD, not Rust's
    # different Unicode tables. Split literals before this step, as the API does.
    result.pre_tokenizer = pre_tokenizers.Metaspace(replacement="▁",prepend_scheme="always",split=False)
    literals = {"<s>":0,"</s>":2,"<unk>":3,"<pad>":1,"<mask>":len(proto.pieces)+1}
    return Backend(result,MappingProxyType(ids),MappingProxyType(literals))


def chunks(text):
    position = 0
    for match in LITERAL_PATTERN.finditer(text):
        yield position,match.start(),False
        yield match.start(),match.end(),True
        position = match.end()
    yield position,len(text),False


def prepare(text,lowercase):
    cleaned = "".join(" " if c in SPACE else c for c in text
                      if c in SPACE or unicodedata.category(c) not in ("Cc","Cf","Co") and c != "\ufffd")
    normalized = unicodedata.normalize("NFKC",cleaned)
    return "".join(c.lower() for c in normalized) if lowercase else normalized


def normalize_origins(text,start,lowercase):
    """Independent prefix-diff alignment; normalization composes entire contributing source spans."""
    prefix,normalized,mapping = "","",[]
    for index,character in enumerate(text,start):
        if character in SPACE:
            character = " "
        elif unicodedata.category(character) in ("Cc","Cf","Co") or character == "\ufffd":
            continue
        prefix += character
        updated = unicodedata.normalize("NFKC",prefix)
        left = 0
        while left < min(len(normalized),len(updated)) and normalized[left] == updated[left]:
            left += 1
        right = 0
        while right < min(len(normalized)-left,len(updated)-left) and normalized[-1-right] == updated[-1-right]:
            right += 1
        old_end,new_end = len(normalized)-right,len(updated)-right
        origins = {index}
        for group in mapping[left:old_end]:
            origins.update(group)
        mapping = mapping[:left]+[origins.copy() for _ in range(left,new_end)]+(mapping[old_end:] if right else [])
        normalized = updated
    if lowercase:
        lowered,new_mapping = "",[]
        for character,origins in zip(normalized,mapping):
            lowered += character.lower()
            new_mapping.extend(origins.copy() for _ in character.lower())
        normalized,mapping = lowered,new_mapping
    return normalized,mapping


def coverage_offsets(data,text,lowercase,maximum):
    ids,offsets,references = [],[],[]
    if all(c in ALL_SPACE for c in text):
        return [0,2],[None,None],[[],[]]
    for position,end,literal in chunks(text):
        if literal:
            ids.append(data.literals[text[position:end]])
            offsets.append([position,end])
            references.append(list(range(position,end)))
            continue
        prepared,origins = normalize_origins(text[position:end],position,lowercase)
        if not prepared:
            continue
        encoded = data.tokenizer.encode(prepared,add_special_tokens=False)
        ids.extend(data.ids[i] for i in encoded.ids)
        for left,right in encoded.offsets:
            covered = set().union(*origins[left:right])
            offsets.append([min(covered),max(covered)+1])
            references.append(sorted(covered))
    return [0,*ids[:maximum-2],2],[None,*offsets[:maximum-2],None],[[],*references[:maximum-2],[]]


def encode(data,text,maximum,lowercase=False):
    ids=[]
    if not all(c in ALL_SPACE for c in text):
        for start,end,literal in chunks(text):
            if literal:
                ids.append(data.literals[text[start:end]])
            else:
                prepared=prepare(text[start:end],lowercase)
                if prepared:
                    ids.extend(data.ids[i] for i in data.tokenizer.encode(prepared,add_special_tokens=False).ids)
    return [0,*ids[:maximum-2],2]


def cases():
    corpus = json.loads(Path("test/Fixture/xlmr-cases.json").read_text(encoding="utf-8"))
    for case in corpus["singleCases"]:
        yield dict(id=case["name"],text=case["text"],maximum=case["maxLength"],lowercase=False,legacy=case)
    for group in corpus["corpora"]:
        expected = json.loads((Path("test/Fixture")/group["expectedFile"]).read_text(encoding="utf-8-sig"))
        if len(expected) != len(group["inputs"]):
            raise ValueError("Golden corpus row count differs: "+group["name"])
        for index,(text,ids) in enumerate(zip(group["inputs"],expected)):
            yield dict(id=group["name"]+f"-{index}",text=text,maximum=128,lowercase=False,legacy=dict(ids=ids))
    edge = ["é<mask>","🧪<mask>é<mask>𩸽","🧪ABC","𩸽XYZ","I","İ","ﬁx","e\u0301x","가x","a\u0301\u0327z","Hello, world!","Café 東京!","\0a\u200bb","＜s＞","＜mask＞"]
    edge += ["㋿","\ua7cb","\U0001e030","\U0001ccf0","\u1c89","\U00010400","\ufffe","\uffff","\U0010ffff",
             "\u2028","a\u2028b","\u0085","\v\f","a\u200db","\U000f0000x","\ufdfa","e\u0301<mask>ﬁ🧪🧪"]
    rng = random.Random(20261106)
    pieces = ["abc","XYZ"," ","  ","\t","é","e\u0301","ﬁ","🧪","東京","عربي","𩸽","I","İ","<mask>","<s>","\u00a0","\0"]
    edge.extend("".join(rng.choice(pieces) for _ in range(rng.randrange(40))) for _ in range(120))
    for lower in (False,True):
        for index,text in enumerate(edge):
            yield dict(id=f"unicode-{lower}-{index}",text=text,maximum=512,lowercase=lower)


def regenerate_legacy(data):
    def write_changed(path,value):
        if path.exists() and json.loads(path.read_text(encoding="utf-8-sig")) == value:
            return
        path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8",newline="\n")
    path = FIXTURE_DIRECTORY/"xlmr-cases.json"
    corpus = json.loads(path.read_text(encoding="utf-8"))
    for case in corpus["singleCases"]:
        ids = encode(data,case["text"],case["maxLength"])
        coverage_ids,offsets,_ = coverage_offsets(data,case["text"],False,case["maxLength"])
        if ids != coverage_ids:
            raise ValueError("Independent alignment changed fixture segmentation")
        case.update(ids=ids,offsets=offsets)
    for group in corpus["corpora"]:
        expected = [encode(data,text,128) for text in group["inputs"]]
        write_changed(FIXTURE_DIRECTORY/group["expectedFile"],expected)
    write_changed(path,corpus)
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    for entry in manifest["files"]:
        asset = FIXTURE_DIRECTORY/entry["path"]
        entry.update(sha256=digest(asset),size=asset.stat().st_size)
    write_changed(MANIFEST,manifest)
    print("PASS independently regenerated legacy expectations and fixture checksums")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,default=Path("artifacts/xlm/reference.json"))
    parser.add_argument("--verify",type=Path)
    parser.add_argument("--regenerate-legacy",action="store_true")
    args = parser.parse_args()
    if args.verify:
        reference = json.loads(args.output.read_text(encoding="utf-8"))
        actual = json.loads(args.verify.read_text(encoding="utf-8"))
        if reference["unicodeVersion"] != UNICODE_VERSION or actual["unicodeVersion"] != UNICODE_VERSION:
            raise ValueError("Changed Unicode profile version")
        if actual["referenceSha256"] != digest(args.output) or actual["tokenizerSha256"] != ASSET:
            raise ValueError("Changed XLM reference or asset")
        if len(actual["cases"]) != len(reference["cases"]):
            raise ValueError("XLM coverage differs")
        for expected,observed in zip(reference["cases"],actual["cases"]):
            for key in ("id","ids","offsets","references"):
                if expected[key] != observed[key]:
                    raise ValueError(f"XLM mismatch: {expected['id']} {key}: {expected[key]} != {observed[key]}")
        print(f"PASS independent XLM-R IDs/offsets: {len(actual['cases'])} cases")
        return
    data = backend()
    normal = encode(data,"HELLO",128)
    if encode(data,"HELLO",128,True) != encode(data,"hello",128) or encode(data,"HELLO",128) != normal:
        raise ValueError("Shared model casing modes are not independent")
    if args.regenerate_legacy:
        regenerate_legacy(data)
    results = []
    for item in cases():
        ids = encode(data,item["text"],item["maximum"],item["lowercase"])
        coverage_ids,offsets,references = coverage_offsets(data,item["text"],item["lowercase"],item["maximum"])
        if coverage_ids != ids:
            raise ValueError("Independent normalized alignment changed segmentation: "+item["id"])
        legacy = item.pop("legacy",None)
        if legacy and ids != legacy["ids"]:
            raise ValueError(f"Legacy token-ID fixture differs from the independent oracle: {item['id']}")
        # Legacy offset fixtures remain tested in .NET, while fresh offsets are independently compared here.
        results.append(dict(**item,ids=ids,offsets=offsets,references=references))
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(dict(schemaVersion=2,unicodeVersion=UNICODE_VERSION,tokenizerSha256=ASSET,cases=results),ensure_ascii=True)+"\n",encoding="utf-8")
    print(f"PASS independent XLM-R oracle and unchanged legacy IDs: {len(results)} cases")


if __name__ == "__main__":
    main()
