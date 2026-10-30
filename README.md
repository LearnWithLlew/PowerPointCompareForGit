# PowerPointCompareForGit

[![build-and-test](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/LearnWithLlew/PowerPointCompareForGit/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)
[![NuGet downloads](https://img.shields.io/nuget/dt/PowerPointCompareForGit.svg)](https://www.nuget.org/packages/PowerPointCompareForGit)

A .NET tool that shows what changed in a PowerPoint deck across git commits. It renders each version of a `.pptx` to a single tall PNG (all slides stacked) and opens two versions side by side in your diff tool.

> **Status:** early development. Argument parsing works; rendering and comparing are not implemented yet.

## Install

```bash
dotnet tool install --global PowerPointCompareForGit
```

Requires [LibreOffice](https://www.libreoffice.org/download/download-libreoffice/) for rendering:

```bash
brew install --cask libreoffice                       # Mac
winget install TheDocumentFoundation.LibreOffice      # Windows
sudo apt install libreoffice                          # Linux
```

## Usage

```bash
CreatePowerpointCheckpoint file.pptx                          # render the last commit that changed the file
CreatePowerpointCheckpoint file.pptx --commit sha1,sha2,sha3  # render these commits
CreatePowerpointCheckpoint file.pptx --compare                # working copy vs last commit that changed the file
CreatePowerpointCheckpoint file.pptx --compare sha1           # working copy vs sha1
CreatePowerpointCheckpoint file.pptx --compare sha1 sha2      # sha1 vs sha2
CreatePowerpointCheckpoint --reset                            # clear local settings
```

Previews are cached in `.git/pptx-previews/`, so git never tracks them and each commit is rendered only once.

## Configure

The first `--compare` asks which diff tool to use and suggests any PNG-capable ones it finds (Beyond Compare, Kaleidoscope, P4Merge, Araxis Merge, WinMerge), or a custom command such as `mytool --flags {file1} {file2}`. The answer is saved to `~/.createpowerpointcheckpoint/settings.json`. `--reset` deletes that file.

## License

Apache-2.0. See [LICENSE](LICENSE).
