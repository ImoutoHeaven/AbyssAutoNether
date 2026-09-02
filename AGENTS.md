# Agent Instructions

## Documentation authority

- [`README.md`](README.md) is the user-facing installation, configuration, and troubleshooting guide.
- [`CONTEXT.md`](CONTEXT.md) is the canonical domain glossary. Keep it free of implementation details, evidence logs, and design history.
- [`docs/specs/evidence-backed-strategy-modes.md`](docs/specs/evidence-backed-strategy-modes.md) is the normative automation behavior specification.
- [`docs/design/autonether-architecture.md`](docs/design/autonether-architecture.md) is the current runtime, compatibility, and verification architecture.
- Git history is the archive for completed plans, RCAs, test counts, and superseded native hashes. Do not preserve those as active documents.

## Agent skills

- Use [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md) to locate and publish project work.
- Use [docs/agents/triage-labels.md](docs/agents/triage-labels.md) for issue readiness and ownership states.
- Use [docs/agents/domain.md](docs/agents/domain.md) to locate the canonical domain glossary and ADRs.

## Decompiled game-design precedence

- Current game design wins only when a spec or ticket conflict is proven by the current assembly hash plus exact type, member, and control-flow evidence. Record the resulting current rule in the affected specification and glossary; incomplete evidence does not override an approved requirement.
