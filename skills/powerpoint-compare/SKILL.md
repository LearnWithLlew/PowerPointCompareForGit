---
name: powerpoint-compare
description: See what changed in a PowerPoint (.pptx) deck across git commits by rendering versions to PNG and opening them in a diff tool, using the CreatePowerpointCheckpoint dotnet tool. Use when the user asks what changed in a .pptx, wants to compare deck versions, or wants slide previews for a commit.
---

# PowerPoint compare

`CreatePowerpointCheckpoint` renders each version of a `.pptx` in a git repo to a single tall PNG (all slides stacked) and opens two versions side by side in the user's diff tool.

> **Status:** early development. Rendering works (`CreatePowerpointCheckpoint deck.pptx` and `--commit`). `--compare` and `--reset` exit 1 with "Not implemented yet"; tell the user rather than retrying.

## Setup

1. Install the tool: `dotnet tool install --global PowerPointCompareForGit`
1. LibreOffice must be installed (`brew install --cask libreoffice`, `winget install TheDocumentFoundation.LibreOffice`, or `sudo apt install libreoffice`). If it is missing, the tool errors with the install command; ask the user before installing anything.

## Commands

Run from inside the git repo that contains the deck.

```bash
CreatePowerpointCheckpoint deck.pptx                          # render the last commit that changed the file
CreatePowerpointCheckpoint deck.pptx --commit sha1,sha2,sha3  # render these commits
CreatePowerpointCheckpoint deck.pptx --compare                # working copy vs last commit that changed the file
CreatePowerpointCheckpoint deck.pptx --compare sha1           # working copy vs sha1
CreatePowerpointCheckpoint deck.pptx --compare sha1 sha2      # sha1 vs sha2
CreatePowerpointCheckpoint --reset                            # delete the user's settings
```

- `--commit` and `--compare` can't be combined. `--compare` takes at most two shas.
- Without `--compare`, the tool prints the path of each PNG and opens nothing. Read those PNGs yourself to describe the slides.
- With `--compare`, it opens the user's diff tool. That is for the human; to compare versions yourself, render both with `--commit` and read the two PNGs.

## Output

- Previews are cached at `.git/pptx-previews/<path-to-deck>/<deck>.pptx.<shortsha>.png`; uncommitted edits go to `<deck>.pptx.working.png`. Git never tracks them, so don't add them to `.gitignore` or commit them.
- Slides are stacked vertically with a gray band labeled "Slide N" between them.
- Errors go to stderr with exit code 1.

## Things to avoid

- Don't run `--reset` unless the user asks; it deletes their diff-tool choice.
- The tool never creates commits. Don't commit or stash the user's work to make a comparison possible.
- When input is piped (as in most agent shells), the tool won't prompt for a diff tool; it errors with where to set it (`~/.createpowerpointcheckpoint/settings.json`). Ask the user which diff tool they want rather than editing that file on your own.
