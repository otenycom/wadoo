# Copilot instructions for this repository

NOTE: repository currently has no source files. These instructions prioritize discovery-first actions
so an AI coding agent can quickly determine the project's shape, then follow project-specific
patterns. If you (human) add existing project files, re-run the discovery steps below and
update this document to include concrete commands and file references.

1. Quick discovery steps (run in repository root):
   - `git status` : confirm branch and changes.
   - `ls -la` : list top-level files (look for `package.json`, `pyproject.toml`, `go.mod`, `README.md`, `Dockerfile`, `docker-compose.yml`, `Makefile`).
   - If a file is present, open it (examples below show how to map files → workflows).

2. Mapping files to workflows (explicit, follow these when those files appear):
   - `package.json` → Node/JS: use `npm ci` (or `pnpm install`/`yarn install` if lockfile present). Run `npm test` for tests.
   - `pyproject.toml` or `requirements.txt` → Python: create a venv and `pip install -r requirements.txt` / `pip install -e .` then `pytest`.
   - `go.mod` → Go: `go test ./...`, build with `go build ./...`.
   - `Cargo.toml` → Rust: `cargo test` and `cargo build`.
   - `Makefile` → check `make help` or `make test` for project-specific targets.

3. Where to look for the "big picture" (when present):
   - `README.md` at repo root: first stop for architecture notes and run instructions.
   - `.github/workflows/*`: CI shows build/test matrix and environment variables.
   - `docker-compose.yml`, `k8s/`, and `deploy/`: reveal integration boundaries and services.
   - Monorepo layouts:
     - `packages/`, `apps/`/`libs/`, or `services/` commonly separate independent services.
     - `cmd/` vs `pkg/` (Go) or `src/` vs `tests/` (Python/JS) indicate service vs library responsibilities.

4. Patterns and conventions to detect and preserve:
   - Environment config: if `.env` and `env.example` exist, use the latter to fill local secrets.
   - Config-over-convention: look for `config/*.yaml|json|toml` or `config/` code that centralizes settings.
   - Database migrations: `migrations/` or `db/migrate` suggests running migration commands before tests.
   - API boundary files: open `routes/`, `api/`, or `handlers/` to see request/response shape.

5. Integration and external dependencies to note:
   - Cloud infra manifests (Terraform, Pulumi) under `infra/` or `terraform/` indicate deployment patterns.
   - `Dockerfile` + `docker-compose.yml` reveal local dev composition and ports to expose.
   - CI secrets/env in `.github/workflows` show required keys; do not attempt to add or reveal them.

6. Editing and pull request guidance for the agent:
   - Keep changes minimal and focused to a single logical purpose (one PR per service or feature).
   - Update or add tests alongside behavior changes. Run the project's test command and include failing test output if you cannot fix it.
   - Preserve existing code style and linters; if a formatter is present (e.g., `prettier`, `black`, `gofmt`), run it on touched files.

7. If the repo is empty (current state):
   - Ask the human which language/framework they intend to use or whether this is a fresh scaffold.
   - If scaffolding is requested, propose a minimal tree (README, .gitignore, LICENSE, example `README.md`) and a single service skeleton for the chosen language.

If any of these points are unclear or you want me to tailor this to a specific language or existing files you plan to add, tell me what to look for and I'll update this file.
