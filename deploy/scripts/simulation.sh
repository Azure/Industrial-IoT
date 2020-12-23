#!/bin/bash -ex

ADMIN=$USER
IMAGES_NAMESPACE=
IMAGES_TAG=
DOCKER_SERVER=
DOCKER_USER=
DOCKER_PASSWORD=
DOCKER_COMPOSE_FILE="default.yml"
DEBIAN_FRONTEND=noninteractive

APP_PATH="/app"
ENVVARS="${APP_PATH}/.env"

# ========================================================================

while [ "$#" -gt 0 ]; do
    case "$1" in
        --admin)                ADMIN="$2" ;;
        --name)                 DOCKER_COMPOSE_FILE="$2.yml" ;;
        --imagesNamespace)      IMAGES_NAMESPACE="$2" ;;
        --imagesTag)            IMAGES_TAG="$2" ;;
        --dockerServer)         DOCKER_SERVER="$2" ;;
        --dockerUser)           DOCKER_USER="$2" ;;
        --dockerPassword)       DOCKER_PASSWORD="$2" ;;
    esac
    shift
done

# ========================================================================

apt-get update
apt-get remove -y docker docker-engine docker.io
apt-get autoremove -y
apt-get install -y --no-install-recommends apt-transport-https ca-certificates curl software-properties-common openssl
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
apt-key fingerprint 0EBFCD88
add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
apt-get update
apt-get install -y --no-install-recommends docker-ce
usermod -aG docker $USER
usermod -aG docker $ADMIN
curl -L "https://github.com/docker/compose/releases/download/1.25.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# ========================================================================

mkdir -p ${APP_PATH}
chmod ugo+rX ${APP_PATH}
cp -f ${DOCKER_COMPOSE_FILE} ${APP_PATH}/docker-compose.yml
cp -f simulation.sh ${APP_PATH}/simulation.sh
cd ${APP_PATH}
touch docker-compose.yml && chmod 644 docker-compose.yml
chmod 755 simulation.sh

# ========================================================================

if [ -z "$DOCKER_SERVER" ]; then
    DOCKER_SERVER=mcr.microsoft.com
fi
if [ -z "$DOCKER_USER" ]; then
    echo -e "Deploying from public ${DOCKER_SERVER}."
else
    echo -e "Logging into private registry at ${DOCKER_SERVER}."
    docker login -u $DOCKER_USER -p $DOCKER_PASSWORD $DOCKER_SERVER
fi

chown -R $ADMIN ${APP_PATH}
cd ${APP_PATH}
rm -f ${ENVVARS}
if [ -z "$IMAGES_NAMESPACE" ]; then
    echo "REPOSITORY=${DOCKER_SERVER}" >> ${ENVVARS}
else
    echo "REPOSITORY=${DOCKER_SERVER}/${IMAGES_NAMESPACE}" >> ${ENVVARS}
fi
if [ -z "$IMAGES_TAG" ]; then
    echo -e "Using latest version of images as defined in compose file."
else
    echo "VERSION=${IMAGES_TAG}" >> ${ENVVARS}
fi
touch ${ENVVARS} && chmod 644 ${ENVVARS}
docker-compose pull
docker-compose up -d
if [ $? -eq 0 ]
then
    echo "Simulation started."
    exit 0
else
    echo "Failure: Cannot start simulation." >&2
    exit 1
fi
