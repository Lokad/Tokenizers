"""Independent exhaustive Unicode 15.1 primitives from CPython, not managed tables."""
import argparse
import json
from pathlib import Path
import unicodedata

VERSION="15.1.0"
FORMS=("NFC","NFD","NFKC","NFKD")
WHITE=frozenset([*range(9,14),0x20,0x85,0xa0,0x1680,*range(0x2000,0x200b),0x2028,0x2029,0x202f,0x205f,0x3000])


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output",type=Path,default=Path("artifacts/unicode/reference.json"))
    args=parser.parse_args()
    if unicodedata.unidata_version!=VERSION:
        raise RuntimeError("Unicode qualification requires Python 3.13 / Unicode 15.1.0")
    cases=[]
    properties=bytearray(0x110000)
    count=0
    for code in range(0x110000):
        if 0xd800<=code<=0xdfff:
            continue
        text=chr(code)
        lowered=text.lower()
        normalized=[unicodedata.normalize(form,text) for form in FORMS]
        if lowered!=text or any(value!=text for value in normalized):
            cases.append([code,lowered,*normalized])
        category=unicodedata.category(text)
        properties[code]=(1 if category in ("Cc","Cf","Co") else 0)|(2 if category.startswith("P") else 0)|(4 if category=="Mn" else 0)|(8 if code in WHITE else 0)
        count+=1
    ranges=[]
    start=0
    for end in range(1,len(properties)+1):
        if end==len(properties) or properties[end]!=properties[start]:
            if properties[start]:
                ranges.append([start,end-1,properties[start]])
            start=end
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(dict(unicodeVersion=VERSION,scalars=count,cases=cases,properties=ranges),ensure_ascii=True)+"\n",encoding="utf-8")
    print(f"PASS independent Unicode {VERSION}: {count} scalars, {len(cases)} nonidentity mappings")


if __name__=="__main__":
    main()
