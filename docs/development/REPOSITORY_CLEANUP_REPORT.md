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

## Commits

See `git log` on `main` after this cleanup session for hashes and messages.
