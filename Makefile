WEB_DIR := web
COMPOSE := docker compose -f $(WEB_DIR)/compose.yaml --project-directory $(WEB_DIR)

.PHONY: all setup up down build logs ps re clean fclean

all: up

VAULT := Transcendence_obsidian

# Put the vault on its latest main if missing or detached, fast-forward main, leave other branches alone
setup:
	[ -e $(VAULT)/.git ] || git submodule update --init --remote
	git -C $(VAULT) symbolic-ref -q HEAD >/dev/null || git -C $(VAULT) switch -q main
	[ "$$(git -C $(VAULT) branch --show-current)" != main ] || git -C $(VAULT) pull -q --ff-only || echo "warning: could not update $(VAULT)"

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
