.PHONY: help start build test-fast test-integration test verify sync-main

help:
	@printf '%s\n' \
		'make help                         List contributor commands.' \
		'make start branch=feature/name    Create a work branch from latest origin/main.' \
		'make build                        Build Festival.sln.' \
		'make test-fast                    Run tests that do not require Docker.' \
		'make test-integration             Run PostgreSQL integration tests (Docker required).' \
		'make test                         Run the complete test suite (Docker required).' \
		'make verify                       Build and run all tests for PR validation.' \
		'make sync-main					   Synchronize local main with origin/main.'

start:
	@if [ -z "$(branch)" ]; then \
		echo "Error: branch is required. Usage: make start branch=feature/example"; \
		exit 1; \
	fi
	@./scripts/start-work.sh "$(branch)"

build:
	dotnet build Festival.sln

test-fast:
	dotnet test Festival.FastTests.slnf

test-integration:
	DOCKER_API_VERSION=1.43 dotnet test tests/Festival.Infrastructure.IntegrationTests

test:
	DOCKER_API_VERSION=1.43 dotnet test Festival.sln

verify: build
	dotnet test Festival.FastTests.slnf --no-build
	DOCKER_API_VERSION=1.43 dotnet test tests/Festival.Infrastructure.IntegrationTests --no-build

sync-main:
	@if [ -n "$$(git status --porcelain)" ]; then \
		echo "Error: existen cambios locales pendientes."; \
		echo "Haz commit, stash o descarta los cambios antes de continuar."; \
		exit 1; \
	fi
	git fetch --prune origin
	git switch main
	git pull --ff-only origin main