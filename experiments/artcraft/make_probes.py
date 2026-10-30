#!/usr/bin/env python3
"""Builds small decks that each isolate one thing the renderers might handle differently.

usage: make_probes.py OUT_FOLDER
"""
import sys
from pathlib import Path
from lxml import etree
from pptx import Presentation
from pptx.chart.data import CategoryChartData
from pptx.dml.color import RGBColor
from pptx.enum.chart import XL_CHART_TYPE
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, MSO_AUTO_SIZE
from pptx.util import Inches, Pt

out = Path(sys.argv[1])
out.mkdir(parents=True, exist_ok=True)

# Most used in the decks: Calibri, Arial, Fira Code, Aptos, Menlo, Consolas, News Gothic MT, Verdana... with a nonsense name to show the fallback.
FONTS = ["Arial", "Arial Black", "Calibri", "Calibri Light", "Aptos", "Aptos Display", "Times New Roman", "Georgia", "Verdana", "Tahoma",
         "Impact", "Courier New", "Consolas", "Menlo", "Fira Code", "Cousine", "Montserrat", "Inter", "Roboto", "Open Sans",
         "News Gothic MT", "Futura", "Ink Free", "Bradley Hand", "Stencil", "Zapf Dingbats", "Google Sans", "OpenDyslexic", "NoSuchFontAtAll"]


def blank(deck):
    return deck.slides.add_slide(deck.slide_layouts[6])


def text(slide, x, y, w, h, value, font=None, size=18, bold=False, italic=False):
    box = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    box.text_frame.word_wrap = True
    run = box.text_frame.paragraphs[0].add_run()
    run.text = value
    run.font.size, run.font.bold, run.font.italic = Pt(size), bold, italic
    if font:
        run.font.name = font
    return box


def fonts():
    deck = Presentation()
    deck.slide_width, deck.slide_height = Inches(13.333), Inches(7.5)
    for start in range(0, len(FONTS), 10):
        slide = blank(deck)
        for row, font in enumerate(FONTS[start:start + 10]):
            text(slide, 0.3, 0.2 + row * 0.7, 12.7, 0.6, f"{font}: Quick brown fox 0123 iIl1 WWW", font, 24)
    # Bold and italic on a font of each kind.
    slide = blank(deck)
    for row, (font, bold, italic) in enumerate([("Arial", True, False), ("Arial", False, True), ("Calibri", True, False), ("Calibri", False, True), ("Aptos", True, False), ("Aptos", False, True), ("Menlo", True, False), ("Menlo", False, True)]):
        text(slide, 0.3, 0.2 + row * 0.8, 12.7, 0.6, f"{font} bold={bold} italic={italic}: Quick brown fox", font, 24, bold, italic)
    deck.save(out / "fonts.pptx")


