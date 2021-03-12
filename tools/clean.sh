#!/bin/bash

# -------------------------------------------------------------------------------
usage(){
    echo '
Usage: '"$0"'  
    --subscription, -s         Subscription to clean up. If not set uses
                               the default subscription for the account.
            -y                 Perform actual deletion.
    --help                     Shows this help.
'
    exit 1
}

args=( "$@"  )
subscription=
delete=

while [ "$#" -gt 0 ]; do
    case "$1" in
        --help)                usage ;;
        --subscription)        subscription="$2" ;;
        -y)                    delete=1 ;;
        -s)                    subscription="$2" ;;
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

# remove groups not marked for keeping
for group in $(az group list --query "[?tags.DoNotDelete!='true'].name" -o tsv | tr -d '\r'); do
    if [[ $group = MC_* ]] ; then
        echo "skipping $group ..."
    elif [[ -z "$delete" ]]; then 
        echo "Would have deleted resourcegroup $group ..."
    else
        echo "deleting resourcegroup $group ..."
        az group delete -g $group -y
    fi
done

# purge deleted keyvault
#for vault in $(az keyvault list-deleted -o tsv | tr -d '\r'); do
#    echo "deleting keyvault $vault ..."
#    az keyvault purge --name $vault
#done

