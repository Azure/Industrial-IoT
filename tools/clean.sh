#!/bin/bash

# -------------------------------------------------------------------------------
usage(){
    echo '
Usage: '"$0"'
    1. Run first on the subscription using -m flag to mark groups for deletion.
    2. Ask team to remove the tag for items they want to keep.
    3. When done, run again with -y flag to remove all remaining tagged groups.

    --subscription, -s         Subscription to clean up. If not set uses
                               the default subscription for the account.
    --mark, -m                 Mark groups not tagged with Production for
                               deletion.
            -y                 Perform actual deletion.
    
    --prefix                   Match and delete everything with prefix.

    --kv-purge                 Purge all soft deleted key vaults in subscription.

    --help                     Shows this help.

'
    exit 1
}

args=( "$@"  )
subscription=
delete=
prefix=
mark=
purgekv=

while [ "$#" -gt 0 ]; do
    case "$1" in
        --subscription|-s)     subscription="$2" ; shift ;;
        --kv-purge)            purgekv=1 ;;
        --mark|-m)             mark=1 ;;
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
    if ! az account set -s $subscription ; then
		echo "Failed to change subscription!"
		exit 1
	fi
fi

# -------------------------------------------------------------------------------
if [[ -n "$prefix" ]] ; then
    # select groups with prefix that are not production
    groups=$(az group list \
        --query "[?starts_with(name, '$prefix') && tags.Production==null].name" \
        -o tsv | tr -d '\r')
elif [[ -n "$mark" ]]; then
    # select groups to mark for deletion
    groups=$(az group list --query "[?tags.Production==null].name" \
        -o tsv | tr -d '\r')
else
    # select groups marked for deletion
    groups=$(az group list --query "[?tags.ReadyToDelete].name" \
        -o tsv | tr -d '\r')
fi

# remove groups 
for group in $groups; do
    if [[ -n "$mark" ]]; then
        if [[ $group = MC_* ]] ; then
			echo "skipping $group ..." > /dev/null
        else
			echo "Marking group $group as ready to delete ..."
			az group update -g $group --set tags.ReadyToDelete='true' > /dev/null
		fi
    else
        if [[ $group = MC_* ]] ; then
            echo "skipping $group ..." > /dev/null
        elif [[ -z "$delete" ]]; then 
            echo "$group up for deletion."
        else
            echo "Deleting resourcegroup $group ..."
            az group delete -g $group -y --no-wait
        fi
    fi
done

# -------------------------------------------------------------------------------
if [[ -n "$purgekv" ]]; then
    # purge deleted keyvault
    for vault in $(az keyvault list-deleted -o tsv | tr -d '\r'); do
        echo "deleting keyvault $vault ..."
        az keyvault purge --name $vault
    done
fi
# -------------------------------------------------------------------------------
