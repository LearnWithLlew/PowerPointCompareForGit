#!/usr/bin/env python3
"""Tags each slide with the XML that explains a difference seen in the review. Writes results/cause_tags.json.

usage: cause_tags.py ROOT
"""
import json, posixpath, re, sys, zipfile
from pathlib import Path

root = Path(sys.argv[1]).resolve()
corpus, results = root / "corpus", root / "results"
PATTERNS = {
    "svg_only_picture": r"<a:blip>\s*<a:extLst>(?:(?!</a:blip>).)*svgBlip",
    "donut_shape": r'prst="donut"',
    "multi_column_text": r'numCol="[2-9]',
    "slide_number_field": r'type="slidenum"',
    "background_removal": r"backgroundRemoval",
    "text_shadow": r"<a:rPr[^>]*>(?:(?!</a:rPr>).)*<a:effectLst>\s*<a:outerShdw",
    "perspective_3d": r'<a:camera prst="(?!orthographicFront)',
    "video_or_audio": r"<a:videoFile|<a:audioFile",
}


def slide_xml(package, number):
    presentation = package.read("ppt/presentation.xml")
    rels = package.read("ppt/_rels/presentation.xml.rels").decode()
    rid = re.findall(rb'<p:sldId [^>]*r:id="([^"]+)"', presentation)[number - 1].decode()
    relationship = re.search(r'<Relationship [^>]*Id="%s"[^>]*>' % rid, rels).group(0)
    target = re.search(r'Target="([^"]+)"', relationship).group(1).lstrip("/").replace("ppt/", "")
    return package.read(posixpath.normpath(f"ppt/{target}")).decode("utf8", "ignore")


tags = {name: [] for name in PATTERNS}
for row in map(json.loads, (results / "slides.jsonl").read_text().splitlines()):
    with zipfile.ZipFile(corpus / row["deck"]) as package:
        xml = slide_xml(package, row["slide"])
    for name, pattern in PATTERNS.items():
        if re.search(pattern, xml, re.S):
            tags[name].append([row["deck"], row["slide"]])
(results / "cause_tags.json").write_text(json.dumps(tags, indent=1))
for name, slides in tags.items():
    print(f"{len(slides):4} slides in {len({deck for deck, _ in slides}):2} decks  {name}")
