# PowerPoint for Git Repos

A dotnet tool that lets you see what changed in a PowerPoint deck across git commits. It renders each version of a `.pptx` to a single tall PNG (all slides stacked) and opens two versions side by side in your diff tool.

For a deck with 4 commits, there would be 4 corresponding preview PNGs, allowing diffing on your computer. Previews can be created retroactively for older commits.

## Usage

```bash
CreatePowerpointCheckpoint file.pptx                          # create the png for the last commit the file was edited in
CreatePowerpointCheckpoint file.pptx --commit sha1,sha2,sha3  # create 3 pngs
CreatePowerpointCheckpoint file.pptx --compare sha1 sha2      # sha1 vs sha2
CreatePowerpointCheckpoint file.pptx --compare                # working copy vs last commit that changed the file (or last two such commits if no uncommitted edits)
CreatePowerpointCheckpoint file.pptx --compare sha1           # working copy vs sha1
CreatePowerpointCheckpoint --reset                            # clears local settings
```

### Output
Without `--compare`, the tool prints the path of each PNG and opens nothing. Previews already in the cache are not re-rendered; their paths are just printed.

With `--compare`, it renders both versions (if needed) and opens them in the configured diff tool.

### Uncommitted edits
The tool never creates commits. Uncommitted edits are rendered directly from the file on disk to `my.pptx.working.png` (in the same folder as the cached previews), which is overwritten each run.

## Rendering
Uses LibreOffice in headless mode, with no native code in our own package:
1. Split the deck into one single-slide `.pptx` per slide with `DocumentFormat.OpenXml` (managed, MIT).
1. Convert all of them to PNG in a single launch: `soffice --headless --convert-to png --outdir <tmp> slide1.pptx slide2.pptx ...`
1. Stitch the PNGs vertically into one long PNG with `StbImageSharp` / `StbImageWriteSharp` (managed, public domain/MIT). Slide labels use a built-in 5x7 pixel font, so no system fonts are needed.

- Free and cross-platform (Mac/Windows/Linux), works in CI.
- Requires LibreOffice to be installed; it is the only external dependency.
- Accepted tradeoff: rendering is not pixel-identical to PowerPoint.
- Why not `--convert-to png` with a `PageRange` filter option: LibreOffice 25.2 ignores `PageRange` and `PixelWidth` and always exports slide 1 at its default size.
- Why not PDF + PDFtoImage/SkiaSharp: it would ship native assets per OS.
- Why not ImageSharp: 4.x fails Release builds without a Six Labors license key, even for open source.
- LibreOffice runs with a private profile folder (`-env:UserInstallation`), so it works while the user has LibreOffice open.
- Measured: a 3-slide deck converted in one launch in 1.78 s (LibreOffice 25.2, Mac).

### Layout
- Slides keep LibreOffice's default export size, with no resizing, so text stays sharp (e.g. a 4:3 deck exports at 960×720). Any slide size, including custom ones, is supported; the same deck always renders at the same size, so versions line up in the diff tool.
- Why not upscale to a fixed width: upscaling blurs text, which hides small changes. Why not enlarge the slide size before export: text and shapes can reflow, so the preview would no longer match the deck.
- Slides are stacked vertically, separated by a 40px gray band labeled "Slide N".
- Known limitation: very long decks (~100+ slides) may exceed some diff tools' max image size.

### Finding LibreOffice
1. `libreOfficePath` in settings.json, if set
1. `soffice` on PATH
1. Standard install locations:
   - Mac: `/Applications/LibreOffice.app/Contents/MacOS/soffice`
   - Windows: `C:\Program Files\LibreOffice\program\soffice.exe`

If not found, it offers to install it:
```
LibreOffice was not found.
1. Install via brew (brew install --cask libreoffice)     # Mac
1. Install via winget (winget install TheDocumentFoundation.LibreOffice)   # Windows
2. Cancel (or set libreOfficePath in ~/.createpowerpointcheckpoint/settings.json)
```
- If input is piped (e.g. CI), it does not prompt; it errors with the install command and the settings option.
- On Linux it never offers to install (that would need `sudo`); it errors with `sudo apt install libreoffice` and the `libreOfficePath` option.

## Storage
Previews are cached inside the repo's `.git/` folder, so git never tracks them and no `.gitignore` changes are needed:

```
.git/pptx-previews/<path-to-deck>/my.pptx.<shortsha>.png
.git/pptx-previews/<path-to-deck>/my.pptx.working.png
```

- Uses the short (7-char) SHA so file names stay readable in the diff tool.
- The cache persists, so each commit is rendered only once.
- `working.png` is overwritten each run.

## Settings
Stored in `~/.createpowerpointcheckpoint/settings.json` (same pattern as SafeQuery's `~/.safequery/settings.json`).
- One global setting per user, not per repo.
- `--reset` deletes the file.
- If input is piped (e.g. CI), it does not prompt; it errors with instructions on where to put the setting.

### Diff tool
The setting holds a diff command such as `mytool --flags {file1} {file2}`.

If it isn't set, the tool searches for known diff tools that support PNG (on PATH or at standard install locations) and suggests the ones it finds, followed by a custom option:

| Tool | Platforms | Command |
|---|---|---|
| Beyond Compare | Mac/Win/Linux | `bcomp {file1} {file2}` |
| Kaleidoscope | Mac | `ksdiff {file1} {file2}` |
| P4Merge | Mac/Win/Linux | `p4merge {file1} {file2}` |
| Araxis Merge | Mac/Win | `compare {file1} {file2}` |
| WinMerge | Win | `WinMergeU {file1} {file2}` |
| custom | any | e.g. `mytool --flags {file1} {file2}` |

Note: executable names and install paths above are unverified; confirm before implementing.

## Platform
Targets `net10.0` (current LTS), with `global.json` pinning SDK `10.0.402`, matching SafeQuery.
Argument parsing uses `System.CommandLine`, matching SafeQuery.

## Naming
- Repo, solution, project and NuGet package ID: `PowerPointCompareForGit`.
- Command name: `CreatePowerpointCheckpoint` (set via `<ToolCommandName>`).
- License: Apache-2.0.

## Project Structure

```
PowerPointCompareForGit.slnx
src/PowerPointCompareForGit/               # the dotnet tool
tests/PowerPointCompareForGit.Tests/       # xunit tests
tools/create-commit-message.sh             # commit message builder
build_and_test.sh / build_and_test.cmd     # local and CI build entry point
```

CI: GitHub Actions
- build and test (uses the script) on push and PR
- publish on GitHub release, version from tag (`v1.2.3` becomes `1.2.3`), using nuget.org trusted publishing

## Release Checklist

Tracked in [CreateNewProject.checklist.md](CreateNewProject.checklist.md).

## TODO

1. Verify the diff-tool executable names and install paths in the Diff tool table.
1. Verify the LibreOffice install paths and the brew/winget package names.
1. Deck folders whose names start with `..` (e.g. `..decks/`) are rejected as outside the repo. Flip `KnownGap_FolderStartingWithDotDotIsRejected` in `PreviewPathsTests` when fixed.
1. In git worktrees and submodules `.git` is a file, not a folder, so the preview cache path is wrong. Use `git rev-parse --git-dir` instead. Flip `KnownGap_WorktreeGitFileIsTreatedAsFolder` in `PreviewPathsTests` when fixed.
1. Verify the OpenXml slide split keeps each slide's layout, master and media (test with a real deck with images and custom layouts).
