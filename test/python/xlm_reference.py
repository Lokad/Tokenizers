"""Independent Rust-tokenizers oracle for the library's documented XLM-R pipeline."""
import argparse
import hashlib
import json
from pathlib import Path
import random
import re
import unicodedata

from sentencepiece import sentencepiece_model_pb2 as protobuf
from tokenizers import AddedToken, Regex, Tokenizer, decoders, models, normalizers, pre_tokenizers

FIXTURE_DIRECTORY = Path("test/Fixture")
MANIFEST = FIXTURE_DIRECTORY/"fixtures.json"
METADATA = json.loads(MANIFEST.read_text(encoding="utf-8"))
FIXTURE = FIXTURE_DIRECTORY/METADATA["model"]["file"]
ASSET = next(item["sha256"] for item in METADATA["files"] if item["path"] == FIXTURE.name)
WHITESPACE = r"[ \t\r\n\u00a0\u1680\u2000-\u200a\u202f\u205f\u3000]"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def backend(lowercase,plain=False):
    if digest(FIXTURE) != ASSET:
        raise ValueError("Changed XLM-R fixture")
    proto = protobuf.ModelProto.FromString(FIXTURE.read_bytes())
    pieces = [(index,p) for index,p in enumerate(proto.pieces) if p.type != protobuf.ModelProto.SentencePiece.CONTROL]
    result = Tokenizer(models.Unigram([(p.piece,p.score) for _,p in pieces],unk_id=0,byte_fallback=False))
    ids = {native:(3 if original == 0 else original+1) for native,(original,_) in enumerate(pieces)}
    steps = [normalizers.Replace(Regex(WHITESPACE)," "),
             normalizers.Replace(Regex(r"[\p{Cc}\p{Cf}\p{Co}\uFFFD]"),""),normalizers.NFKC()]
    if lowercase:
        steps.append(normalizers.Lowercase())
    if not plain:
        result.normalizer = normalizers.Sequence(steps)
    result.pre_tokenizer = pre_tokenizers.Metaspace(replacement="▁",prepend_scheme="always",split=False)
    result.decoder = decoders.Metaspace(replacement="▁",prepend_scheme="always")
    if not plain:
        result.add_special_tokens([AddedToken(t,special=True,normalized=False) for t in ["<s>","</s>","<unk>","<pad>","<mask>"]])
        ids.update({result.token_to_id(t):i for t,i in [("<s>",0),("</s>",2),("<unk>",3),("<pad>",1),("<mask>",len(proto.pieces)+1)]})
    return result,ids


def normalize_origins(text,start,lowercase):
    """Independent prefix-diff alignment; normalization composes entire contributing source spans."""
    prefix,normalized,mapping = "","",[]
    whitespace = set(" \t\r\n\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000")
    for index,character in enumerate(text,start):
        if character in whitespace:
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


def coverage_offsets(plain,text,lowercase,maximum):
    tokenizer,special_ids = plain
    ids,offsets = [],[]
    position = 0
    literals = {"<s>":0,"</s>":2,"<unk>":3,"<pad>":1,"<mask>":250001}
    def ordinary(end):
        prepared,origins = normalize_origins(text[position:end],position,lowercase)
        if not prepared:
            return
        encoded = tokenizer.encode(prepared,add_special_tokens=False)
        ids.extend(special_ids[i] for i in encoded.ids)
        for left,right in encoded.offsets:
            covered = set().union(*origins[left:right])
            offsets.append([min(covered),max(covered)+1])
    for match in re.finditer("|".join(re.escape(t) for t in literals),text):
        ordinary(match.start())
        ids.append(literals[match.group()])
        offsets.append([match.start(),match.end()])
        position = match.end()
    ordinary(len(text))
    return [0,*ids[:maximum-2],2],[None,*offsets[:maximum-2],None]


def encode(backend_data,text,maximum):
    tokenizer,special_ids = backend_data
    encoded = tokenizer.encode(text,add_special_tokens=False)
    ids = [special_ids[i] for i in encoded.ids][:maximum-2]
    offsets = [list(pair) for pair in encoded.offsets][:maximum-2]
    return [0,*ids,2],[None,*offsets,None]


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
    rng = random.Random(20261106)
    pieces = ["abc","XYZ"," ","  ","\t","é","e\u0301","ﬁ","🧪","東京","عربي","𩸽","I","İ","<mask>","<s>","\u00a0","\0"]
    edge.extend("".join(rng.choice(pieces) for _ in range(rng.randrange(40))) for _ in range(120))
    for lower in (False,True):
        for index,text in enumerate(edge):
            yield dict(id=f"unicode-{lower}-{index}",text=text,maximum=512,lowercase=lower)


def regenerate_legacy(backends,plain):
    def write_changed(path,value):
        if path.exists() and json.loads(path.read_text(encoding="utf-8-sig")) == value:
            return
        path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8",newline="\n")
    path = FIXTURE_DIRECTORY/"xlmr-cases.json"
    corpus = json.loads(path.read_text(encoding="utf-8"))
    for case in corpus["singleCases"]:
        ids,_ = encode(backends[False],case["text"],case["maxLength"])
        coverage_ids,offsets = coverage_offsets(plain,case["text"],False,case["maxLength"])
        if ids != coverage_ids:
            raise ValueError("Independent alignment changed fixture segmentation")
        case.update(ids=ids,offsets=offsets)
    for group in corpus["corpora"]:
        expected = [encode(backends[False],text,128)[0] for text in group["inputs"]]
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
        if actual["referenceSha256"] != digest(args.output) or actual["tokenizerSha256"] != ASSET:
            raise ValueError("Changed XLM reference or asset")
        if len(actual["cases"]) != len(reference["cases"]):
            raise ValueError("XLM coverage differs")
        for expected,observed in zip(reference["cases"],actual["cases"]):
            for key in ("id","ids","offsets"):
                if expected[key] != observed[key]:
                    raise ValueError(f"XLM mismatch: {expected['id']} {key}: {expected[key]} != {observed[key]}")
        print(f"PASS independent XLM-R IDs/offsets: {len(actual['cases'])} cases")
        return
    backends = {lower:backend(lower) for lower in (False,True)}
    plain = backend(False,plain=True)
    if args.regenerate_legacy:
        regenerate_legacy(backends,plain)
    results = []
    for item in cases():
        ids,_ = encode(backends[item["lowercase"]],item["text"],item["maximum"])
        coverage_ids,offsets = coverage_offsets(plain,item["text"],item["lowercase"],item["maximum"])
        if coverage_ids != ids:
            raise ValueError("Independent normalized alignment changed segmentation: "+item["id"])
        legacy = item.pop("legacy",None)
        if legacy and ids != legacy["ids"]:
            raise ValueError(f"Legacy token-ID fixture differs from the independent oracle: {item['id']}")
        # Legacy offset fixtures remain tested in .NET, while fresh offsets are independently compared here.
        results.append(dict(**item,ids=ids,offsets=offsets))
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(dict(schemaVersion=1,tokenizerSha256=ASSET,cases=results),ensure_ascii=True)+"\n",encoding="utf-8")
    print(f"PASS independent XLM-R oracle and unchanged legacy IDs: {len(results)} cases")


if __name__ == "__main__":
    main()
