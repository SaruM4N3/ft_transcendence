_This project has been created as part of the 42 curriculum by zsonie, erbuffet, jureix-c._

## Description

**Transcendence** is the final project of the 42 common core: a group web application, built around
a custom Unity multiplayer game. The repository holds two things with very different lifecycles:

- `web/` — the actual deliverable required by the subject: a web app (frontend, backend, database)
  with user accounts and everything else `ft_transcendence` requires of a "real" web app around the
  game.
- `UnityProject/` — the Unity **source** project the game is built and iterated on in. It is not part
  of the web app's runtime; only its exported WebGL **build** (not its source) is meant to end up
  served as a static asset by the web app.

The game itself is a 2D top-down "Vampire Survivors"-like, built around three multiplayer modes
rather than a single-player loop:

- **4-player co-op** — all players fight the horde together.
- **2 vs 2** — two teams of survivors fight each other (and the horde).
- **3 vs 1 asymmetric** — three survivors vs. one player controlling/leading the horde as a "horde
  leader".

<!-- TODO: expand this description with the team's actual pitch/key features once the web app and the
     chosen modules take shape — see the Modules section below. -->

**Current status:** both sides of the project are early-stage. The web app is a bare scaffold (static
page behind an HTTPS reverse proxy, no framework/database/auth yet) and the Unity game has core player
movement/combat/stats and procedural terrain, but no networking, enemy AI, or the three game modes
wired in yet. See [Features List](#features-list) below for exactly what exists today.

## Instructions

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose (v2+).
- Node.js 22+ and npm, only if you want to run/edit the web app outside Docker.
- [Unity Hub](https://unity.com/download) with the Unity **6000.5.6f1** editor installed, only if you
  want to open/edit the game source in `UnityProject/`.

### Running the web app

```sh
make up
# or, equivalently:
cd web && docker compose up --build -d
```

This builds the app image, generates a self-signed TLS certificate on first boot, and starts the app
behind an nginx reverse proxy:

- App: https://localhost (self-signed certificate — your browser will warn you, that's expected for
  local development)

Other shortcuts from the `Makefile` (repository root):

| Command      | Effect                                              |
| ------------ | ---------------------------------------------------- |
| `make up`    | Build and start the stack in the background          |
| `make down`  | Stop the stack                                        |
| `make build` | Rebuild images without starting                        |
| `make logs`  | Follow container logs                                  |
| `make ps`    | Show container status                                  |
| `make re`    | `down` then `up`                                       |
| `make clean` | `down` and remove volumes                               |
| `make fclean`| `clean` and remove built images                        |

<!-- TODO: once a database/auth stack is added, document `.env` / `.env.example` setup here (the
     subject requires secrets to live in a gitignored `.env` with a committed `.env.example`) and any
     migration commands needed before first boot. -->

### Opening the game source (optional, for game development only)

```sh
# Open UnityProject/ in Unity Hub with editor version 6000.5.6f1
```

This is only needed to work on the Unity game itself; it is not required to run the web app.

## Resources

- [Docker Compose docs](https://docs.docker.com/compose/)
- [nginx docs](https://nginx.org/en/docs/)
- [Node.js docs](https://nodejs.org/docs/latest/api/)
- [Unity Manual](https://docs.unity3d.com/Manual/index.html)
- [Unity Netcode for GameObjects docs](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- 42 `ft_transcendence` subject — available on the 42 intranet (not linked here since it isn't a
  public URL; a local copy is kept in the gitignored `.datas/` folder for reference)

<!-- TODO: add framework/library docs here once the web stack (frontend, backend, database, auth) is
     chosen. -->

### AI usage

This project uses Claude (Anthropic) as a coding assistant. Concretely, in this repository so
far, it was used for:

- Scaffolding the bare-bones static web app (`web/`) and its Docker/Compose/nginx setup (HTTPS via a
  self-signed certificate, reverse proxy config), iteratively tested end-to-end (built, run, and
  exercised with real HTTP requests) rather than written blind.
- Maintaining this repository's `README.md` as the project evolves.

<!-- TODO: keep this section updated as AI is used for more parts of the project — the subject
     requires specifying which tasks and which parts of the project it was used for. -->

## Team Information

The team will grow to 4-5 people; for now, **zsonie** is working solo and covers the Product
Owner, Technical Lead, and Developer roles, focusing mainly on the Unity game side. The other two
members will pick up Developer work first, with their final role(s) decided once they're active on
the project.

- **zsonie** — Product Owner, Technical Lead, Developer: defines product priorities, owns
  architecture/technical decisions, and implements features — currently focused mainly on the
  Unity game (`UnityProject/`).
- **erbuffet** — Developer <!-- TODO: additional role(s) once active on the project -->
- **jureix-c** — Developer <!-- TODO: additional role(s) once active on the project -->

<!-- TODO: Project Manager / Scrum Master isn't assigned yet — pick this up once the team is
     active (see the subject's "Required Team Roles"). Also flesh out erbuffet/jureix-c's
     responsibilities once they start contributing. -->

## Project Management

- **Branching model**: `main` (always deployable, protected — PRs only, CI must pass) is fed by two
  separate integration branches, matching the repo's two very different lifecycles (see
  [Description](#description)):
  - `develop/web` — integration branch for everything under `web/`.
  - `develop/game` — integration branch for everything under `UnityProject/`.

  Day-to-day work happens on `feature/<short-name>` branches, branched off whichever of the two
  matches what's being worked on, merged back into it via Pull Request.
- **Workflow**: branch off `develop/web` or `develop/game` as `feature/<short-name>`, open a PR
  into that same branch when ready (CI runs automatically, see [CI/CD](#cicd)), get it reviewed by
  at least one teammate, merge. `develop/web`/`develop/game` are merged into `main` at
  milestones / before evaluation.

<!-- TODO: fill in the tools the team actually uses for task tracking (GitHub Issues/Projects,
     Trello, etc.), the communication channel (Discord, Slack, etc.), and meeting cadence. -->

## CI/CD

GitHub Actions run automatically on every push/PR that touches the relevant part of the repo (see
`.github/workflows/`):

- **Web CI** (`web-ci.yml`) — on any push/PR to `main`/`develop/web` touching `web/**`: lints and builds
  it with npm once a frontend/backend framework is added (for now, since there's no `package.json`
  yet, it just syntax-checks the plain JS scaffold), validates `docker compose config`, and builds the
  Docker image. Required to pass before merging into `main`.
- **Unity Build Check** (`unity-ci.yml`) — on any push/PR touching `UnityProject/**`: compiles the
  Unity project headlessly via [GameCI](https://game.ci/) (no test suite exists yet, so this is a
  compile check only). **Currently a no-op** until a Unity license is added as repo secrets — see the
  comment at the top of the workflow file for setup steps.

## Technical Stack

- **Frontend & backend**: not yet chosen/implemented. `web/` currently serves a static page via a
  plain `node:http` server (`web/server.js`) — this is a placeholder, not the final stack.
- **Database**: not yet implemented.
- **Authentication**: not yet implemented.
- **Deployment**: Docker Compose (`web/compose.yaml`), with an nginx reverse proxy (`web/nginx/`)
  terminating HTTPS via a self-signed certificate generated on first boot.
- **Game**: Unity 6000.5.6f1 (source lives in `UnityProject/`; only its WebGL build output is meant to
  be consumed by the web app).

<!-- TODO: replace the "not yet chosen/implemented" lines above once the team picks a frontend
     framework, backend framework, database, and auth solution, and add a one-line justification for
     each major choice (the subject explicitly asks for this). -->

## Database Schema

<!-- TODO: no database exists yet. Once one is added, document it here: a visual/textual schema,
     tables and their relationships, and key fields/data types for each. -->

## Features List

- Static landing page (`web/public/index.html`) served behind an HTTPS reverse proxy — no interactive
  features yet.
- Unity game skeleton (`UnityProject/`): core player movement/combat/stats
  (`UnityProject/Assets/Scripts/Player/`), procedural chunk-based terrain generation
  (`UnityProject/Assets/Scripts/Terrain/`), and basic main-menu/pause UI
  (`UnityProject/Assets/Scripts/UI/`). Not yet networked, no enemies/AI, none of the three game modes
  implemented, and not yet embedded into the web app.

<!-- TODO: as features are implemented, list each one here with who worked on it and a one-line
     description of what it does — e.g.:

- **Email/password signup & login** (<login>) — form validation + hashed/salted password storage.
- **Privacy Policy / Terms of Service pages** (<login>) — linked from the site footer.
-->

## Modules

<!-- TODO: once the team has decided which modules to pursue (14 points minimum required), list them
     here with:
     - Major (2 pts) or Minor (1 pt) classification and running point total.
     - A justification for each choice, especially for any custom "Modules of choice".
     - How each module was implemented and which team member(s) worked on it.
-->

## Individual Contributions

### zsonie

Solo on the project so far, covering both sides:

- Unity game (`UnityProject/`): player movement/combat/stats scripts, procedural terrain
  generation, main-menu/pause UI.
- Web scaffold (`web/`): the static app, Docker/Compose setup, and nginx HTTPS reverse proxy.
- Repo setup: branching model, GitHub Actions CI (`web-ci.yml`, `unity-ci.yml`), and this README.

<!-- TODO: challenges run into and how they were overcome. -->

### erbuffet

<!-- TODO: what they implemented (features/modules/components), and any challenges + how they were overcome. -->

### jureix-c

<!-- TODO: what they implemented (features/modules/components), and any challenges + how they were overcome. -->
