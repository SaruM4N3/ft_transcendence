WEB_DIR := web
COMPOSE := docker compose -f $(WEB_DIR)/compose.yaml --project-directory $(WEB_DIR)

.PHONY: all setup up down build logs ps re clean fclean

all: up

# Fetch the vault's latest main if not yet initialized, never resets an existing checkout
setup:
	@[ -e Transcendence_obsidian/.git ] || { git submodule update --init --remote && git -C Transcendence_obsidian switch -q main; }

up: setup
	$(COMPOSE) up --build -d
	@echo "Transcendence is up: https://localhost:4443"

down:
	$(COMPOSE) down

build:
	$(COMPOSE) build

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps

re: down up

clean:
	$(COMPOSE) down -v

fclean: clean
	$(COMPOSE) down --rmi all
