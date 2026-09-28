WEB_DIR := web
COMPOSE := docker compose -f $(WEB_DIR)/compose.yaml --project-directory $(WEB_DIR)

.PHONY: all up down build logs ps re clean fclean

all: up

up:
	$(COMPOSE) up --build -d
	@echo "TRANSCENDENCE is up: https://localhost:4443"
	@echo "GAME is up: https://localhost:4443/game"
	@echo "ADMINER is up: http://localhost:8180"

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
