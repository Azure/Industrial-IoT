
<#
This script enables the Azure IoT Operations Private Preview feature and installs 
the necessary extension.
It requires the Azure CLI and the Azure IoT Operations extension to be installed.
#>
param (
    [string] $SubscriptionId
)

az feature register --name PrivatePreview2509 --namespace Microsoft.IoTOperations `
    --subscription $SubscriptionId `
    --only-show-errors

Write-Host "Azure IoT Operations Private Preview enabled." -ForegroundColor Green
Write-Host "You must use '--ops-extension preview' parameter for ./cluster.setup.ps1 script" `
    -ForegroundColor Yellow
Write-Host "Otherwise this script must be run again." -ForegroundColor Yellow