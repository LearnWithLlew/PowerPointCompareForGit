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
Uses LibreOffice in headless mode: `soffice --headless --convert-to pdf`, then rasterizes each page and stitches them vertically into one long PNG.
- Free and cross-platform (Mac/Windows/Linux), works in CI.
- Requires LibreOffice to be installed.
- PDF pages are rasterized with the `PDFtoImage` NuGet package (PDFium, MIT) and stitched with SkiaSharp, so LibreOffice is the only external dependency.
- Accepted tradeoff: rendering is not pixel-identical to PowerPoint.

### Layout
- Each slide is rendered 1280px wide; height follows the deck's aspect ratio (16:9 → 1280×720, 4:3 → 1280×960, any custom size scales the same way).
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
