COMPOSE_FILE=infrastructure/docker/docker-compose.yml
ENV_FILE=infrastructure/docker/.env.docker

up:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) up -d --build

down:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) down

restart:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) down
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) up -d --build

build:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) build

logs:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) logs -f

ps:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) ps

clean:
	docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) down -v --remove-orphans

status:
	@echo "Docker"
	@docker --version
	@echo ""
	@echo ".NET"
	@dotnet --version
	@echo ""
	@echo "Containers"
	@docker compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) ps