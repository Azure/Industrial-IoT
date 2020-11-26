Param(
    [string]
    $ResourceGroupName,
    [Guid]
    $TenantId,
    [String]
    $Location = "EastUS"
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

if (!$Location) {
    Write-Error "Location not set."
}

## Login if required

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Check if resource group exists

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue

if (!$resourceGroup) {
    Write-Host "Creating Resource Group $($ResourceGroupName) in $($Location)..."
    $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName -Location $Location
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

## Ensure KeyVault

$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $keyVaultName -ErrorAction SilentlyContinue

if (!$keyVault) {
    Write-Host "Creating Key Vault $($keyVaultName)"
    $keyVault = New-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $keyVaultName -Location $resourceGroup.Location
}

Write-Host "Deployment finished."