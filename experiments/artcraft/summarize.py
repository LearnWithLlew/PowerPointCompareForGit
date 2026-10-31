#!/usr/bin/env python3
"""Prints summary tables from the results. usage: summarize.py ROOT"""
import collections, json, sys
from pathlib import Path
results = Path(sys.argv[1]).resolve() / "results"
read = lambda name: [json.loads(line) for line in (results / name).read_text().splitlines()]
runs, slides, features, consistency = read("runs.jsonl"), read("slides.jsonl"), read("features.jsonl"), read("consistency.jsonl")
print("runs by exit:", collections.Counter((r["renderer"], r["mode"], r["exit"]) for r in runs))
for r in runs:
    if r["exit"] != 0:
        print("FAILED", r["deck"], r["renderer"], r["mode"], r["exit"], r["error"][:300])
for renderer in ("LibreOffice", "DeckCraft"):
    for mode in ("pages", "whole"):
        times = [r["seconds"] for r in runs if r["renderer"] == renderer and r["mode"] == mode and r["exit"] == 0]
        print(renderer, mode, "total", round(sum(times)), "s, max", max(times))
print("severity:", collections.Counter(s["severity"] for s in slides), "of", len(slides))
print("size mismatches:", [(s["deck"], s["slide"], s["libreoffice_size"], s["deckcraft_size"]) for s in slides if not s["same_size"]][:20])
print("blank in one only:", [(s["deck"].split("/")[-1], s["slide"], s["libreoffice_ink"], s["deckcraft_ink"]) for s in slides if (s["libreoffice_ink"] < .001) != (s["deckcraft_ink"] < .001)])
print("whole vs pages mismatches:", [(c["deck"].split("/")[-1], c["renderer"], c["mismatched_slides"], c["height_ok"]) for c in consistency if c["mismatched_slides"] or not c["height_ok"]])
tags = {(f["deck"], f["slide"]): f for f in features}
print("missing feature rows:", sum((s["deck"], s["slide"]) not in tags for s in slides), "slides without comparison:", len(tags) - len(slides))
totals, majors = collections.Counter(), collections.Counter()
for s in slides:
    f = tags.get((s["deck"], s["slide"]), {"features": [], "hidden": False})
    for name in f["features"] + (["hidden"] if f["hidden"] else []) + ["(all)"]:
        totals[name] += 1
        majors[name] += s["severity"] == "major"
print("feature: major/total")
for name, total in totals.most_common():
    print(f"  {name:20} {majors[name]:4}/{total:<4} {majors[name] / total:.0%}")
by_deck = collections.defaultdict(collections.Counter)
for s in slides:
    by_deck[s["deck"]][s["severity"]] += 1
print("deck: major moderate minor identical")
for deck, c in sorted(by_deck.items(), key=lambda kv: -kv[1]["major"]):
    print(f"  {c['major']:4} {c['moderate']:4} {c['minor']:4} {c['identical']:4}  {deck}")
print("worst:")
for s in sorted(slides, key=lambda s: -s["largest_region_pct"])[:40]:
    print(f"  {s['largest_region_pct']:6} {s['coarse_pct']:6} {s['deck'].split('/')[-1]} #{s['slide']} {s.get('image')}")
