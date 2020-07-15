#!/usr/bin/env bash

################################################################################
#
# NOTE: Requires to be run as sudo
#
# The script does the following:
#   1. Install Azure CLI
#   2. Install kubectl
#   3. Install Helm
#   4. Install stable/nginx-ingress Helm chart
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
AIIOT_TENANT_ID=
AIIOT_KEY_VAULT_URI=
AIIOT_SERVICES_APP_ID=
AIIOT_SERVICES_APP_SECRET=
AIIOT_SERVICES_HOSTNAME=

# ==============================================================================

while [ "$#" -gt 0 ]; do
    case "$1" in
        --resource_group)               RESOURCE_GROUP="$2" ;;
        --aks_cluster)                  AKS_CLUSTER="$2" ;;
        --role)                         ROLE="$2" ;;
        --load_balancer_ip)             LOAD_BALANCER_IP="$2" ;;
        --public_ip_dns_label)          PUBLIC_IP_DNS_LABEL="$2" ;;
        --helm_repo_url)                HELM_REPO_URL="$2" ;;
        --helm_chart_version)           HELM_CHART_VERSION="$2" ;;
        --aiiot_image_tag)              AIIOT_IMAGE_TAG="$2" ;;
        --aiiot_tenant_id)              AIIOT_TENANT_ID="$2" ;;
        --aiiot_key_vault_uri)          AIIOT_KEY_VAULT_URI="$2" ;;
        --aiiot_services_app_id)        AIIOT_SERVICES_APP_ID="$2" ;;
        --aiiot_services_app_secret)    AIIOT_SERVICES_APP_SECRET="$2" ;;
        --aiiot_services_hostname)      AIIOT_SERVICES_HOSTNAME="$2" ;;
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
az acr helm install-cli --client-version "3.1.2" -y

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
helm repo add stable https://kubernetes-charts.storage.googleapis.com
helm repo add jetstack https://charts.jetstack.io
helm repo add aiiot $HELM_REPO_URL
helm repo update

# Create nginx-ingress namespace
kubectl create namespace nginx-ingress

# Install stable/nginx-ingress Helm chart
helm install --atomic nginx-ingress stable/nginx-ingress --namespace nginx-ingress --version 1.36.0 \
    --set controller.replicaCount=2 \
    --set controller.nodeSelector."beta\.kubernetes\.io\/os"=linux \
    --set controller.service.loadBalancerIP=$LOAD_BALANCER_IP \
    --set controller.service.annotations."service\.beta\.kubernetes\.io\/azure-dns-label-name"=$PUBLIC_IP_DNS_LABEL \
    --set controller.config.compute-full-forward-for='"true"' \
    --set controller.config.use-forward-headers='"true"' \
    --set controller.config.proxy-buffer-size='"32k"' \
    --set controller.config.client-header-buffer-size='"32k"' \
    --set controller.metrics.enabled=true \
    --set defaultBackend.nodeSelector."beta\.kubernetes\.io\/os"=linux

# Install the CustomResourceDefinition resources separately
kubectl apply --validate=false -f https://raw.githubusercontent.com/jetstack/cert-manager/release-0.13/deploy/manifests/00-crds.yaml

# Create cert-manager namespace and label it to disable resource validation
kubectl create namespace cert-manager
kubectl label namespace cert-manager cert-manager.io/disable-validation=true

# Install jetstack/cert-manager Helm chart
helm install --atomic cert-manager jetstack/cert-manager --namespace cert-manager --version v0.13.0

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

# Install aiiot/azure-industrial-iot Helm chart
helm install --atomic azure-industrial-iot aiiot/azure-industrial-iot --namespace azure-industrial-iot --version $HELM_CHART_VERSION \
    --set image.tag=$AIIOT_IMAGE_TAG \
    --set loadConfFromKeyVault=true \
    --set azure.tenantId=$AIIOT_TENANT_ID \
    --set azure.keyVault.uri=$AIIOT_KEY_VAULT_URI \
    --set azure.auth.servicesApp.appId=$AIIOT_SERVICES_APP_ID \
    --set azure.auth.servicesApp.secret=$AIIOT_SERVICES_APP_SECRET \
    --set externalServiceUrl="https://$AIIOT_SERVICES_HOSTNAME" \
    --set deployment.microServices.engineeringTool.enabled=true \
    --set deployment.microServices.telemetryCdmProcessor.enabled=true \
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

echo "Done"
