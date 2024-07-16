<#
 .SYNOPSIS
    Provision an Eflow IoT edge (must have been setup)

 .DESCRIPTION
    Provisions an eflow iot edge on the device. This script will
    provision the eflow as a IoT Edge device in IoT Hub.
#>

param(
    [Parameter(Mandatory)]
    [string] $IotHubName = "iothub-lvmdwa",
    [string] $TenantId = "6e54c408-5edd-4f87-b3bb-360788b7ca18",
    [string] $SubscriptionId
)

$ErrorActionPreference = "Stop"
$path = Split-Path $script:MyInvocation.MyCommand.Path

if (![string]::IsNullOrWhiteSpace($SubscriptionId))
{
    Update-AzConfig -DefaultSubscriptionForLogin $SubscriptionId
}
$azargs = @{}
if (![string]::IsNullOrWhiteSpace($TenantId))
{
    $azargs.Add("-Tenant", $TenantId)
}
Connect-AzAccount @azargs

$hub = Get-AzIoTHub | Where-Object Name -eq $IotHubName
if (!$hub) {
    throw "IoT Hub $IotHubName not found."
}
$device = $hub | Get-AzIotHubDevice -DeviceId eflow
if (!$device.EdgeEnabled) {
    $hub | Remove-AzIotHubDevice -DeviceId eflow
    $device = $null
}
if (!$device) {
    $device = $hub | Add-AzIotHubDevice -DeviceId eflow `
        -AuthMethod shared_private_key -Status Enabled -EdgeEnabled
}
$devConnString = $hub | Get-AzIotHubDeviceConnectionString -DeviceId $device.Id

Write-Host "Provision eflow with connection string."
Provision-EflowVm ManualConnectionString -devConnString $devConnString
Write-Host "Eflow provisioned."

Start-EflowVm
Verify-EflowVm

Write-Host "Deploy modules..."
$modulesContent = Get-Content -Path $(Join-Path $path "eflow-run.json") `
    -Raw | ConvertFrom-Json -AsHashtable
$hub | Set-AzIotHubEdgeModule -DeviceId $device.Id -ModulesContent $modulesContent
Write-Host "Eflow running."
