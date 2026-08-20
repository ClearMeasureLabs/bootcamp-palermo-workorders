#!/usr/bin/env python3
import sys, re

def normalize_svg_content(text: str) -> str:
    # Remove PlantUML processing instruction
    text = re.sub(r"<\?plantuml[^>]*\?>\s*", "", text, flags=re.I)
    # Remove title tags and their content
    text = re.sub(r"<title>.*?</title>", "", text, flags=re.S|re.I)
    # Remove XML comments
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    # Remove metadata blocks
    text = re.sub(r"<metadata>.*?</metadata>", "", text, flags=re.S|re.I)
    # Remove common non-deterministic attributes from the root svg tag
    text = re.sub(r"\s(width|height|viewBox|version|id|xml:space|xmlns:[^=]+)\=\"[^\"]*\"", "", text, flags=re.I)
    # Remove empty lines and trim whitespace
    lines = [line.strip() for line in text.splitlines() if line.strip()]
    return "\n".join(lines)

if __name__ == '__main__':
    if len(sys.argv) != 3:
        print("Usage: normalize_svg.py <input.svg> <output.norm>", file=sys.stderr)
        sys.exit(2)
    inp = sys.argv[1]
    out = sys.argv[2]
    with open(inp, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    norm = normalize_svg_content(content)
    with open(out, 'w', encoding='utf-8') as f:
        f.write(norm)
