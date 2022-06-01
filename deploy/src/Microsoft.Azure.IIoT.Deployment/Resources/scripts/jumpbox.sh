#!/usr/bin/env bash

################################################################################
#
# NOTE: Requires to be run as sudo
#
# The script does the following:
#   1. Install Azure CLI
#   2. Install kubectl
#   3. Install Helm
#   4. Install ingress-nginx/ingress-nginx Helm chart
#   5. Install jetstack/cert-manager Helm chart
#   6. Install <repo>/azure-industrial-iot Helm chart
#
################################################################################

set -e
set -x

CWD=$(pwd)

RESOURCE_GROUP=
AKS_CLUSTER=
ROLE=
LOAD_BALANCER_IP=
PUBLIC_IP_DNS_LABEL=
HELM_REPO_URL=
HELM_CHART_VERSION=
AIIOT_IMAGE_TAG=
AIIOT_IMAGE_NAMESPACE=
AIIOT_CONTAINER_REGISTRY_SERVER=
AIIOT_CONTAINER_REGISTRY_USERNAME=
AIIOT_CONTAINER_REGISTRY_PASSWORD=
AIIOT_TENANT_ID=
AIIOT_KEY_VAULT_URI=
AIIOT_SERVICES_APP_ID=
AIIOT_SERVICES_APP_SECRET=
AIIOT_SERVICES_HOSTNAME=

# ==============================================================================

while [ "$#" -gt 0 ]; do
    case "$1" in
        --resource_group)                       RESOURCE_GROUP="$2" ;;
        --aks_cluster)                          AKS_CLUSTER="$2" ;;
        --role)                                 ROLE="$2" ;;
        --load_balancer_ip)                     LOAD_BALANCER_IP="$2" ;;
        --public_ip_dns_label)                  PUBLIC_IP_DNS_LABEL="$2" ;;
        --helm_repo_url)                        HELM_REPO_URL="$2" ;;
        --helm_chart_version)                   HELM_CHART_VERSION="$2" ;;
        --aiiot_image_tag)                      AIIOT_IMAGE_TAG="$2" ;;
        --aiiot_image_namespace)                AIIOT_IMAGE_NAMESPACE="$2" ;;
        --aiiot_container_registry_server)      AIIOT_CONTAINER_REGISTRY_SERVER="$2" ;;
        --aiiot_container_registry_username)    AIIOT_CONTAINER_REGISTRY_USERNAME="$2" ;;
        --aiiot_container_registry_password)    AIIOT_CONTAINER_REGISTRY_PASSWORD="$2" ;;
        --aiiot_tenant_id)                      AIIOT_TENANT_ID="$2" ;;
        --aiiot_key_vault_uri)                  AIIOT_KEY_VAULT_URI="$2" ;;
        --aiiot_services_app_id)                AIIOT_SERVICES_APP_ID="$2" ;;
        --aiiot_services_app_secret)            AIIOT_SERVICES_APP_SECRET="$2" ;;
        --aiiot_services_hostname)              AIIOT_SERVICES_HOSTNAME="$2" ;;
    esac
    shift
done

# ==============================================================================

if [[ -z "$RESOURCE_GROUP" ]]; then
    echo "Parameter is empty or missing: resource_group"
    exit 1
fi

if [[ -z "$AKS_CLUSTER" ]]; then
    echo "Parameter is empty or missing: aks_cluster"
    exit 1
fi

if [[ -z "$ROLE" ]]; then
    echo "Parameter is empty or missing: role"
    exit 1
fi

if [[ -z "$LOAD_BALANCER_IP" ]]; then
    echo "Parameter is empty or missing: load_balancer_ip"
    exit 1
fi

if [[ -z "$PUBLIC_IP_DNS_LABEL" ]]; then
    echo "Parameter is empty or missing: public_ip_dns_label"
    exit 1
fi

if [[ -z "$HELM_REPO_URL" ]]; then
    echo "Parameter is empty or missing: helm_repo_url"
    exit 1
fi

if [[ -z "$HELM_CHART_VERSION" ]]; then
    echo "Parameter is empty or missing: helm_chart_version"
    exit 1
