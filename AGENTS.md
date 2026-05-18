# AGENTS.md - 6502 Emulator Vibe Agents

## Project
MOS 6502 emulator. Cycle-accurate. C#. .NET.

## Vibe Agents

| Agent | Role | Model |
|-------|------|-------|
| bug-fixer | Fix code bugs | mistral-medium-3.5 |
| coder | Implement features | mistral-medium-3.5 |
| planner | Plan tasks | mistral-medium-3.5 |
| analyst | Analyze problems | mistral-medium-3.5 |
| code-reviewer | Review code | mistral-medium-3.5 |
| test-writer | Write unit tests | mistral-medium-3.5 |
| documenter | Write docs | mistral-medium-3.5 |

### Use Agent
```bash
vibe --agent <name> "Task"
```
Example:
```bash
vibe --agent coder "Implement LDA"
```

### Rules
1. One task at a time.
2. Always test: `dotnet build && dotnet test`.
3. Update docs.
4. Clean code.
5. Write tests.
6. Commit: `feat: description`.
7. Use caveman English for agent tasks (save tokens).

## Karpathy Guidelines

### 1. Think First
- State assumptions.
- Ask if unsure.
- Show trade-offs.

### 2. Keep It Simple
- Minimal code.
- No specs.
- No dead code.

### 3. Surgical Changes
- Touch only what needed.
- Match existing style.
- Remove only your mess.

### 4. Goal-Driven
- Define success.
- Verify.
- Loop until done.

## Tools
- C# 12
- .NET 10.0
- NUnit 4.3.2