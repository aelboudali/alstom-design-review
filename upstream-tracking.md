# Upstream tracking

We track `Unity-Technologies/unity-industry-viewer-template` as the `upstream` git remote.

## Current pin

- **Upstream version:** 2.2.1
- **Pinned commit:** `b61299f06baaaefa3e0b349671461930016aac60`
- **Pin date:** 2026-04-29

## Merge cadence

Merge upstream between phases (A → B, B → C). Resolve conflicts in feature folders we own (`Features/Alstom.*`, `Features/Pixyz.*`); accept upstream for everything else unless deliberately overridden.

## Merge command

```bash
git fetch upstream
git merge upstream/main
# Resolve conflicts; prefer upstream for template features.
```

## Modifications to upstream files

Track any edits we make to template-owned files here, so the next merge surfaces them as expected conflicts rather than surprises.

| Date | File | Reason | Phase task |
|---|---|---|---|
| _none yet_ | | | |
