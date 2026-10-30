#!/usr/bin/env python3
"""Lists the fonts each deck asks for, resolving theme references (+mn-lt, +mj-lt). Writes results/fonts.json.

usage: font_inventory.py ROOT
"""
import collections, json, re, sys, zipfile
from pathlib import Path

root = Path(sys.argv[1]).resolve()
corpus = root / "corpus"
TYPEFACE = re.compile(rb'<a:(latin|ea|cs|sym)\s[^>]*typeface="([^"]*)"')
THEME = re.compile(rb'<a:(major|minor)Font>\s*<a:latin\s[^>]*typeface="([^"]*)"')
report = {}
for deck in sorted(corpus.rglob("*.pptx")):
    if ".git" in deck.relative_to(corpus).parts:
        continue
    counts = collections.Counter()
    with zipfile.ZipFile(deck) as package:
        themes = {}
        for name in package.namelist():
            if re.fullmatch(r"ppt/theme/theme1\.xml", name):
                themes = {kind.decode(): face.decode() for kind, face in THEME.findall(package.read(name))}
        for name in package.namelist():
            if re.fullmatch(r"ppt/slides/slide\d+\.xml", name):
                for kind, face in TYPEFACE.findall(package.read(name)):
                    if kind == b"latin" and face:
                        counts[face.decode()] += 1
    report[str(deck.relative_to(corpus))] = {"theme": themes, "explicit_latin": counts.most_common()}
(root / "results" / "fonts.json").write_text(json.dumps(report, indent=1))
totals = collections.Counter()
for deck in report.values():
    for face in set(deck["theme"].values()) | {f for f, _ in deck["explicit_latin"]}:
        totals[face] += 1
for face, count in totals.most_common():
    print(f"{count:3} decks  {face}")
