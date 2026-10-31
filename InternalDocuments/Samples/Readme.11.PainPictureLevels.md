# <img src="https://raw.githubusercontent.com/LearnWithLlew/PowerPointCompareForGit/main/icon.png" alt="" width="48" align="center"> PowerPointCompareForGit

[![build-and-test](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)
[![NuGet downloads](https://img.shields.io/nuget/dt/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)

Here's what git tells you about your PowerPoint deck:

```
$ git diff talk.pptx
Binary files a/talk.pptx and b/talk.pptx differ
```

Here's what this tool shows you:

![VisualDiffer after --compare --selective: slide 4 edited with the difference highlighted, and slide 5 added opposite a blank](compare-selective-screenshot.png)

*A 60-slide deck with one slide edited and one added. The diff shows just those two.*

> **Status:** early development. All commands work; the detected diff-tool commands are not yet verified on every OS.

## Install

```bash
dotnet tool install --global PowerPointCompareForGit
```

The first compare sets up two things:
- **Slide drawing:** slides are drawn by [DeckCraft](https://github.com/storytold/deckcraft), and the tool offers to install it if it's missing.
- **Diff tool:** you choose a PNG-capable diff tool. It suggests any it finds (Beyond Compare, Kaleidoscope, P4Merge, Araxis Merge, WinMerge), or you can enter a custom command such as `mytool --flags {file1} {file2}`.

Your answer is saved to `~/.createpowerpointcheckpoint/settings.json`.

## Level 1: See what you just changed

This is the only command most people need.

```bash
CreatePowerpointCheckpoint talk.pptx --compare --selective
```

Your diff tool opens with only the slides that changed, older version on the left. It compares your uncommitted edits to the last commit that changed the deck. With no uncommitted edits, it compares the last two commits that changed it.

- **Gray bands** show each slide's real number in its own deck.
- **A blank** means the slide doesn't exist on that side, so it was added or removed.
- **Moved slides aren't shown,** because they didn't change. They're listed in the terminal as `Slides moved but unchanged: 19→23, 20→24.`
- **If no slide looks different,** nothing opens and the terminal says so.
- **Speaker notes, animations and transitions** aren't in the images, so changes to them won't show.

## Level 2: Pick the versions

Name a sha, tag or branch to compare any two points in the deck's history.

```bash
CreatePowerpointCheckpoint talk.pptx --compare oslo --selective         # what's changed since the Oslo talk?
CreatePowerpointCheckpoint talk.pptx --compare oslo berlin --selective  # what changed between the two talks?
```

## Level 3: Pick the slides

Look at slides that didn't change too, either the whole deck or part of it.

```bash
CreatePowerpointCheckpoint talk.pptx --compare                # every slide
CreatePowerpointCheckpoint talk.pptx --compare --pages 2-25   # only slides 2 to 25, stitched into one PNG per version
```

`--pages` works with any of the versions from level 2, but it can't be combined with `--selective`.

## Level 4: Get the images without a compare

Leave out `--compare` to write PNGs and print their paths, with nothing opened.

```bash
CreatePowerpointCheckpoint talk.pptx                          # the last commit that changed the deck
CreatePowerpointCheckpoint talk.pptx --commit sha1,sha2,sha3  # each of these commits
CreatePowerpointCheckpoint talk.pptx --pages 40               # only slide 40; past the end means the last slide
```

Previews are cached in `.git/pptx-previews/`, so git never tracks them and each commit is rendered only once.

## Settings

```bash
CreatePowerpointCheckpoint talk.pptx --renderer LibreOffice   # draw with LibreOffice instead of DeckCraft
CreatePowerpointCheckpoint --reset                            # clear local settings
```

## For AI agents

[skills/powerpoint-compare/SKILL.md](../../skills/powerpoint-compare/SKILL.md) is an [Agent Skill](https://agentskills.io) that teaches coding agents how to use this tool. Copy the `powerpoint-compare` folder into your agent's skills directory (e.g. `.claude/skills/`).

## License

Apache-2.0. See [LICENSE](../../LICENSE).
