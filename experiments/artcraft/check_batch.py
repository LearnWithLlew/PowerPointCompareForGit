#!/usr/bin/env python3
"""Does `deckcraft-cli render --all` draw the same pixels as rendering one slide at a time?

Compares a fresh --all render of each deck with the per-slide PNGs run_corpus.py kept. Writes results/batch.jsonl.

usage: check_batch.py ROOT CLI
"""
import json, re, shutil, subprocess, sys, tempfile, time
from pathlib import Path
import numpy as np
from PIL import Image

root, cli = Path(sys.argv[1]).resolve(), sys.argv[2]
results = root / "results"
runs = [json.loads(line) for line in (results / "runs.jsonl").read_text().splitlines()]
number = re.compile(r"\.page\.(\d+)\.png$")
total = changed = 0
with (results / "batch.jsonl").open("w") as out:
    for run in (r for r in runs if r["renderer"] == "DeckCraft" and r["mode"] == "pages"):
        with tempfile.TemporaryDirectory(prefix="pptx-batch-") as folder:
            start = time.time()
            subprocess.run([cli, "render", str(root / "corpus" / run["deck"]), "--all", "--scale", str(4 / 3), folder + "/o"], check=True, capture_output=True)
            seconds = time.time() - start
            differing = []
            for path in run["outputs"]:
                slide = int(number.search(path).group(1))
                batch = Path(folder, "o", f"slide-{slide:02d}.png")
                total += 1
                if not batch.exists():
                    differing.append((slide, "missing"))
                    continue
                a, b = np.asarray(Image.open(path).convert("RGBA")), np.asarray(Image.open(batch).convert("RGBA"))
                if a.shape != b.shape or not np.array_equal(a, b):
                    differing.append((slide, int((a != b).any(axis=2).sum()) if a.shape == b.shape else "size"))
            changed += len(differing)
            produced = len(list(Path(folder, "o").iterdir()))
        out.write(json.dumps(dict(deck=run["deck"], slides=len(run["outputs"]), produced=produced, seconds=round(seconds, 1), differing=differing)) + "\n")
        print(run["deck"].split("/")[-1], len(run["outputs"]), produced, round(seconds, 1), differing[:6], flush=True)
print(f"{total} slides compared, {changed} differ")
