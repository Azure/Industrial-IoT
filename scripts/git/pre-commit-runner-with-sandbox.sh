#!/usr/bin/env bash

set -e

# Path relative to .git/hooks/
APP_HOME="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && cd .. && pwd )/"
cd $APP_HOME

./scripts/git/pre-commit.sh --with-sandbox

set +e
