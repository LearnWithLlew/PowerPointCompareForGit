#!/usr/bin/env python3
"""Tags each slide with the features it contains, in deck order. Writes results/features.jsonl.

usage: slide_features.py ROOT
"""
import json, posixpath, re, sys, zipfile
from pathlib import Path

root = Path(sys.argv[1]).resolve()
corpus = root / "corpus"
PATTERNS = {
    "picture": rb"<p:pic[ >]", "svg": rb"svgBlip", "table": rb"<a:tbl>", "chart": rb"drawingml/2006/chart\"", "smartart": rb"drawingml/2006/diagram\"",
    "group": rb"<p:grpSp>", "custom_geometry": rb"<a:custGeom>", "gradient": rb"<a:gradFill", "shadow": rb"<a:outerShdw", "glow": rb"<a:glow", "reflection": rb"<a:reflection",
    "soft_edge": rb"<a:softEdge", "three_d": rb"<a:(scene3d|sp3d)", "wordart": rb"<a:prstTxWarp prst=\"(?!textNoShape)", "ole": rb"<p:oleObj", "ink": rb"contentPart", "math": rb"<m:oMath",
    "alternate_content": rb"<mc:AlternateContent", "media": rb"<a:(videoFile|audioFile)|<p14:media", "animation": rb"<p:timing>", "transition": rb"<p:transition", "image_crop": rb"<a:srcRect [^/]*[ltrb]=", "picture_fill": rb"<p:spPr>(?:(?!</p:spPr>).)*<a:blipFill", "duotone": rb"<a:duotone", "autofit_shrink": rb"<a:normAutofit",
}
with (root / "results" / "features.jsonl").open("w") as out:
    for deck in sorted(p for p in corpus.rglob("*.pptx") if ".git" not in p.relative_to(corpus).parts):
        with zipfile.ZipFile(deck) as package:
            rels = {m[1].decode(): m[0].decode() for m in re.findall(rb'<Relationship [^>]*?Target="([^"]+)"[^>]*?Id="([^"]+)"', package.read("ppt/_rels/presentation.xml.rels"))}
            rels.update({m[0].decode(): m[1].decode() for m in re.findall(rb'<Relationship [^>]*?Id="([^"]+)"[^>]*?Target="([^"]+)"', package.read("ppt/_rels/presentation.xml.rels"))})
            ids = re.findall(rb'<p:sldId [^>]*r:id="([^"]+)"', package.read("ppt/presentation.xml"))
            for number, rid in enumerate(ids, 1):
                target = rels[rid.decode()].lstrip("/")
                xml = package.read(posixpath.normpath(target if target.startswith("ppt/") else f"ppt/{target}"))
                row = {"deck": str(deck.relative_to(corpus)), "slide": number, "hidden": b'show="0"' in xml[:2000],
                       "features": [name for name, pattern in PATTERNS.items() if re.search(pattern, xml, re.S)]}
                out.write(json.dumps(row) + "\n")
