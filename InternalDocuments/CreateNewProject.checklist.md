# Create New Project Checklist

## Repo scaffolding
1. [x] `.gitattributes`, `.gitignore`, `.editorconfig`
1. [x] `LICENSE` and `NOTICE`
1. [x] `AGENTS.md` with working rules
1. [x] `InternalDocuments/project.md` spec: decisions, structure, release checklist, TODOs
1. [ ] `tools/create_message.py` commit message script, documented in `AGENTS.md`
1. [ ] Solution file (`.slnx`)
1. [ ] `global.json` pinning the SDK
1. [ ] Central package management (`Directory.Packages.props`)
1. [ ] Project skills in `.claude/skills/` (e.g. C# style)

## Tests
1. [ ] xunit test project in the solution
1. [ ] Extract logic out of `Program` into testable classes
1. [ ] Record known gaps as named passing tests, with TODOs to flip them

## Build and CI
1. [ ] `build_and_test.sh` and `build_and_test.cmd`
1. [ ] Build and test workflow on push and PR
1. [ ] Tidy code workflow (main only)
1. [ ] Markdown link check workflow
1. [ ] Dependabot for NuGet and GitHub Actions
1. [ ] CI uses `global.json`
1. [ ] Claim OpenSource sponsorship exemption

## Packaging
1. [ ] Package metadata: ID, description, license, repository URL
1. [ ] Version comes from the release tag, not the csproj
1. [ ] Package icon
1. [ ] Native assets for every target OS
1. [ ] Check the package ID is free on nuget.org

## Publishing
1. [ ] Publish workflow on GitHub release
1. [ ] NuGet trusted publishing policy (owner, repo, workflow file, package glob)
1. [ ] `NUGET_USER` repo secret
1. [ ] Cut first release and confirm the publish run with `gh`

## README
1. [ ] Build and NuGet badges
1. [ ] Install and usage docs
