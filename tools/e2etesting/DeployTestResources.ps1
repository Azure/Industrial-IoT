Param(
    $ResourceGroupName,
    $IoTHubName,
    $TenantId
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

## Pre-Checks ##

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName is empty."
    return
}

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Ensure Resource Group ##

$resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName

if (!$resourceGroup) {
    throw "ResourceGroup $($ResourceGroupName) does not exist!"
}

$suffix = $resourceGroup.Tags["TestingResourcesSuffix"]
if ([String]::IsNullOrWhiteSpace($suffix)) {
    $suffix = (Get-Random -Minimum 10000 -Maximum 99999)
    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix"=$suffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

Write-Host "Using suffix $($suffix) for test-related resources."

$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName
if ($keyVault.Count -ne 1) {
    throw "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."
}

## Log parameters ##

Write-Host "=============================================="
Write-Host "Test suffix:  $($suffix)"
Write-Host "=============================================="
Write-Host "Subscription Id: $($context.Subscription.Id)"
Write-Host "Subscription Name: $($context.Subscription.Name)"
Write-Host "Tenant Id: $($context.Tenant.Id)"
Write-Host "Resource Group: $($ResourceGroupName)"
Write-Host "=============================================="
Write-Host "KeyVault: $($keyVault.VaultName)"
Write-Host "IoTHub: $($IoTHubName)"
Write-Host "=============================================="

Write-Host "##vso[build.addbuildtag]$($suffix)"
Write-Host "##vso[build.addbuildtag]$($context.Subscription.Id)"
Write-Host "##vso[build.addbuildtag]$($ResourceGroupName)"
Write-Host "##vso[build.addbuildtag]$($keyVault.VaultName)"

## Check existence of IoT Hub ##

if (!$IoTHubName) {
    $iothub = Get-AzIotHub -ResourceGroupName $ResourceGroupName
    if ($iotHub.Count -eq 1) {
        $IoTHubName = $iotHub.Name
    } else {
        throw "More then 1 IoT Hub instances found in resource group $($ResourceGroupName). Please specify IoTHubName argument of this script."
    }
} else {
    $iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $IoTHubName -ErrorAction SilentlyContinue
}
if (!$iotHub) {
    throw "Could not retrieve IoTHub '$($IoTHubName)' in Resource Group '$($ResourceGroupName)'. Please make sure that it exists."
}

## Get IoT Hub EventHub-compatible Endpoint ##

$ehEndpoint = $iotHub.Properties.EventHubEndpoints["events"].Endpoint
$iotHubkey = Get-AzIotHubKey -ResourceGroupName $ResourceGroupName -Name $IoTHubName -KeyName "iothubowner"
$ehConnectionString =  "Endpoint={0};SharedAccessKeyName={1};SharedAccessKey={2};EntityPath={3}" -f $ehEndpoint,$iotHubkey.KeyName,$iotHubkey.PrimaryKey,$iotHub.Name
Write-Host "Setting KeyVault Secret 'iothub-eventhub-connectionstring' to '***'."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $ehConnectionString -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name "iothub-eventhub-connectionstring" -SecretValue $secret | Out-Null

# Ensure Event Hub additional consumer group for tests

$cgName = "TestConsumer"
$iotHubCg = Get-AzIotHubEventHubConsumerGroup -ResourceGroupName $ResourceGroupName -Name $IoTHubName | Where-Object Name -eq $cgName
if (!$iotHubCg) {
    Write-Host "Creating IoT Hub $($IoTHubName) Event Hub Consumer Group $($cgName)..."
    $iotHubCg = Add-AzIotHubEventHubConsumerGroup -ResourceGroupName $ResourceGroupName -Name $IoTHubName -EventHubConsumerGroupName $cgName
}

# NOTE: Storage account + Azure Files share `acishare` were previously created here to
# deliver PLC simulation configs into ACI containers via an Azure Files volume mount.
# That mount required a storage account key (Azure Files SMB mount on ACI does not
# support managed identity per docs/Limitations), and the key was the last local-auth
# surface in the test resource group. The PLC config is now delivered inline as a
# secure environment variable on the container group (see TestHelper.cs
# CreatePlcContainerGroupAsync). No storage account is created here anymore.
