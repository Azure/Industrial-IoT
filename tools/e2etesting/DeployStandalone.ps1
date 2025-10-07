Param(
    [string]
    $ResourceGroupName,
    [Guid]
    $TenantId,
    [string]
    $Region = "EastUS",
    [string]
    $ServicePrincipalId,
    [string]
    $OpcPlcAddresses
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

if (!$Region) {
    Write-Error "Region not set."
}

if (!$ServicePrincipalId) {
    Write-Warning "ServicePrincipalId not set, cannot update permissions."
}

## Login if required

Write-Host "Getting Azure Context..."
$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

if (!$TenantId) {
    $TenantId = $context.Tenant.Id
    Write-Host "Using TenantId $($TenantId)."
}

## Check if resource group exists

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue

if (!$resourceGroup) {
    Write-Host "Creating Resource Group $($ResourceGroupName) in $($Region)..."
    $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName -Location $Region
}

Write-Host "Resource Group: $($resourceGroup.ResourceGroupName)"

## Determine suffix for testing resources

if (!$resourceGroup.Tags) {
    $resourceGroup.Tags = @{}
}

$testSuffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999

    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix" = $testSuffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

Write-Host "Resources Suffix: $($testSuffix)"

$iotHubName = "e2etesting-iotHub-$($testSuffix)"
$keyVaultName = "e2etestingkeyVault$($testSuffix)"

Write-Host "IoT Hub: $($iotHubName)"
Write-Host "Key Vault: $($keyVaultName)"

## Ensure IoT Hub
$iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $iotHubName -ErrorAction SilentlyContinue

if (!$iotHub) {
    Write-Host "Creating IoT Hub $($iotHubName)..."
    $iotHub = New-AzIotHub -ResourceGroupName $ResourceGroupName -Name $iotHubName -SkuName S1 -Units 1 -Location $resourceGroup.Location
}

# Ensure Event Hub additional consumer group for tests

$cgName = "TestConsumer"
$iotHubCg = Get-AzIotHubEventHubConsumerGroup -ResourceGroupName $ResourceGroupName -Name $iotHubName | Where-Object Name -eq $cgName

if (!$iotHubCg) {
    Write-Host "Creating IoT Hub Event Hub Consumer Group $($cgName)..."
    $iotHubCg = Add-AzIotHubEventHubConsumerGroup -ResourceGroupName $ResourceGroupName -Name $iotHubName -EventHubConsumerGroupName $cgName
}

## Ensure KeyVault

$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $keyVaultName -ErrorAction SilentlyContinue

if (!$keyVault) {
    Write-Host "Creating Key Vault $($keyVaultName)"
    $keyVault = New-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $keyVaultName -Location $resourceGroup.Location -DisableRbacAuthorization
}
else {
    $keyVault | Update-AzKeyVault -DisableRbacAuthorization
}

if ($ServicePrincipalId) {
    Write-Host "Setting Key Vault Permissions for Service Principal $($ServicePrincipalId)..."
    Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -ServicePrincipalName $ServicePrincipalId -PermissionsToSecrets get,list,set
}

$connectionString = Get-AzIotHubConnectionString $ResourceGroupName -Name $iothub.Name -KeyName "iothubowner"
$SubscriptionId = $context.Subscription.Id

Write-Host "Adding/Updating KeyVault-Secret 'PCS-IOTHUB-CONNSTRING' with value '***'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $connectionString.PrimaryConnectionString -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-IOTHUB-CONNSTRING' -SecretValue $secret | Out-Null
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $TenantId -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-AUTH-TENANT' -SecretValue $secret | Out-Null
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $SubscriptionId -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-SUBSCRIPTION-ID' -SecretValue $secret | Out-Null
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $ResourceGroupName -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-RESOURCE-GROUP' -SecretValue $secret | Out-Null

Write-Host "Deployment finished."