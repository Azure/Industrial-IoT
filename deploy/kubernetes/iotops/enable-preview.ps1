

az feature register --name PrivatePreview2507 --namespace Microsoft.IoTOperations --subscription 53d910a7-f1f8-4b7a-8ee0-6e6b67bddd82
az extension add --source azure_iot_ops-1.8.0a1-py3-none-any.whl --upgrade --allow-preview --yes