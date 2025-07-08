
<#
This script enables the Azure IoT Operations Private Preview feature and installs 
the necessary extension.
It requires the Azure CLI and the Azure IoT Operations extension to be installed.
#>
param (
    [string] $SubscriptionId = "53d910a7-f1f8-4b7a-8ee0-6e6b67bddd82"
)

az feature register --name PrivatePreview2507 --namespace Microsoft.IoTOperations `
    --subscription $SubscriptionId `
    --only-show-errors
az extension add --source azure_iot_ops-1.8.0a1-py3-none-any.whl `
    --upgrade --allow-preview --yes `
    --only-show-errors

Write-Host "Azure IoT Operations Private Preview enabled." -ForegroundColor Green
Write-Host "You must use '--ops-extension installed' parameter for ./cluster.setup.ps1 script" `
    -ForegroundColor Yellow
Write-Host "Otherwise this script must be run again." -ForegroundColor Yellow