#!/bin/bash -ex

APP_PATH="/app"
LOGS_PATH="/logs"
ENVVARS="${APP_PATH}/.env"
DOCKERCOMPOSE="${APP_PATH}/docker-compose.yml"
CERTS="${APP_PATH}/certs"
PFX="${CERTS}/tls.pfx"
CERT="${CERTS}/tls.crt"
PKEY="${CERTS}/tls.key"
UNSAFE="false"
ADMIN=$USER
REGISTRY_PREFIX=

# ========================================================================

HOST_NAME="localhost"
PCS_LOG_LEVEL="Info"
PCS_WEBUI_AUTH_TYPE="aad"
PCS_APPLICATION_SECRET=$(cat /dev/urandom | LC_CTYPE=C tr -dc 'a-zA-Z0-9-,./;:[]\(\)_=^!~' | fold -w 64 | head -n 1)

while [ "$#" -gt 0 ]; do
    case "$1" in
        --hostname)                     HOST_NAME="$2" ;;
        --registry-prefix)              REGISTRY_PREFIX="$2" ;;
        --admin)                        ADMIN="$2" ;;
        --log-level)                    PCS_LOG_LEVEL="$2" ;;
        --unsafe)                       UNSAFE="$2" ;;
        --iothub-name)                  PCS_IOTHUBREACT_HUB_NAME="$2" ;;
        --iothub-endpoint)              PCS_IOTHUBREACT_HUB_ENDPOINT="$2" ;;
        --iothub-consumer-group)        PCS_IOTHUBREACT_HUB_CONSUMERGROUP="$2" ;;
        --iothub-connstring)            PCS_IOTHUB_CONNSTRING="$2" ;;
        --azureblob-account)            PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT="$2" ;;
        --azureblob-key)                PCS_IOTHUBREACT_AZUREBLOB_KEY="$2" ;;
        --azureblob-endpoint-suffix)    PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX="$2" ;;
        --docdb-connstring)             PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING="$2" ;;
        --ssl-certificate)              PCS_CERTIFICATE="$2" ;;
        --ssl-certificate-key)          PCS_CERTIFICATE_KEY="$2" ;;
        --auth-audience)                PCS_AUTH_AUDIENCE="$2" ;;
        --auth-type)                    PCS_WEBUI_AUTH_TYPE="$2" ;;
        --aad-appid)                    PCS_WEBUI_AUTH_AAD_APPID="$2" ;;
        --aad-tenant)                   PCS_WEBUI_AUTH_AAD_TENANT="$2" ;;
        --aad-instance)                 PCS_WEBUI_AUTH_AAD_INSTANCE="$2" ;;
        --aad-appsecret)                PCS_APPLICATION_SECRET="$2" ;;
        --release-version)              PCS_RELEASE_VERSION="$2" ;;
        --evenhub-connstring)           PCS_EVENTHUB_CONNSTRING="$2" ;;
        --eventhub-name)                PCS_EVENTHUB_NAME="$2" ;;
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
curl -L "https://github.com/docker/compose/releases/download/1.22.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# ========================================================================

PCS_AUTH_ISSUER="https://sts.windows.net/${PCS_WEBUI_AUTH_AAD_TENANT}/"

# Configure Docker registry based on host name
# ToDo: we may need to add similar parameter to AzureGermanCloud and AzureUSGovernment
config_for_azure_china() {
    set +e
    local host_name=$1
    if (echo $host_name | grep -c  "\.cn$") ; then
        # If the host name has .cn suffix, dockerhub in China will be used to avoid slow network traffic failure.
        local config_file='/etc/docker/daemon.json'
        echo "{\"registry-mirrors\": [\"https://registry.docker-cn.com\"]}" > ${config_file}
        service docker restart

        # Rewrite the AAD issuer in Azure China environment
        PCS_AUTH_ISSUER="https://sts.chinacloudapi.cn/${PCS_WEBUI_AUTH_AAD_TENANT}/"
    fi
    set -e
}

config_for_azure_china $HOST_NAME $5

# ========================================================================
# Configure SSH to not use weak HostKeys, algorithms, ciphers and MAC algorithms.
# Comment out the option if exists or ignore it.
switch_off() {
    local key=$1
    local value=$2
    local config_path=$3
    sed -i "s~#*$key\s*$value~#$key $value~g" $config_path
}