fi

if [[ -z "$AIIOT_IMAGE_TAG" ]]; then
    echo "Parameter is empty or missing: aiiot_image_tag"
    exit 1
fi

if [[ -z "$AIIOT_CONTAINER_REGISTRY_SERVER" ]]; then
    echo "Parameter is empty or missing: aiiot_container_registry_server"
    exit 1
fi

if [ "$AIIOT_CONTAINER_REGISTRY_SERVER" != "mcr.microsoft.com" ]; then
    echo "Private registry specified. Checking for username and password.."
    is_private_repo=true

    if [[ -z "$AIIOT_CONTAINER_REGISTRY_USERNAME" ]]; then
        echo "Parameter is empty or missing: aiiot_container_registry_username"
        exit 1
    fi

    if [[ -z "$AIIOT_CONTAINER_REGISTRY_PASSWORD" ]]; then
        echo "Parameter is empty or missing: aiiot_container_registry_password"
        exit 1
    fi
fi

if [[ -z "$AIIOT_TENANT_ID" ]]; then
    echo "Parameter is empty or missing: aiiot_tenant_id"
    exit 1
fi

if [[ -z "$AIIOT_KEY_VAULT_URI" ]]; then
    echo "Parameter is empty or missing: aiiot_key_vault_uri"
    exit 1
fi

if [[ -z "$AIIOT_SERVICES_APP_ID" ]]; then
    echo "Parameter is empty or missing: aiiot_services_app_id"
    exit 1
fi

if [[ -z "$AIIOT_SERVICES_APP_SECRET" ]]; then
    echo "Parameter is empty or missing: aiiot_services_app_secret"
    exit 1
fi

if [[ -z "$AIIOT_SERVICES_HOSTNAME" ]]; then
    echo "Parameter is empty or missing: aiiot_services_hostname"
    exit 1
fi

# Go to home.
cd ~

################################################################################
# Wait until dpkg locks are released
lockPath='/var/lib/dpkg/lock'
lockCount=$(lsof $lockPath | wc -l)

n=0
iterations=20
while [[ $n -lt $iterations ]] && [[ $lockCount -gt 0 ]]
do
    echo "Wating for 15 seconds before checking the lock again: $lockPath"
    sleep 15

    lockCount=$(lsof $lockPath | wc -l)

    n=$[$n+1]
done

if [[ $n -eq $iterations ]]; then
    echo "Faulire: $lockPath was not released"
    exit 1
fi

lockPath='/var/lib/dpkg/lock-frontend'
lockCount=$(lsof $lockPath | wc -l)

n=0
iterations=20
while [[ $n -lt $iterations ]] && [[ $lockCount -gt 0 ]]
do
    echo "Wating for 15 seconds before checking the lock again: $lockPath"
    sleep 15

    lockCount=$(lsof $lockPath | wc -l)

    n=$[$n+1]
done

if [[ $n -eq $iterations ]]; then
    echo "Faulire: $lockPath was not released"
    exit 1
fi

################################################################################
# Install Azure CLI with apt
wget "https://aka.ms/InstallAzureCLIDeb" -O deb_install.sh
chmod +x deb_install.sh

# ./deb_install.sh will fail if run without sudo
# We need to wrap ./deb_install.sh in retry loop because it sometimes fails to
# acquire /var/lib/dpkg/lock-frontend lock.
n=0
iterations=20
until [[ $n -ge $iterations ]]
do
    ./deb_install.sh && break
    n=$[$n+1]

    echo "Trying to install Azure CLI again in 15 seconds"
    sleep 15
done

if [[ $n -eq $iterations ]]; then
    echo "Failed to install Azure CLI"
    exit 1
fi

# Install kubectl
az aks install-cli

# Install Helm
az acr helm install-cli --client-version "3.7.1" -y

################################################################################
# Login to az using manaed identity
az login --identity

# Get AKS credentials
if [[ "$ROLE" -eq "AzureKubernetesServiceClusterAdminRole" ]]; then
    az aks get-credentials --resource-group $RESOURCE_GROUP --name $AKS_CLUSTER --admin
