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
# Then open http://localhost:5000 (or the port printed by dotnet)
```

## Conventions
@.claude/rules/coding-standards.md
@.claude/rules/architecture.md
@.claude/rules/design-principles.md
@.claude/rules/testing.md
@.claude/rules/workflow.md
