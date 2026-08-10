#!/usr/bin/env bash

set -euo pipefail

BASE_BRANCH="main"
REMOTE="origin"
BRANCH_PATTERN='^(feature|fix|hotfix|chore|docs|test|refactor)/[a-z0-9]+(-[a-z0-9]+)*$'

if [[ $# -ne 1 ]]; then
    echo "Usage: ./scripts/start-work.sh <branch>"
    echo "Example: ./scripts/start-work.sh feature/add-fairness-policy"
    exit 1
fi

BRANCH_NAME="$1"

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "Error: this command must run inside a Git repository."
    exit 1
fi

if ! git remote get-url "$REMOTE" >/dev/null 2>&1; then
    echo "Error: Git remote '$REMOTE' is not configured."
    echo "Configure it explicitly before starting new work."
    exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
    echo "Error: the working tree contains local changes."
    echo "Commit, stash, or discard them before starting new work."
    exit 1
fi

if [[ ! "$BRANCH_NAME" =~ $BRANCH_PATTERN ]]; then
    echo "Error: invalid branch name '$BRANCH_NAME'."
    echo "Use an allowed prefix and a lowercase kebab-case description."
    echo "Example: feature/add-fairness-policy"
    exit 1
fi

if ! git check-ref-format --branch "$BRANCH_NAME" >/dev/null 2>&1; then
    echo "Error: '$BRANCH_NAME' is not a valid Git branch name."
    exit 1
fi

if git show-ref --verify --quiet "refs/heads/$BRANCH_NAME"; then
    echo "Error: local branch '$BRANCH_NAME' already exists."
    exit 1
fi

echo "Fetching the latest references from '$REMOTE'..."
git fetch --prune "$REMOTE"

if ! git show-ref --verify --quiet "refs/remotes/$REMOTE/$BASE_BRANCH"; then
    echo "Error: source branch '$REMOTE/$BASE_BRANCH' does not exist."
    exit 1
fi

if git show-ref --verify --quiet "refs/remotes/$REMOTE/$BRANCH_NAME"; then
    echo "Error: remote branch '$REMOTE/$BRANCH_NAME' already exists."
    exit 1
fi

git switch -c "$BRANCH_NAME" "$REMOTE/$BASE_BRANCH"

echo
echo "Branch '$BRANCH_NAME' created from '$REMOTE/$BASE_BRANCH'."
git status --short --branch