# Change existing option if found or append specified key value pair.
switch_on() {
    local key=$1
    local value=$2
    local config_path=$3
    grep -q "$key" $config_path && sed -i -e "s/$key.*/$key $value/g" $config_path || sed -i -e "\$a$key $value" $config_path
}

config_ssh() {
    local config_path="${1:-/etc/ssh/sshd_config}"
    switch_off 'HostKey' '/etc/ssh/ssh_host_dsa_key' $config_path
    switch_off 'HostKey' '/etc/ssh/ssh_host_ecdsa_key' $config_path
    switch_on 'KexAlgorithms' 'curve25519-sha256@libssh.org,diffie-hellman-group-exchange-sha256' $config_path
    switch_on 'Ciphers' 'chacha20-poly1305@openssh.com,aes256-gcm@openssh.com,aes128-gcm@openssh.com,aes256-ctr,aes192-ctr,aes128-ctr' $config_path
    switch_on 'MACs' 'hmac-sha2-512-etm@openssh.com,hmac-sha2-256-etm@openssh.com,hmac-ripemd160-etm@openssh.com,umac-128-etm@openssh.com,hmac-sha2-512,hmac-sha2-256,hmac-ripemd160,umac-128@openssh.com' $config_path
    service ssh restart
}

config_ssh

# ========================================================================

mkdir -p ${LOGS_PATH}
chmod ugo+rX ${LOGS_PATH}
mkdir -p ${APP_PATH}
chmod ugo+rX ${APP_PATH}
cp -f docker-compose.yml ${APP_PATH}/docker-compose.yml
cp -f nginx.conf ${APP_PATH}/nginx.conf
cp -f setup.sh ${APP_PATH}/setup.sh
cp -f ctrl.sh ${APP_PATH}/ctrl.sh
cd ${APP_PATH}
touch docker-compose.yml && chmod 644 docker-compose.yml
touch nginx.conf && chmod 644 nginx.conf
chmod 755 ctrl.sh
chmod 755 setup.sh

mkdir -p ${CERTS}
# Always have quotes around the pfx to preserve the formatting
echo "${PCS_CERTIFICATE}" | base64 --decode > ${PFX}
openssl pkcs12 -in ${PFX} -clcerts -nokeys -out ${CERT} -passin pass:${PCS_CERTIFICATE_KEY}
openssl pkcs12 -in ${PFX} -nocerts -nodes -out ${PKEY} -passin pass:${PCS_CERTIFICATE_KEY}
touch ${CERT} && chmod 444 ${CERT}
touch ${PKEY} && chmod 444 ${PKEY}
rm -f ${PFX}

# ========================================================================

# Environment variables
rm -f ${ENVVARS}
touch ${ENVVARS} && chmod 644 ${ENVVARS}

