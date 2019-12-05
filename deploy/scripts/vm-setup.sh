#!/bin/bash -e

while [ "$#" -gt 0 ]; do
    case "$1" in
        --keyVaultUri)                  keyVaultUri="$2" ;;
        --appId)                        appId="$2" ;;
        --appSecret)                    appSecret="$2" ;;
        --imageTag)                     imageTag="$2" ;;
        --dockerUrl)                    dockerUrl="$2" ;;
        --dockerUser)                   dockerUser="$2" ;;
        --dockerPassword)               dockerPassword="$2" ;;
    esac
    shift
done

