#!/usr/bin/env python3
"""Render local .puml files to PNG via the public PlantUML server."""
import glob, os, sys, zlib, urllib.request

_MAP = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_"

def _enc3(b1, b2, b3):
    c1 = b1 >> 2
    c2 = ((b1 & 0x3) << 4) | (b2 >> 4)
    c3 = ((b2 & 0xF) << 2) | (b3 >> 6)
    c4 = b3 & 0x3F
    return _MAP[c1] + _MAP[c2] + _MAP[c3] + _MAP[c4]

def encode(data: bytes) -> str:
    out = []
    for i in range(0, len(data), 3):
        chunk = data[i:i+3]
        if len(chunk) == 3:
            out.append(_enc3(chunk[0], chunk[1], chunk[2]))
        elif len(chunk) == 2:
            out.append(_enc3(chunk[0], chunk[1], 0))
        else:
            out.append(_enc3(chunk[0], 0, 0))
    return "".join(out)

def main():
    here = os.path.dirname(os.path.abspath(__file__))
    for puml in sorted(glob.glob(os.path.join(here, "*.puml"))):
        text = open(puml, encoding="utf-8").read()
        comp = zlib.compressobj(9, zlib.DEFLATED, -15)
        deflated = comp.compress(text.encode("utf-8")) + comp.flush()
        url = "https://www.plantuml.com/plantuml/png/~1" + encode(deflated)
        out = puml[:-5] + ".png"
        try:
            with urllib.request.urlopen(url, timeout=40) as r:
                open(out, "wb").write(r.read())
            print(f"OK  {os.path.basename(out)}  ({os.path.getsize(out)} bytes)")
        except Exception as e:
            print(f"ERR {os.path.basename(puml)}: {e}", file=sys.stderr)
            sys.exit(1)

if __name__ == "__main__":
    main()
