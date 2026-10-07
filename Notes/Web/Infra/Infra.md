---
graph-size: 200
---
#module #web

Orchestration et terminaison TLS.

## Fichiers

- [compose.yaml](../../../web/compose.yaml) — orchestration : service web + service `certs` (génère un cert self-signed au premier boot) + nginx
- [nginx/nginx.conf](../../../web/nginx/nginx.conf) — reverse proxy, redirect HTTP→HTTPS, terminaison TLS

## Connexions

- Orchestre [[Server]]
