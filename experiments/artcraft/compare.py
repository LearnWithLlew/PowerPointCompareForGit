#!/usr/bin/env python3
"""Compares the LibreOffice and DeckCraft renders of every slide that run_corpus.py produced.

Writes to <root>/results: slides.jsonl (one row per slide), consistency.jsonl (does each whole-deck PNG
match its per-slide PNGs) and images/ (LibreOffice | DeckCraft | difference, for each slide that differs).

usage: compare.py ROOT
"""
import json, re, sys
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

Image.MAX_IMAGE_PIXELS = None
PIXEL_THRESHOLD = 32        # a channel must move this much (of 255) for a pixel to count as different
BLOCK = 16                  # coarse comparison averages blocks this size, which hides anti-aliasing and small text shifts
BLOCK_THRESHOLD = 16
BAND = 40
root = Path(sys.argv[1]).resolve()
results = root / "results"


def load(path):
    image = Image.open(path).convert("RGBA")
    background = Image.new("RGBA", image.size, (255, 255, 255, 255))
    return Image.alpha_composite(background, image).convert("RGB")


def ink(pixels):
    """Fraction of pixels that aren't the slide's most common color; near 0 means the slide is blank."""
    flat = pixels.reshape(-1, 3).astype(np.uint32)
    codes = flat[:, 0] << 16 | flat[:, 1] << 8 | flat[:, 2]
    values, counts = np.unique(codes, return_counts=True)
    return 1 - counts.max() / codes.size


def coarse(pixels):
    height, width = pixels.shape[0] // BLOCK * BLOCK, pixels.shape[1] // BLOCK * BLOCK
    return pixels[:height, :width].reshape(height // BLOCK, BLOCK, width // BLOCK, BLOCK, 3).astype(np.float32).mean(axis=(1, 3))


def severity(row):
    if row["pixel_pct"] == 0:
        return "identical"
    # Lines of text in another font change thin strips; a missing picture or shape changes one solid region.
    if (row["libreoffice_ink"] < 0.001) != (row["deckcraft_ink"] < 0.001) or row["largest_region_pct"] >= 3 or not row["same_size"]:
        return "major"
    return "moderate" if row["largest_region_pct"] >= 1 or row["coarse_pct"] >= 5 else "minor"


def compare_slide(job):
    deck, slide, left_path, right_path = job
    left, right = load(left_path), load(right_path)
    row = {"deck": deck, "slide": slide, "libreoffice_size": list(left.size), "deckcraft_size": list(right.size)}
    same_size = left.size == right.size
    scaled = right if same_size else right.resize(left.size, Image.LANCZOS)
    a, b = np.asarray(left), np.asarray(scaled)
    moved = (np.abs(a.astype(np.int16) - b.astype(np.int16)).max(axis=2) > PIXEL_THRESHOLD)
    exact = np.array_equal(a, b)
    blocks = np.abs(coarse(a) - coarse(b)).max(axis=2) > BLOCK_THRESHOLD
    regions, count = ndimage.label(ndimage.binary_closing(blocks, iterations=1))
    largest = int(np.bincount(regions.ravel())[1:].max()) if count else 0
    row["largest_region_pct"] = round(largest / blocks.size * 100, 3)
    row.update(same_size=same_size, exact=exact,
               pixel_pct=0.0 if exact else round(max(float(moved.mean()) * 100, 0.0001), 4),
               coarse_pct=round(float(blocks.mean()) * 100, 3),
               libreoffice_ink=round(float(ink(a)), 5), deckcraft_ink=round(float(ink(np.asarray(right))), 5))
    row["severity"] = severity(row)
    if row["severity"] != "identical":
        heat = np.full(a.shape, 255, np.uint8)
        heat[moved] = (220, 30, 30)
        panel_height = 480
        panels = [Image.fromarray(p).resize((max(1, p.shape[1] * panel_height // p.shape[0]), panel_height), Image.LANCZOS) for p in (a, np.asarray(scaled), heat)]
        sheet = Image.new("RGB", (sum(p.width for p in panels) + 20, panel_height), (90, 90, 90))
        x = 0
        for panel in panels:
            sheet.paste(panel, (x, 0))
            x += panel.width + 10
        target = results / "images" / re.sub(r"[^A-Za-z0-9._-]+", "_", deck) / f"slide-{slide:04d}.jpg"
        target.parent.mkdir(parents=True, exist_ok=True)
        sheet.save(target, quality=82)
        row["image"] = str(target.relative_to(results))
    return row


def check_whole(job):
    """Stitching adds a band per slide; every slide cut from the tall PNG should match that slide's own PNG."""
    deck, renderer, whole, pages = job
    tall = np.asarray(load(whole))
    top, mismatches = 0, []
    for slide, path in pages:
        page = np.asarray(load(path))
        top += BAND
        cut = tall[top:top + page.shape[0], :page.shape[1]]
        if cut.shape != page.shape or not np.array_equal(cut, page):
            mismatches.append(slide)
        top += page.shape[0]
    return {"deck": deck, "renderer": renderer, "slides": len(pages), "height_ok": top == tall.shape[0], "mismatched_slides": mismatches}


def main():
    runs = [json.loads(line) for line in (results / "runs.jsonl").read_text().splitlines()]
    by_key = {(r["deck"], r["renderer"], r["mode"]): r for r in runs}
    slide_jobs, whole_jobs = [], []
    number = re.compile(r"\.page\.(\d+)\.png$")
    for deck in sorted({r["deck"] for r in runs}):
        pages = {}
        for renderer in ("LibreOffice", "DeckCraft"):
            run = by_key.get((deck, renderer, "pages"))
            if run and run["exit"] == 0:
                pages[renderer] = {int(number.search(p).group(1)): str(Path(root, "corpus", p)) if not p.startswith("/") else p for p in run["outputs"]}
            whole = by_key.get((deck, renderer, "whole"))
            if whole and whole["exit"] == 0 and renderer in pages:
                listed = sorted(pages[renderer].items())
                whole_jobs.append((deck, renderer, whole["outputs"][0], listed))
        if len(pages) == 2:
            slide_jobs += [(deck, n, pages["LibreOffice"][n], pages["DeckCraft"][n]) for n in sorted(pages["LibreOffice"]) if n in pages["DeckCraft"]]
    with ProcessPoolExecutor(6) as pool:
        with (results / "slides.jsonl").open("w") as file:
            for row in pool.map(compare_slide, slide_jobs, chunksize=4):
                file.write(json.dumps({k: (bool(v) if isinstance(v, np.bool_) else v) for k, v in row.items()}) + "\n")
        with (results / "consistency.jsonl").open("w") as file:
            for row in pool.map(check_whole, whole_jobs):
                file.write(json.dumps(row) + "\n")
    print(f"{len(slide_jobs)} slides, {len(whole_jobs)} whole decks")


if __name__ == "__main__":
    main()