else
    az aks get-credentials --resource-group $RESOURCE_GROUP --name $AKS_CLUSTER
fi

# Configure omsagent
kubectl apply -f "$CWD/04_oms_agent_configmap.yaml"

# Add Helm repos
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo add jetstack https://charts.jetstack.io
helm repo add aiiot $HELM_REPO_URL
helm repo update

# Create ingress-nginx namespace
kubectl create namespace ingress-nginx

# Install ingress-nginx/ingress-nginx Helm chart
helm install --atomic ingress-nginx ingress-nginx/ingress-nginx --namespace ingress-nginx --version 4.0.19 --timeout 30m0s \
    --set controller.replicaCount=2 \
    --set controller.service.loadBalancerIP=$LOAD_BALANCER_IP \
    --set controller.service.annotations."service\.beta\.kubernetes\.io\/azure-dns-label-name"=$PUBLIC_IP_DNS_LABEL \
    --set controller.config.compute-full-forwarded-for='true' \
    --set controller.config.use-forwarded-headers='true' \
    --set controller.config.proxy-buffer-size='"32k"' \
    --set controller.config.client-header-buffer-size='"32k"' \
    --set controller.metrics.enabled=true \
    --set defaultBackend.enabled=true

# Create cert-manager namespace
kubectl create namespace cert-manager

# Install jetstack/cert-manager Helm chart
helm install --atomic cert-manager jetstack/cert-manager --namespace cert-manager --version v1.8.0 --timeout 30m0s \
    --set installCRDs=true

# Create Let's Encrypt ClusterIssuer
n=0
iterations=20
until [[ $n -ge $iterations ]]
do
    kubectl apply -f "$CWD/90_letsencrypt_cluster_issuer.yaml" && break
    n=$[$n+1]

    echo "Trying to create Let's Encrypt ClusterIssuer again in 15 seconds"
    sleep 15
done

if [[ $n -eq $iterations ]]; then
    echo "Failed to create Let's Encrypt ClusterIssuer"
    exit 1
fi

# Create azure-industrial-iot namespace
kubectl create namespace azure-industrial-iot

