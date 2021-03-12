#!/bin/bash

# -------------------------------------------------------------------------------

usage(){
    echo '
Usage: '"$0"' 
    --keyvault                 The keyvault to get secrets from. 
         --subscription        Subscription to use in which the keyvault
                               was created.  Default subscription is used 
                               if not provided.
    --help                     Shows this help.
'
    exit 1
}

keyVaultName=
subscription=

[ $# -eq 0 ] && usage
while [ "$#" -gt 0 ]; do
    case "$1" in
        --keyvault)        keyVaultName="$2" ;;
        --subscription)    subscription="$2" ;;
        --help)            usage ;;
    esac
    shift
done

if [[ -z "$keyVaultName" ]] ; then
    echo "Must provide name of keyvault!"
    usage
fi

if [[ -n "$subscription" ]] ; then
    az account set --subscription $subscription
fi

# try get access to the keyvault
rg=$(az keyvault show --name $keyVaultName \
    --query resourceGroup -o tsv | tr -d '\r')
if [[ -n "$rg" ]] ; then
    rgid=$(az group show --name $rg --query id -o tsv | tr -d '\r')
    user=$(az ad signed-in-user show --query "objectId" -o tsv | tr -d '\r')
    if [[ -n "$user" ]] && [[ -n "$rgid" ]] ; then
        name=$(az role assignment create --assignee-object-id $user \
            --role b86a8fe4-44ce-4948-aee5-eccb2c155cd7 --scope $rgid \
            --query principalName -o tsv | tr -d '\r')
        echo "Assigned secret officer role to $name ($user) scoped to '$rg'..."
    fi 
fi

# Wait for role assignment to complete
while ! secrets=$(az keyvault secret list --vault-name $keyVaultName \
    --query "[].id" -o tsv | tr -d '\r')
do 
    echo "... retry in 5 seconds..."
    sleep 5s; 
done

echo "Dumping contents of keyvault:"
echo ""
for id in $secrets; do 
    IFS=$'\n' 
    kv=($(az keyvault secret show --id $id \
        --query "[name, value]" -o tsv | tr -d '\r'))
    unset IFS
    echo "${kv[0]}=${kv[1]}"
done

