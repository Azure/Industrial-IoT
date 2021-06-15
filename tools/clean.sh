#!/bin/bash

# -------------------------------------------------------------------------------
usage(){
    echo '
Usage: '"$0"'  
    --subscription, -s         Subscription to clean up. If not set uses
                               the default subscription for the account.
            -y                 Perform actual deletion.
    --prefix                   Match everything with prefix
    --help                     Shows this help.
'
    exit 1
}

args=( "$@"  )
subscription=
delete=
prefix=

while [ "$#" -gt 0 ]; do
    case "$1" in
        --subscription|-s)     subscription="$2" ; shift ;;
        --prefix)              prefix="$2" ; shift ;;
        -y)                    delete=1 ;;
        *)                     usage ;;
    esac
    shift
done

# -------------------------------------------------------------------------------
if ! az account show > /dev/null 2>&1 ; then
    az login
fi
if [[ -n "$subscription" ]]; then 
    az account set -s $subscription
fi

if [[ -n "$prefix" ]] ; then
    # remove groups with prefix
    groups=$(az group list --query "[?starts_with(name, '$prefix')].name" -o tsv | tr -d '\r')
else
    # remove groups not marked for keeping
    groups=$(az group list --query "[?tags.DoNotDelete!='true'].name" -o tsv | tr -d '\r')
fi

# remove groups 
for group in $groups; do
    if [[ $group = MC_* ]] ; then
        echo "skipping $group ..."
    elif [[ -z "$delete" ]]; then 
        echo "Would have deleted resourcegroup $group ..."
    else
        echo "Deleting resourcegroup $group ..."
        az group delete -g $group -y
    fi
done

# purge deleted keyvault
#for vault in $(az keyvault list-deleted -o tsv | tr -d '\r'); do
#    echo "deleting keyvault $vault ..."
#    az keyvault purge --name $vault
#done

