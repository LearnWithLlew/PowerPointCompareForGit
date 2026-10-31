# <img src="https://raw.githubusercontent.com/LearnWithLlew/PowerPointCompareForGit/main/icon.png" alt="" width="48" align="center"> PowerPointCompareForGit

[![build-and-test](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)
[![NuGet downloads](https://img.shields.io/nuget/dt/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)

## See what changed in your deck

```bash
CreatePowerpointCheckpoint talk.pptx --compare --selective
```

This opens your diff tool with only the slides that changed, side by side. It compares your uncommitted edits to the last commit that changed the deck. With no uncommitted edits, it compares the last two commits that changed it.

<a href="https://raw.githubusercontent.com/LearnWithLlew/PowerPointCompareForGit/main/compare-selective-screenshot.png"><img src="https://raw.githubusercontent.com/LearnWithLlew/PowerPointCompareForGit/main/compare-selective-screenshot.png" alt="VisualDiffer after --compare --selective: slide 4 edited with the difference highlighted, and slide 5 added opposite a blank" width="400"></a>

## Install

```bash
dotnet tool install --global PowerPointCompareForGit
```

The first compare sets up two things:
- **Slide drawing:** slides are drawn by [DeckCraft](https://github.com/storytold/deckcraft), and the tool offers to install it if it's missing.
- **Diff tool:** you choose a PNG-capable diff tool. It suggests any it finds (Beyond Compare, Kaleidoscope, P4Merge, Araxis Merge, WinMerge), or you can enter a custom command such as `mytool --flags {file1} {file2}`.

Your answer is saved to `~/.createpowerpointcheckpoint/settings.json`.

## Other commands

```bash
CreatePowerpointCheckpoint talk.pptx --compare oslo --selective       # working copy vs a sha, tag or branch

CreatePowerpointCheckpoint talk.pptx --compare oslo berlin --selective  # one version vs another

CreatePowerpointCheckpoint talk.pptx --compare                        # every slide, not just the changed ones

CreatePowerpointCheckpoint talk.pptx --compare --pages 2-25           # only slides 2 to 25, stitched into one PNG per version

CreatePowerpointCheckpoint talk.pptx                                  # write a PNG of the last commit that changed the deck, and open nothing

CreatePowerpointCheckpoint talk.pptx --commit sha1,sha2,sha3          # write a PNG of each of these commits

CreatePowerpointCheckpoint talk.pptx --pages 40                       # write a PNG of only slide 40; past the end means the last slide

CreatePowerpointCheckpoint talk.pptx --renderer LibreOffice           # draw with LibreOffice instead of DeckCraft

CreatePowerpointCheckpoint --reset                                    # clear local settings
```

`--selective` can't be combined with `--pages`.

## Good to know

- Slides that moved but didn't change are hidden, and listed in the terminal as `Slides moved but unchanged: 19→23, 20→24.`
- If no slide looks different, nothing opens and the terminal says so.
- Speaker notes, animations and transitions aren't in the images, so changes to them won't show.
- Previews are cached in `.git/pptx-previews/`, so git never tracks them and each commit is rendered only once.

## For AI agents

[skills/powerpoint-compare/SKILL.md](skills/powerpoint-compare/SKILL.md) is an [Agent Skill](https://agentskills.io) that teaches coding agents how to use this tool. Copy the `powerpoint-compare` folder into your agent's skills directory (e.g. `.claude/skills/`).

## License

Apache-2.0. See [LICENSE](LICENSE).