def shapes():
    deck = Presentation()
    # 1: preset shapes
    slide = blank(deck)
    for i, kind in enumerate([MSO_SHAPE.ROUNDED_RECTANGLE, MSO_SHAPE.OVAL, MSO_SHAPE.CLOUD, MSO_SHAPE.HEART, MSO_SHAPE.STAR_5_POINT, MSO_SHAPE.LIGHTNING_BOLT, MSO_SHAPE.RIGHT_ARROW, MSO_SHAPE.CAN, MSO_SHAPE.CUBE, MSO_SHAPE.DONUT, MSO_SHAPE.GEAR_6, MSO_SHAPE.WAVE]):
        shape = slide.shapes.add_shape(kind, Inches(0.3 + i % 4 * 2.4), Inches(0.4 + i // 4 * 2.3), Inches(2.1), Inches(1.9))
        shape.text_frame.text = str(i)
    # 2: rotation, gradient, transparency, shadow, glow, reflection via raw XML
    slide = blank(deck)
    rotated = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.5), Inches(0.5), Inches(2), Inches(1))
    rotated.rotation = 30
    rotated.text_frame.text = "rotated"
    gradient = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(3.5), Inches(0.5), Inches(2), Inches(1))
    gradient.fill.gradient()
    gradient.fill.gradient_angle = 45
    a = "http://schemas.openxmlformats.org/drawingml/2006/main"
    def effect(shape, xml):
        pr = shape._element.spPr
        for old in pr.findall(f"{{{a}}}effectLst"):
            pr.remove(old)
        pr.append(etree.fromstring(f'<a:effectLst xmlns:a="{a}">{xml}</a:effectLst>'))
    for i, (name, xml) in enumerate([
        ("outer shadow", '<a:outerShdw blurRad="150000" dist="100000" dir="2700000"><a:srgbClr val="000000"><a:alpha val="60000"/></a:srgbClr></a:outerShdw>'),
        ("glow", '<a:glow rad="200000"><a:srgbClr val="FF8800"><a:alpha val="70000"/></a:srgbClr></a:glow>'),
        ("soft edge", '<a:softEdge rad="150000"/>'),
        ("reflection", '<a:reflection blurRad="6350" stA="50000" endA="300" endPos="55000" dist="50800" dir="5400000" sy="-100000" algn="bl" rotWithShape="0"/>')]):
        shape = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.5 + i * 2.3), Inches(2.7), Inches(1.9), Inches(1.1))
        shape.text_frame.text = name
        effect(shape, xml)
    transparent = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(6.5), Inches(0.5), Inches(2), Inches(1.5))
    transparent.fill.solid()
    transparent.fill.fore_color.rgb = RGBColor(255, 0, 0)
    transparent.fill._xPr.find(f"{{{a}}}solidFill")[0].append(etree.fromstring(f'<a:alpha xmlns:a="{a}" val="40000"/>'))
    text(slide, 6.3, 0.9, 3, 0.5, "text behind transparent oval", size=14)
    # 3: table
    slide = blank(deck)
    table = slide.shapes.add_table(4, 4, Inches(0.5), Inches(0.5), Inches(9), Inches(3)).table
    for r in range(4):
        for c in range(4):
            table.cell(r, c).text = f"r{r} c{c}"
    table.cell(1, 1).merge(table.cell(2, 2))
    # 4-6: charts
    data = CategoryChartData()
    data.categories = ["A", "B", "C", "D"]
    data.add_series("One", (4, 7, 2, 9))
    data.add_series("Two", (3, 5, 8, 1))
    for kind in (XL_CHART_TYPE.COLUMN_CLUSTERED, XL_CHART_TYPE.LINE_MARKERS, XL_CHART_TYPE.PIE):
        slide = blank(deck)
        slide.shapes.add_chart(kind, Inches(0.5), Inches(0.5), Inches(9), Inches(6), data).chart.has_legend = True
    # 7: text fitting
    slide = blank(deck)
    long = "Text that is much too long for its small box, to see how overflow is drawn. " * 4
    overflow = text(slide, 0.3, 0.3, 3, 1, long, size=18)
    overflow.line.color.rgb = RGBColor(0, 0, 255)
    shrink = text(slide, 3.6, 0.3, 3, 1.5, long, size=18)
    shrink.line.color.rgb = RGBColor(0, 0, 255)
    shrink.text_frame.auto_size = MSO_AUTO_SIZE.TEXT_TO_FIT_SHAPE
    grow = text(slide, 6.9, 0.3, 3, 1, long, size=18)
    grow.line.color.rgb = RGBColor(0, 0, 255)
    grow.text_frame.auto_size = MSO_AUTO_SIZE.SHAPE_TO_FIT_TEXT
    middle = text(slide, 0.3, 4.5, 4, 2, "anchored middle", size=18)
    middle.line.color.rgb = RGBColor(0, 0, 255)
    middle.text_frame.vertical_anchor = MSO_ANCHOR.MIDDLE
    # 8: a group, flipped and rotated
    slide = blank(deck)
    group = slide.shapes.add_group_shape()
    inner = group.shapes.add_shape(MSO_SHAPE.RIGHT_ARROW, Inches(1), Inches(1), Inches(3), Inches(1))
    inner.text_frame.text = "in group"
    group.shapes.add_shape(MSO_SHAPE.OVAL, Inches(4), Inches(2), Inches(1), Inches(1))
    group._element.grpSpPr.find(f"{{{a}}}xfrm").set("rot", str(20 * 60000))
    # 9: a hidden slide with a marker, then a slide after it
    slide = blank(deck)
    text(slide, 1, 1, 8, 1, "THIS SLIDE IS HIDDEN", size=40)
    slide._element.set("show", "0")
    slide = blank(deck)
    text(slide, 1, 1, 8, 1, "Slide after the hidden one", size=40)
    # 11: speaker notes must not appear on the slide
    slide = blank(deck)
    text(slide, 1, 1, 8, 1, "Has speaker notes", size=40)
    slide.notes_slide.notes_text_frame.text = "NOTES TEXT"
    deck.save(out / "shapes.pptx")


def sizes():
    # Slide sizes: LibreOffice's export size and DeckCraft's 4/3 scale may not agree on every one.
    for name, w, h in [("size_16x9", 13.333, 7.5), ("size_4x3", 10, 7.5), ("size_a4", 11.69, 8.27), ("size_custom", 7.3, 4.1), ("size_portrait", 7.5, 13.333), ("size_big", 30, 20)]:
        deck = Presentation()
        deck.slide_width, deck.slide_height = Inches(w), Inches(h)
        text(blank(deck), 0.5, 0.5, 5, 1, name, size=24)
        deck.save(out / f"{name}.pptx")


fonts()
shapes()
sizes()
