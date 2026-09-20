# Repository cleanup report

**Date:** 20 September 2026  
**Branch:** `main`  
**Repository:** Care Home Back-Office (Angular + ASP.NET Core), not the separate Spring Finance Platform template.

## Summary

- Moved root-level demo, pilot, operations, and archive Markdown into `docs/demo/`, `docs/pilot/`, `docs/operations/`, and `docs/archive/`.
- Added `docs/README.md` as the documentation index; updated cross-links in runbooks, scripts, and API email messages.
- Expanded `.gitignore` for logs, local SQL scratch files, `.env.demo`, and IDE noise while keeping `.env.example` templates tracked.
- Left local-only files on disk (compose logs, `debug.log`, `tmp_demo_inspect.sql`) untracked via ignore rules.
- Did not delete Flyway/SQL migrations, tests, or `artifacts/` content (already ignored).

## Validation

- `dotnet build` on `backend/CareHome.Api`
- `npm run build` on `frontend/care-home-web` (see commit session output)

## Commits (this session)

| Hash | Message |
|------|---------|
| `4b0d47f` | chore(repo): organize documentation and tighten ignore rules |
| `db71d78` | feat(docker): add Compose stack for local and demo environments |
| `52202ed` | feat(email): harden production SMTP configuration and startup checks |
| `1b7b237` | feat(azure): extend deployment IaC and backup operational scripts |
| `aa12f83` | feat(ui): improve demo-ready workspace layout and shared components |

## Validation results

- `dotnet build backend/CareHome.Api/CareHome.Api.csproj` — succeeded (0 warnings)
- `npm run build` in `frontend/care-home-web` — succeeded (Angular budget warning only)
