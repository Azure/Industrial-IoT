#!/usr/bin/env bash

# Sometimes when creating bash scripts in Windows, bash scripts will not have
# the +x flag carried over to Linux/MacOS. This script should help setting the
# permission flags right.

set -e
APP_HOME="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && cd .. && cd .. && pwd )/"
cd $APP_HOME

chmod ugo+x ./scripts/build
chmod ugo+x ./scripts/compile
chmod ugo+x ./scripts/run
chmod ugo+x ./scripts/clean-up
chmod ugo+x ./scripts/env-vars-check
chmod ugo+x ./scripts/env-vars-setup
chmod ugo+x ./scripts/travis
chmod ugo+x ./scripts/docker/build
chmod ugo+x ./scripts/docker/run
chmod ugo+x ./scripts/docker/publish
chmod ugo+x ./scripts/docker/content/*.sh 2> /dev/null
chmod ugo+x ./scripts/git/setup
chmod ugo+x ./scripts/git/*.sh
chmod ugo+x ./scripts/iothub/*.sh

git update-index --chmod=+x ./scripts/build
git update-index --chmod=+x ./scripts/compile
git update-index --chmod=+x ./scripts/run
git update-index --chmod=+x ./scripts/clean-up
git update-index --chmod=+x ./scripts/env-vars-check
git update-index --chmod=+x ./scripts/env-vars-setup
git update-index --chmod=+x ./scripts/travis
git update-index --chmod=+x ./scripts/docker/build
git update-index --chmod=+x ./scripts/docker/run
git update-index --chmod=+x ./scripts/docker/publish
git update-index --chmod=+x ./scripts/docker/content/*.sh
git update-index --chmod=+x ./scripts/git/setup
git update-index --chmod=+x ./scripts/git/*.sh
git update-index --chmod=+x ./scripts/iothub/*.sh

set +e
