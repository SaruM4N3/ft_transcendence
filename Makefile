WEB_DIR := web
IMAGE := ft_transcendence-web
NAME := ft_transcendence-web

.PHONY: all up down build logs re clean fclean

all: up

build:
	docker build -t $(IMAGE) $(WEB_DIR)

up: build
	docker rm -f $(NAME) >/dev/null 2>&1 || true
	docker run -d --name $(NAME) -p 80:80 $(IMAGE)

down:
	docker rm -f $(NAME) >/dev/null 2>&1 || true

logs:
	docker logs -f $(NAME)

re: down up

clean: down

fclean: clean
	docker rmi -f $(IMAGE) >/dev/null 2>&1 || true