# Create secrets if private registry
if [ "$is_private_repo" = true ] ; then
    echo 'Need to additionally create secrets for the container registry..'
    kubectl create secret docker-registry $AIIOT_CONTAINER_REGISTRY_USERNAME --docker-server=$AIIOT_CONTAINER_REGISTRY_SERVER --docker-username=$AIIOT_CONTAINER_REGISTRY_USERNAME --docker-password=$AIIOT_CONTAINER_REGISTRY_PASSWORD --namespace azure-industrial-iot

    pcs_server="PCS_DOCKER_SERVER"
    pcs_user="PCS_DOCKER_USER"
    pcs_pwd="PCS_DOCKER_PASSWORD"
    pcs_namespace="PCS_IMAGES_NAMESPACE"
    pcs_tag="PCS_IMAGES_TAG"
    iiotsvcs=(publisher registry twin)
    namestr="--set deployment.microServices.SERVICE_NAME.extraEnv[IDX].name=PCS_KEY "
    valuestr="--set deployment.microServices.SERVICE_NAME.extraEnv[IDX].value=PCS_VAL "
    setsvc=""
    declare -A envvar=( [$pcs_server]=$AIIOT_CONTAINER_REGISTRY_SERVER [$pcs_user]=$AIIOT_CONTAINER_REGISTRY_USERNAME [$pcs_pwd]=$AIIOT_CONTAINER_REGISTRY_PASSWORD [$pcs_tag]=$AIIOT_IMAGE_TAG)
    registryserver="$AIIOT_CONTAINER_REGISTRY_SERVER"
    if [[ -n "$AIIOT_IMAGE_NAMESPACE" ]] ; then
        registryserver="$registryserver/$AIIOT_IMAGE_NAMESPACE"
        envvar+=( [$pcs_namespace]=$AIIOT_IMAGE_NAMESPACE )
    fi
    for svc in ${iiotsvcs[@]}; do
      idx=0
      for key in ${!envvar[@]}; do
        tnamestr="${namestr/SERVICE_NAME/$svc}"
        tnamestr="${tnamestr/PCS_KEY/${key}}"
        tnamestr="${tnamestr/IDX/$idx}"
        setsvc+=$tnamestr
        tvalstr="${valuestr/SERVICE_NAME/$svc}"
        tvalstr="${tvalstr/PCS_VAL/${envvar[${key}]}}"
        tvalstr="${tvalstr/IDX/$idx}"
        setsvc+=$tvalstr
        idx=$[$idx +1]
      done
    done
    echo $setsvc

    # Install aiiot/azure-industrial-iot Helm chart
    helm install --atomic azure-industrial-iot aiiot/azure-industrial-iot --namespace azure-industrial-iot --version $HELM_CHART_VERSION --timeout 30m0s \
        --set image.tag=$AIIOT_IMAGE_TAG \
        --set image.registry="$registryserver" \
        --set image.pullSecrets[0].name=$AIIOT_CONTAINER_REGISTRY_USERNAME \
        --set loadConfFromKeyVault=true \
        --set azure.tenantId=$AIIOT_TENANT_ID \
        --set azure.keyVault.uri=$AIIOT_KEY_VAULT_URI \
        --set azure.auth.servicesApp.appId=$AIIOT_SERVICES_APP_ID \
        --set azure.auth.servicesApp.secret=$AIIOT_SERVICES_APP_SECRET \
        --set externalServiceUrl="https://$AIIOT_SERVICES_HOSTNAME" \
        --set deployment.microServices.engineeringTool.enabled=true \
        --set deployment.ingress.enabled=true \
        --set deployment.ingress.annotations."kubernetes\.io\/ingress\.class"=nginx \
        --set deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/affinity"=cookie \
        --set deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/session-cookie-name"=affinity \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/session-cookie-expires"=14400 \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/session-cookie-max-age"=14400 \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/proxy-read-timeout"=3600 \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/proxy-send-timeout"=3600 \
        --set deployment.ingress.annotations."cert-manager\.io\/cluster-issuer"=letsencrypt-prod \
        --set deployment.ingress.tls[0].hosts[0]=$AIIOT_SERVICES_HOSTNAME \
        --set deployment.ingress.tls[0].secretName=tls-secret \
        --set deployment.ingress.hostName=$AIIOT_SERVICES_HOSTNAME \
        $setsvc
else
    # Install aiiot/azure-industrial-iot Helm chart
    helm install --atomic azure-industrial-iot aiiot/azure-industrial-iot --namespace azure-industrial-iot --version $HELM_CHART_VERSION --timeout 30m0s \
        --set image.tag=$AIIOT_IMAGE_TAG \
        --set loadConfFromKeyVault=true \
        --set azure.tenantId=$AIIOT_TENANT_ID \
        --set azure.keyVault.uri=$AIIOT_KEY_VAULT_URI \
        --set azure.auth.servicesApp.appId=$AIIOT_SERVICES_APP_ID \
        --set azure.auth.servicesApp.secret=$AIIOT_SERVICES_APP_SECRET \
        --set externalServiceUrl="https://$AIIOT_SERVICES_HOSTNAME" \
        --set deployment.microServices.engineeringTool.enabled=true \
        --set deployment.ingress.enabled=true \
        --set deployment.ingress.annotations."kubernetes\.io\/ingress\.class"=nginx \
        --set deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/affinity"=cookie \
        --set deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/session-cookie-name"=affinity \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/session-cookie-expires"=14400 \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/session-cookie-max-age"=14400 \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/proxy-read-timeout"=3600 \
        --set-string deployment.ingress.annotations."nginx\.ingress\.kubernetes\.io\/proxy-send-timeout"=3600 \
        --set deployment.ingress.annotations."cert-manager\.io\/cluster-issuer"=letsencrypt-prod \
        --set deployment.ingress.tls[0].hosts[0]=$AIIOT_SERVICES_HOSTNAME \
        --set deployment.ingress.tls[0].secretName=tls-secret \
        --set deployment.ingress.hostName=$AIIOT_SERVICES_HOSTNAME
fi
echo "Done"