echo "HOST_NAME=${HOST_NAME}" >> ${ENVVARS}
echo "PCS_AUTH_HTTPSREDIRECTPORT=0" >> ${ENVVARS}
echo "PCS_AUTH_ISSUER=${PCS_AUTH_ISSUER}" >> ${ENVVARS}
echo "PCS_AUTH_AUDIENCE=${PCS_AUTH_AUDIENCE}" >> ${ENVVARS}
echo "PCS_WEBUI_AUTH_AAD_TENANT=${PCS_WEBUI_AUTH_AAD_TENANT}" >> ${ENVVARS}
echo "PCS_WEBUI_AUTH_AAD_APPID=${PCS_WEBUI_AUTH_AAD_APPID}" >> ${ENVVARS}
echo "PCS_WEBUI_AUTH_AAD_INSTANCE=${PCS_WEBUI_AUTH_AAD_INSTANCE}" >> ${ENVVARS}
echo "REACT_APP_PCS_AUTH_ISSUER=${PCS_AUTH_ISSUER}" >> ${ENVVARS}
echo "REACT_APP_PCS_AUTH_AUDIENCE=${PCS_AUTH_AUDIENCE}" >> ${ENVVARS}
echo "REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT=${PCS_WEBUI_AUTH_AAD_TENANT}" >> ${ENVVARS}
echo "REACT_APP_PCS_WEBUI_AUTH_AAD_APPID=${PCS_WEBUI_AUTH_AAD_APPID}" >> ${ENVVARS}
echo "REACT_APP_PCS_WEBUI_AUTH_AAD_INSTANCE=${PCS_WEBUI_AUTH_AAD_INSTANCE}" >> ${ENVVARS}
echo "PCS_IOTHUB_CONNSTRING=${PCS_IOTHUB_CONNSTRING}" >> ${ENVVARS}
echo "PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING=${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}" >> ${ENVVARS}
echo "PCS_TELEMETRY_DOCUMENTDB_CONNSTRING=${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}" >> ${ENVVARS}
echo "PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING=${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_ACCESS_CONNSTRING=${PCS_IOTHUB_CONNSTRING}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_HUB_NAME=${PCS_IOTHUBREACT_HUB_NAME}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_HUB_ENDPOINT=${PCS_IOTHUBREACT_HUB_ENDPOINT}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_HUB_CONSUMERGROUP=${PCS_IOTHUBREACT_HUB_CONSUMERGROUP}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT=${PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_AZUREBLOB_KEY=${PCS_IOTHUBREACT_AZUREBLOB_KEY}" >> ${ENVVARS}
echo "PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX=${PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX}" >> ${ENVVARS}
echo "PCS_ASA_DATA_AZUREBLOB_ACCOUNT=${PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT}" >> ${ENVVARS}
echo "PCS_ASA_DATA_AZUREBLOB_KEY=${PCS_IOTHUBREACT_AZUREBLOB_KEY}" >> ${ENVVARS}
echo "PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX=${PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX}" >> ${ENVVARS}
echo "PCS_EVENTHUB_CONNSTRING=${PCS_EVENTHUB_CONNSTRING}" >> ${ENVVARS}
echo "PCS_EVENTHUB_NAME=${PCS_EVENTHUB_NAME}" >> ${ENVVARS}
echo "PCS_APPLICATION_SECRET=${PCS_APPLICATION_SECRET}" >> ${ENVVARS}
echo "PCS_LOG_LEVEL=${PCS_LOG_LEVEL}" >> ${ENVVARS}
echo "PCS_RELEASE_VERSION=${PCS_RELEASE_VERSION}" >> ${ENVVARS}
echo "_HUB_CS=${PCS_IOTHUB_CONNSTRING}" >> ${ENVVARS}

if [ -z "$REGISTRY_PREFIX" ]; then
  echo -e "Deploying from default registry."
else
  echo -e "Using registry prefix ${REGISTRY_PREFIX}."
  echo "SERVICES_REPOSITORY=${REGISTRY_PREFIX}" >> ${ENVVARS}
  echo "MODULES_REPOSITORY=${REGISTRY_PREFIX}" >> ${ENVVARS}
fi

# ========================================================================

if [[ "$UNSAFE" == "true" ]]; then
  echo -e "${COL_ERR}WARNING! Starting services in UNSAFE mode!${COL_NO}"
  # Disable Auth
  # Allow cross-origin requests from anywhere
  echo "PCS_AUTH_REQUIRED=false" >> ${ENVVARS}
  echo "REACT_APP_PCS_AUTH_REQUIRED=false" >> ${ENVVARS}
  echo "PCS_CORS_WHITELIST={ 'origins': ['*'], 'methods': ['*'], 'headers': ['*'] }" >> ${ENVVARS}
  echo "REACT_APP_PCS_CORS_WHITELIST={ 'origins': ['*'], 'methods': ['*'], 'headers': ['*'] }" >> ${ENVVARS}
else
  echo "PCS_AUTH_REQUIRED=true" >> ${ENVVARS}
  echo "REACT_APP_PCS_AUTH_REQUIRED=true" >> ${ENVVARS}
  echo "PCS_CORS_WHITELIST=" >> ${ENVVARS}
  echo "REACT_APP_PCS_CORS_WHITELIST=" >> ${ENVVARS}
fi

chown -R $ADMIN ${APP_PATH}
cd ${APP_PATH}
./ctrl.sh --start

