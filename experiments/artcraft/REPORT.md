# LibreOffice vs DeckCraft rendering experiment

Run 2026-10-30 with DeckCraft / `deckcraft-cli` 0.12.0 and the LibreOffice installed on this Mac.

## Outcome

DeckCraft became the default on this branch by the maintainer's choice, with `--renderer LibreOffice` kept. The recommendation below is what this report said at the time.

## Recommendation

Keep LibreOffice as the default, and keep `--renderer DeckCraft` as an option. Don't switch yet.

DeckCraft is faster, and on several features it draws the slide better. But it sometimes leaves things out or adds a fill that isn't there: pictures that exist only as SVG disappear, and donut shapes get a gray center. This tool exists to show what changed, so a missing picture is the worse failure, because an edit to that picture can't be seen at all. LibreOffice's worst failures are garbled, but the content is still on the slide.

Revisit this when DeckCraft fixes the three bugs under [DeckCraft problems](#deckcraft-problems). It calls itself alpha and is shipping a release every few days.

## Reported to DeckCraft

Posted 2026-10-31 from @isidore, retested on `deckcraft-cli` 0.13.1 using test decks with none of the talk content:

- [#1595](https://github.com/storytold/deckcraft/issues/1595): overview of the whole comparison, with the offer of a call.
- [#1596](https://github.com/storytold/deckcraft/issues/1596): shadows and glows fill the holes of shapes.
- [#1597](https://github.com/storytold/deckcraft/issues/1597): glow on a transparent picture fills its rectangle.
- [#1598](https://github.com/storytold/deckcraft/issues/1598): monospace fallback.

The font flip and the parallel slowdown under Repeatability and Speed did not reproduce on 0.13.1, including 64 renders of one slide with 8 workers and 4 simultaneous renders of the largest deck.

## What was run

- 49 decks (25 from `lightning_talks`, 24 from `Talks`), 1,541 slides, 221 of them hidden.
- Each deck in both renderers, as a whole deck and as one PNG per slide: 196 runs, 0 failures.
- The decks were copied first, so the source repos were not touched. The copies are the working files, including the uncommitted edit to `On Not being Defensive.pptx`.
- Small test decks for fonts, shapes, effects, tables, charts, text fitting and slide sizes (`make_probes.py`).
- Every slide pair was scored by pixels. I looked at the 2 worst slides from each deck with any major difference (82 slides), all 9 SVG slides, 7 multi-column slides, 7 donut slides, and about 25 background-removal and video slides.

## Limits of this report

- **PowerPoint was not used as a reference.** It is installed here, but I didn't drive your desktop app. "Better" below means it matches what the file's XML asks for, or an obvious defect in the other render. Where neither applies, I say so.
- **Most of the 544 major slides were not looked at individually.** The score flags large solid changes, and big text in another font triggers it too, so major does not mean broken. All of them are in the gallery.
- **The cause counts come from XML tags.** A tag means the slide has that feature, not that it renders wrong.

## Which slides differ

Every deck differs. Only 2 of the 1,541 slides are pixel-identical: `Agentic AI and Exploratory Testing.pptx` slide 3 and `Knowledge_documents_AI_is_a_new_employee_on_day_1.pptx` slide 1.

| Score | Slides | Meaning |
|---|---:|---|
| major | 544 | One slide blank, or a solid region of at least 3% of the slide changed |
| moderate | 391 | A solid region of 1-3% changed, or at least 5% of the 16px blocks did |
| minor | 604 | Small scattered changes, usually anti-aliasing or a slightly different font |
| identical | 2 | Every pixel matches |

[slide_list.md](slide_list.md) lists every deck with its slide numbers by score and by feature.

## The biggest cause: fonts

Most differences are text drawn in a different font, which also moves line breaks and sometimes the whole layout.

| Font in the deck | LibreOffice | DeckCraft |
|---|---|---|
| Installed fonts (Arial, Georgia, Verdana, Times New Roman, Impact...) | Correct font | Correct font, slightly lighter text |
| Calibri | Calibri-like substitute | Calibri-like substitute |
| Calibri Light | Regular weight | Light weight |
| Aptos | Arial-like; **bold becomes very heavy, italic becomes a serif** | Consistent Calibri-like substitute |
| Montserrat, Inter, Roboto, Open Sans | Arial-like, except Open Sans, which mixes fonts within a line | Correct-looking, using fonts DeckCraft bundles |
| Stencil, Ink Free | Arial-like | A matching-style font |
| Futura | Looks bold when the deck didn't ask for bold | Regular weight |
| Consolas | Monospace, wider | Monospace, narrower |
| Cousine | Monospace | **Proportional font, which breaks code alignment** |
| Unknown name | Arial-like | Calibri-like |

The fallback is the main reason text wraps differently. For example, `Cutting Code Quickly.pptx` slide 8 is a typewriter-style font in LibreOffice and a Calibri-like one in DeckCraft, on 3 lines in both but with different breaks.

## DeckCraft problems

1. **Pictures stored only as SVG are missing** (already tracked as [#297](https://github.com/storytold/deckcraft/issues/297), with data added in [#1595](https://github.com/storytold/deckcraft/issues/1595)). All 9 slides that have them lose the picture: `WorkingWithAI_MyFirst4Weeks.pptx` slides 2, 5, 7, 8, 34, 35, 62, 88 and `Analyzers.Net.pptx` slide 33. Several are QR codes. Those pictures have an SVG and no PNG fallback, which PowerPoint itself normally writes, so decks saved by other tools are the ones at risk. 38 other slides have an SVG with a fallback. None of them came out blank, but I didn't check each one.
1. **Shadows and glows fill the holes of shapes** ([#1596](https://github.com/storytold/deckcraft/issues/1596)). A donut gets a gray center from its shadow, or a colored one from its glow. Frames and "no" symbols do the same. It shows clearly in the test deck, `Thread_base_theory_test.pptx` slides 3, 4, 15, 16 and `Quantum Computing for BigData.pptx` slide 31. It darkens what sits behind the hole, and on the Thread slides that changes how the slide reads. `Documentation.pptx` slide 69 and `Remote Mob Programming.pptx` slide 65 have small donuts that were too small to judge.
1. **A glow on a picture with transparency fills its whole rectangle** ([#1597](https://github.com/storytold/deckcraft/issues/1597)). This is the pale box around the cut-out characters in `Leveling_Up.pptx`, seen on the 8 slides I checked. Background removal is not the cause: a test picture with no background removal does the same.
1. **Text that should shrink to fit doesn't, when the file has no stored scale.** In the test deck LibreOffice shrinks it and DeckCraft leaves it overflowing. PowerPoint normally stores a scale, so this may rarely matter in real decks.
1. **Some missing monospace fonts become proportional** ([#1598](https://github.com/storytold/deckcraft/issues/1598)): Cousine, Source Code Pro and JetBrains Mono.

## LibreOffice problems

1. **Slide numbers always say 1.** 392 slides in 15 decks have a slide number field, and in LibreOffice they all show "1". This comes from our own code, not LibreOffice: the renderer splits each slide into a one-slide deck first. It exists on `main` today. One upside for diffing: inserting a slide doesn't change the number on every later slide.
1. **Multi-column text overlaps.** 5 of the 7 multi-column slides have columns printed on top of each other: `WorkingWithAI_MyFirst4Weeks.pptx` slides 25, 28, 29, 30 and `Do_NOT_use_the_greater_than_sign_in_programming_16x9.pptx` slide 19. The other 2 are fine. DeckCraft got all 7 right.
1. **Text shadows are missing.** Examples: `Lessons learned from The Phoenix Project and The Goal.pptx` slide 26 and `10x.pptx` slide 12. 108 slides have the tag. On `10x.pptx` DeckCraft's shadow makes the small text harder to read, but that's what the file asks for.
1. **Reflections and 3D perspective are missing.** The test deck's reflection isn't drawn. `Split vs Shared Pain.pptx` slides 11 and 39 have tilted pictures that LibreOffice draws flat.
1. **Content can land off the slide.** `Provable_Inverse_Refactoring.pptx` slide 29 shows 4 list items in LibreOffice and 5 in DeckCraft, with the text larger in LibreOffice. I didn't confirm the cause.
1. **One text box moved.** In the test deck, a box the file places at 4.5 inches (432px) is drawn at 509px by LibreOffice and at the right place by DeckCraft. I didn't look for this in the real decks.
1. **Very large slides are capped.** A 30×20 inch slide comes out 2048×1365 in LibreOffice and 2880×1920 in DeckCraft. All normal slide sizes match exactly.

## Differences with no clear winner

- **Table header text:** DeckCraft makes it bold and LibreOffice doesn't. Table text sits a few pixels differently, which affects `Arlo_git_notation.pptx` slide 3.
- **Table cell fill:** `Lucky Conference.pptx` slide 58 has clear cells in LibreOffice and solid beige in DeckCraft.
- **Charts:** same data, different plot size, legend position and gridlines. `Agile Development Practices - Explained.pptx` slide 7 has the legend at the bottom in LibreOffice and at the top in DeckCraft.
- **Glow:** LibreOffice draws it much wider in the test deck.
- **Gradient angle:** `How To Setup Remote Pairing & Mobibing.pptx` slides 9 and 13 have visibly different angles, which changes large areas of the background.
- **Texture fills:** Many of `Zero day bugs.pptx` slides 24-47 have a darker dotted background in LibreOffice and a lighter gray in DeckCraft. These are the highest-scoring slides in the whole run, even though the content matches.
- **Video and audio icons:** `Pair Programming.pptx` slide 4 is a gray screen with a speaker icon in LibreOffice, and DeckCraft shows the picture underneath with the icon on top. Other video slides match.
- **Text weight:** LibreOffice text looks slightly heavier everywhere, even in the same font. This is why nearly no slide is pixel-identical.

Both renderers handled preset shapes, rotation, groups, gradients, transparency, soft edges, merged table cells, and speaker notes correctly in the test deck. Neither put notes on the slide. Both draw hidden slides.

## Speed

Each deck rendered one at a time, through this tool, one PNG per slide, in seconds:

| Deck | Slides | LibreOffice | DeckCraft |
|---|---:|---:|---:|
| `vectorized_words.pptx` | 3 | 4.1 | 0.8 |
| `Effective Team Change.pptx` | 28 | 15.4 | 7.8 |
| `Pair Programming.pptx` | 42 | 12.5 | 3.9 |
| `ROI on Learning Hour.pptx` | 116 | 38.5 | 12.4 |
| `Leveling_Up.pptx` | 24 | 10.3 | 5.3 |

DeckCraft is 2-5× faster, and could be faster still. Rendering all 49 decks with `deckcraft-cli render --all` took 38.5 seconds in total. Most of this tool's DeckCraft time goes on decoding and re-encoding PNGs in our own C#.

Asking for only some slides starts one `deckcraft-cli` launch per slide, each reloading the deck. Whole decks use one launch. On a large deck that costs about 0.6 seconds per slide.

Ignore the times in `runs.jsonl`. They came from 4 runs at once and reached 3,594 seconds for one deck. I didn't work out why parallel DeckCraft runs slowed down that much, so don't run it in parallel without checking first.

## Repeatability

A diff tool needs the same deck to produce the same pixels.

- **One at a time:** 5 decks rendered twice in each renderer, in fresh repos. No slide changed.
- **DeckCraft batch vs slide by slide:** all 1,541 slides are pixel-identical.
- **A rare font flip in both renderers:** in the parallel run, 2 slides came out in a different fallback font between two renders of the same deck: `Effective Team Change.pptx` slide 26 in DeckCraft and `vectorized_words.pptx` slide 3 in LibreOffice. That's 2 of 3,082 renders. It didn't reproduce one at a time, but 2 repeats on those decks can't rule it out. If it happens in real use, it shows up as a false change between two versions.

## Using it

```bash
CreatePowerpointCheckpoint deck.pptx --renderer DeckCraft
CreatePowerpointCheckpoint deck.pptx --compare --renderer DeckCraft --pages 2-25
```

`deckcraft-cli` must be on `PATH`, or set in `deckCraftCliPath` in the settings file. Files get `.deckcraft` in their names (`my.pptx.abc1234.deckcraft.page.1.png`), so the two renderers' previews never overwrite each other.

The tool doesn't offer to install `deckcraft-cli` the way it does LibreOffice. Comparing a DeckCraft render against a LibreOffice render of the same commit isn't a built-in command either.

## Local files

These are not in git. Everything is under `~/Github/ArtCraftExperiment/`:

- `results/gallery.html`: every differing slide as LibreOffice | DeckCraft | changed pixels, grouped by deck.
- `results/slides.csv`: every slide with its scores and tags.
- `results/review/`: the sheets I looked at.
- `corpus/.git/pptx-previews/`: all the full-size PNGs.
- `probes/sheets/`: the test-deck comparisons.

`deckcraft-cli` is at `~/.local/share/deckcraft-cli/0.12.0/`.

## Reproducing

```bash
export PATH="$HOME/.local/share/deckcraft-cli/0.12.0/deckcraft-cli-0.12.0-macos-universal:$PATH"
ROOT=~/Github/ArtCraftExperiment
python3 run_corpus.py $ROOT ~/Github/lightning_talks ~/Github/Talks   # needs the tool's build output copied to $ROOT/tool
python3 compare.py $ROOT
python3 slide_features.py $ROOT
python3 cause_tags.py $ROOT
python3 write_lists.py $ROOT slide_list.md
```
