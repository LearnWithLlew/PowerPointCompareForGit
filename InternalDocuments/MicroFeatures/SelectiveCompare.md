# Selective Compare

Part of [PowerPoint for Git Repos](../Project.md).

Compare two versions of a deck and show only the slides that changed, so a small edit to a 100-slide deck produces a short diff.

## Usage
`--selective` works with every form of `--compare`. The full-deck compare stays the default.

```bash
CreatePowerpointCheckpoint file.pptx --compare --selective            # working copy vs last commit that changed the file (or last two such commits if no uncommitted edits)
CreatePowerpointCheckpoint file.pptx --compare sha1 --selective       # working copy vs sha1
CreatePowerpointCheckpoint file.pptx --compare sha1 sha2 --selective  # sha1 vs sha2
```

- Why not make selective the default with a `--full` flag: it changes what `--compare` does for existing users, and it's less proven until it's been tested on real decks.
- Why not a separate `--selective-compare` option: it repeats all the `--compare` forms and the logic for parsing them.

`--selective` can't be combined with `--pages`, and the tool errors with `--selective can't be combined with --pages.` Selective already narrows the deck to what changed, and support can be added later without breaking anyone.
- Why not apply `--pages` to each deck first: a slide that moved from 19 to 30 is only found in one deck under `--pages 1-25`, so it shows as removed even though it's unchanged.
- Why not compare whole decks and then filter to the page range: the same number can mean different slides in each deck, so we'd have to define which deck the range applies to, and a pair could be cut in half.

## Steps
1. Run both decks through Determinize to normalize them. This removes differences in GUIDs and compression style.
1. Match slides that are identical between the two decks, regardless of position. A slide that moved from 19 to 23 still counts as identical.
1. Remove the identical slides from both decks.
1. Render each remaining set to one image and compare the two images.

## Normalizing
Determinize's normalization comes from the `DeterministicIoPackaging` NuGet package (MIT, targets `net10.0`), which we reference directly. It installs with our tool, so it adds no external dependency beyond the renderers.
- Why not call the `determinize` command: every user would have to install another tool, and we'd need find-and-offer-to-install logic like the renderers'.
- Why not write our own: it means maintaining work that already exists, and it would be easy to miss sources of difference.

To verify:
1. The library's API can normalize a deck from our code.
1. Normalizing is enough to match unchanged slides in real decks saved by PowerPoint, which may renumber IDs inside a slide when saving.

## Identical slides
Two slides are identical when their normalized slide XML and everything it draws from (layout, master, theme and media, with media compared by content) all match.
- Speaker notes are ignored because they don't appear in the image. A notes-only change would otherwise show two identical-looking slides.
- A moved slide with a slide number field has identical XML but displays a different number. It counts as identical, and the move is reported on stderr.
- Why not compare only the slide's own XML: editing a master or replacing a linked image could change how a slide looks while it's still treated as identical.
- Why not compare rendered images: this renders every slide, which loses the speed benefit on long decks.

## Lining up changed slides
The longest run of identical slides that appear in the same order in both decks are the fixed points, as in a text diff. Slides that aren't identical are grouped by the fixed points around them, then paired in order within each group. Where one side has nothing to pair, it gets a slide-sized blank, so paired slides sit level in the diff tool.
- Moved slides are still hidden, but they aren't fixed points. If A, B, C became C, A, B, then A and B are the fixed points and C counts as moved.
- Each slide can match only one slide in the other deck, so if a duplicate is added, the extra copy shows as added.
- If a commit changes the slide size, each row is as tall as the taller slide in it, and a blank is the same size as the slide opposite it. When both decks use one size, nothing extra is added.
- Each slide keeps its real number from its own deck in the gray band.
- A slide that was both moved and edited appears as one removal and one addition.
- Why not use only slides that are unique in each deck as fixed points (patience diff): it needs a fallback for decks with few unique slides, and an added duplicate would need its own rule.
- Why not treat moved slides as changed: a slide that moved from 19 to 23 has to count as identical.
- Why not stack each side with no gaps: after the first added slide everything is offset, so the diff tool marks every later slide as changed.
- Why not pair by similarity (titles or text): it's a guess that can pair the wrong slides, and it adds a lot of logic.
- Why not make each blank the slide size of its own deck, with no row padding: once the sizes differ, every later slide drifts out of line.
- Why not scale slides to one common size: scaling blurs text and hides small changes, which `Project.md` already rejects.

## Reporting moved slides
The images never show a move, so the tool prints one line to stderr listing the moves, such as `Slides moved but unchanged: 19→23, 20→24.` A move is a real change to the deck, so it shouldn't go unmentioned, and stdout stays a clean list of PNG paths. A long list is cut off with a count, such as `...and 12 more`.
- Why not print nothing: a reordered deck looks the same as one where nothing moved, so the reorder is easy to miss.
- Why not show moved slides in the images, marked as moved: it adds back slides that selective compare is meant to remove, and it needs a new way to draw them.

## When every slide matches
This covers decks that were only reordered, or where only speaker notes or file details changed. The tool opens nothing and exits with 0, because this is a valid result, not a failure. Stdout stays a clean, empty list of PNG paths, and a message goes to stderr:
- `No visible slide changes between sha1 and sha2.`
- `Slides were reordered, but none changed.` if the order differs, followed by the list of moves.

- Why not open two blank images: it makes you stop and work out whether something broke, and there's no reliable size to give images with no slides in them.
- Why not exit non-zero like `git diff --exit-code`: it treats a normal result as a failure, and CI steps would fail on it by default.

## Storage
A selective image depends on both decks, because the slides it shows depend on what it's being compared against. So each compare caches its own pair of images, next to the other previews, with both versions in each file name:

```
.git/pptx-previews/<path-to-deck>/my.pptx.<sha1>.selective-vs.<sha2>.png
.git/pptx-previews/<path-to-deck>/my.pptx.<sha2>.selective-vs.<sha1>.png
```

- Each name starts with its own version, so the diff tool shows which side is which.
- If both files exist, the tool reuses them and renders nothing.
- If either side is the working copy, they're regenerated on every run.
- The name also includes the renderer when it isn't LibreOffice, using the existing `PreviewPaths` pattern.
- Why not regenerate on every run: it redoes the renders each time, unlike every other commit compare in this tool.
- Why not cache each slide by a hash of its content: it needs a new cache layout and more logic for what's usually only a few slides.

## Known limitations
- An edited picture that DeckCraft doesn't draw counts as changed, but the slide looks the same in both images.
- DeckCraft launches once per listed slide, so a compare with many changed slides could take longer than rendering the whole deck with `--all`.

## Examples
- Two slides added: one image is blank but just as tall, and the other shows the added slides 21 and 24.
- One slide edited: one image shows the old slide 23, and the other shows the new slide 23.
- Slide 21 added and slide 23 edited: new 21 sits opposite a blank, and old 23 sits opposite new 24.
- Any combination of the above can appear in the same compare.
