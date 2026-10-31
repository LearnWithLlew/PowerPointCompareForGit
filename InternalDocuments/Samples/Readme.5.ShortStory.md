# <img src="https://raw.githubusercontent.com/LearnWithLlew/PowerPointCompareForGit/main/icon.png" alt="" width="48" align="center"> PowerPointCompareForGit

[![build-and-test](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)
[![NuGet downloads](https://img.shields.io/nuget/dt/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)

You gave your talk in Oslo in June, and you tagged the commit `oslo`. Since then you've reworked the 60-slide deck for a new audience, a bit at a time, in hotel rooms and on planes. Now it's the night before Berlin, and you want to rehearse the parts that are new.

What did you actually change?

```bash
CreatePowerpointCheckpoint talk.pptx --compare oslo --selective
```

Your diff tool opens with two images: Oslo on the left, tonight's deck on the right. Instead of 60 slides, you see four. Three you edited, with the differences highlighted, and one you added, sitting opposite a blank. The terminal mentions the rest:

```
Slides moved but unchanged: 19→23, 20→24.
```

Right, you moved the demo later. Now you know what to rehearse.

> **Status:** early development. All commands work; the detected diff-tool commands are not yet verified on every OS.

## Try it on your deck

```bash
dotnet tool install --global PowerPointCompareForGit
CreatePowerpointCheckpoint talk.pptx --compare --selective
```

With no version named, it compares your uncommitted edits to the last commit that changed the deck. With no uncommitted edits, it compares the last two commits that changed it.

The first compare sets up two things:
- **Slide drawing:** slides are drawn by [DeckCraft](https://github.com/storytold/deckcraft), and the tool offers to install it if it's missing.
- **Diff tool:** you choose a PNG-capable diff tool. It suggests any it finds (Beyond Compare, Kaleidoscope, P4Merge, Araxis Merge, WinMerge), or you can enter a custom command such as `mytool --flags {file1} {file2}`.

Your answer is saved to `~/.createpowerpointcheckpoint/settings.json`.

## Other commands

```bash
CreatePowerpointCheckpoint talk.pptx --compare oslo berlin --selective  # one sha, tag or branch vs another
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

- If no slide looks different, `--selective` opens nothing and the terminal says so.
- Speaker notes, animations and transitions aren't in the images, so changes to them won't show.
- Previews are cached in `.git/pptx-previews/`, so git never tracks them and each commit is rendered only once.

## For AI agents

[skills/powerpoint-compare/SKILL.md](../../skills/powerpoint-compare/SKILL.md) is an [Agent Skill](https://agentskills.io) that teaches coding agents how to use this tool. Copy the `powerpoint-compare` folder into your agent's skills directory (e.g. `.claude/skills/`).

## License

Apache-2.0. See [LICENSE](../../LICENSE).
