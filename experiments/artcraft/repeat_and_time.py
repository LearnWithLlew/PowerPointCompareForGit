#!/usr/bin/env python3
"""Renders a few decks twice, one at a time, in fresh repos: are the results stable, and how long does each take alone?

usage: repeat_and_time.py ROOT TOOL_DLL DECK...
"""
import hashlib, json, shutil, subprocess, sys, tempfile, time
from pathlib import Path
import numpy as np
from PIL import Image

root, tool, decks = Path(sys.argv[1]).resolve(), sys.argv[2], sys.argv[3:]
rows = []


def render(deck, renderer, mode):
    with tempfile.TemporaryDirectory(prefix="pptx-repeat-") as folder:
        repo = Path(folder)
        shutil.copyfile(root / "corpus" / deck, repo / "deck.pptx")
        for command in (["init", "-q"], ["add", "-A"], ["-c", "user.name=e", "-c", "user.email=e@example.com", "commit", "-qm", "d"]):
            subprocess.run(["git", *command], cwd=repo, check=True)
        start = time.time()
        args = ["--pages", "1-9999"] if mode == "pages" else []
        run = subprocess.run(["dotnet", tool, "deck.pptx", "--renderer", renderer, *args], cwd=repo, capture_output=True, text=True, stdin=subprocess.DEVNULL, check=True)
        seconds = time.time() - start
        return seconds, [np.asarray(Image.open(path).convert("RGB")) for path in run.stdout.splitlines()]


for deck in decks:
    for renderer in ("LibreOffice", "DeckCraft"):
        first_seconds, first = render(deck, renderer, "pages")
        second_seconds, second = render(deck, renderer, "pages")
        changed = [(i + 1, int((np.abs(a.astype(int) - b.astype(int)).max(axis=2) > 0).sum())) for i, (a, b) in enumerate(zip(first, second)) if a.shape != b.shape or not np.array_equal(a, b)]
        whole_seconds, _ = render(deck, renderer, "whole")
        row = dict(deck=deck, renderer=renderer, slides=len(first), pages_seconds=round(first_seconds, 1), repeat_seconds=round(second_seconds, 1), whole_seconds=round(whole_seconds, 1), slides_changed_on_repeat=changed)
        rows.append(row)
        print(json.dumps(row), flush=True)
(root / "results" / "repeat.jsonl").write_text("".join(json.dumps(r) + "\n" for r in rows))
