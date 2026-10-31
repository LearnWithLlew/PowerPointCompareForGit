# <img src="https://raw.githubusercontent.com/LearnWithLlew/PowerPointCompareForGit/main/icon.png" alt="" width="48" align="center"> PowerPointCompareForGit

[![build-and-test](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)
[![NuGet downloads](https://img.shields.io/nuget/dt/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)

Diff two versions of a PowerPoint deck in your diff tool, slide by slide.

> **Status:** early development. All commands work; the detected diff-tool commands are not yet verified on every OS.

## Usage

Every compare answers two questions: which versions, and which slides?

```bash
CreatePowerpointCheckpoint talk.pptx --compare <which versions> <which slides>
```

### Which versions?

| You write | It compares |
|---|---|
| `--compare` | Your uncommitted edits vs the last commit that changed the deck, or the last two such commits if there are no edits |
| `--compare oslo` | Your working copy vs a sha, tag or branch |
| `--compare oslo berlin` | One sha, tag or branch vs another |

### Which slides?

| You write | It shows |
|---|---|
| `--selective` | Only the slides that changed |
| `--pages 2-25` | Only slides 2 to 25 |
| *(nothing)* | Every slide |

### Put them together

```bash
CreatePowerpointCheckpoint talk.pptx --compare --selective              # what did I just change?
CreatePowerpointCheckpoint talk.pptx --compare oslo --selective         # what have I changed since the Oslo talk?
CreatePowerpointCheckpoint talk.pptx --compare oslo berlin --pages 2-25 # how did the intro differ between those two talks?
```

Any answer to the first question works with any answer to the second. You just can't use `--selective` and `--pages` together.

### Without a compare

Leave out `--compare` to write PNGs and print their paths, with nothing opened:

```bash
CreatePowerpointCheckpoint talk.pptx                          # the last commit that changed the deck
CreatePowerpointCheckpoint talk.pptx --commit sha1,sha2,sha3  # each of these commits
CreatePowerpointCheckpoint talk.pptx --pages 40               # only slide 40; past the end means the last slide
```

### Other options

```bash
CreatePowerpointCheckpoint talk.pptx --renderer LibreOffice   # draw with LibreOffice instead of DeckCraft
CreatePowerpointCheckpoint --reset                            # clear local settings
```

## Install

```bash
dotnet tool install --global PowerPointCompareForGit
```

The first compare sets up two things:
- **Slide drawing:** slides are drawn by [DeckCraft](https://github.com/storytold/deckcraft), and the tool offers to install it if it's missing.
- **Diff tool:** you choose a PNG-capable diff tool. It suggests any it finds (Beyond Compare, Kaleidoscope, P4Merge, Araxis Merge, WinMerge), or you can enter a custom command such as `mytool --flags {file1} {file2}`.

Your answer is saved to `~/.createpowerpointcheckpoint/settings.json`.

## Good to know

- With `--selective`, slides that moved but didn't change are hidden, and listed in the terminal as `Slides moved but unchanged: 19→23, 20→24.`
- With `--selective`, if no slide looks different, nothing opens and the terminal says so.
- Speaker notes, animations and transitions aren't in the images, so changes to them won't show.
- Previews are cached in `.git/pptx-previews/`, so git never tracks them and each commit is rendered only once.

## For AI agents

[skills/powerpoint-compare/SKILL.md](../../skills/powerpoint-compare/SKILL.md) is an [Agent Skill](https://agentskills.io) that teaches coding agents how to use this tool. Copy the `powerpoint-compare` folder into your agent's skills directory (e.g. `.claude/skills/`).

## License

Apache-2.0. See [LICENSE](../../LICENSE).
