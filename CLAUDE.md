# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build entire solution
dotnet build Mastermind.slnx

# Run all tests
dotnet test Mastermind.slnx

# Run tests for a single project
dotnet test test/Mastermind.Core.UnitTests
dotnet test test/Mastermind.ConsoleApp.UnitTests
dotnet test test/Mastermind.WebApp.UnitTests

# Run a single test by name
dotnet test test/Mastermind.Core.UnitTests --filter "FullyQualifiedName~MethodName"

# Run the console app
dotnet run --project src/Mastermind.ConsoleApp
dotnet run --project src/Mastermind.ConsoleApp -- DEBUG

# Run the web app
dotnet run --project src/Mastermind.WebApp
# Then open http://localhost:5012 (or the port printed by dotnet)
```

## Conventions
@.claude/rules/core/coding-standards.md
@.claude/rules/core/design-principles.md
@.claude/rules/core/architecture.md
@.claude/rules/core/testing-philosophy.md
@.claude/rules/core/workflow-core.md
@.claude/rules/overlays/workflow-solo.md
@.claude/rules/archetype/application.md
@.claude/rules/overlays/frontend-blazor.md

These are copied from the shared [claude-rules](https://github.com/lgamorim/claude-rules)
repository via its `tools/sync.ps1`, composed as `application-solo -Add
frontend-blazor`. Adding an overlay makes the set a composition that matches no
profile, so the modules are imported directly rather than through a profile
manifest. Re-audit for drift from the claude-rules checkout with the **same
flags**, plus `-Check` -- it cannot infer how the set was composed:

```powershell
./tools/sync.ps1 -Target <path-to>\mastermind -Profile application-solo -Add frontend-blazor -Check
```

`Mastermind.Core` is an internal library consumed only by the console and web
apps in this repo (never shipped as a package), so this repo follows the
`application` archetype, not `library`.
