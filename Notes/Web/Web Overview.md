---
graph-size: 300
---
#Main #web

Scaffold minimal, pas encore l'architecture finale (voir `.datas/en.subject.pdf`, gitignored, pour le sujet complet).

## Modules

- [[Server]] — serveur Node.js (`server.js`, `Dockerfile`)
- [[Frontend]] — front actuel (`public/`), dépôt du build WebGL du jeu
- [[Infra]] — orchestration Docker Compose + nginx/TLS

## Ce qui manque encore (mandatory part du sujet)

Framework frontend/backend, base de données, gestion utilisateurs, pages Privacy Policy/Terms of Service, secrets via `.env` — rien de tout ça n'est câblé pour l'instant.

## Connexions

- Le seul lien avec [[Game Overview]] est le dépôt du build WebGL dans `public/game/` ([[Frontend]]) — sinon les deux codebases sont indépendantes
