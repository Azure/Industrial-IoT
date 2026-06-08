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
    $keyVault = New-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $keyVaultName -Location $resourceGroup.Location -EnableRbacAuthorization
}
else {
    $keyVault | Update-AzKeyVault -EnableRbacAuthorization
}

if ($ServicePrincipalId) {
    Write-Host "Granting 'Key Vault Secrets Officer' role on Key Vault to Service Principal $($ServicePrincipalId)..."
    # $ServicePrincipalId may be either an App (client) ID or an SP Object ID
    # depending on caller. The ADO `addSpnToEnvironment: true` task exposes the
    # App ID via $env:servicePrincipalId, so resolve to the SP Object ID before
    # calling New-AzRoleAssignment -ObjectId.
    $spObjectId = $ServicePrincipalId
    $sp = Get-AzADServicePrincipal -ApplicationId $ServicePrincipalId -ErrorAction SilentlyContinue
    if (!$sp) {
        $sp = Get-AzADServicePrincipal -ObjectId $ServicePrincipalId -ErrorAction SilentlyContinue
    }
    if ($sp) { $spObjectId = $sp.Id }
    $existing = Get-AzRoleAssignment -ObjectId $spObjectId -RoleDefinitionName "Key Vault Secrets Officer" -Scope $keyVault.ResourceId -ErrorAction SilentlyContinue
    if (!$existing) {
        New-AzRoleAssignment -ObjectId $spObjectId -RoleDefinitionName "Key Vault Secrets Officer" -Scope $keyVault.ResourceId | Out-Null
    }
}

# Wait briefly for RBAC role propagation before writing secrets. New role
# assignments typically take 10-60s before the KV data plane accepts the
# principal; this helper retries the secret write to absorb that delay.
function Set-KvSecretWithRetry {
    param(
        [Parameter(Mandatory)] [string] $VaultName,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [System.Security.SecureString] $SecretValue,
        [int] $MaxAttempts = 12,
        [int] $DelaySeconds = 10
    )
    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            Set-AzKeyVaultSecret -VaultName $VaultName -Name $Name -SecretValue $SecretValue -ErrorAction Stop | Out-Null
            return
        } catch {
            if ($i -eq $MaxAttempts) { throw }
            Write-Host "Set-AzKeyVaultSecret '$Name' attempt $i failed: $($_.Exception.Message). Retrying in ${DelaySeconds}s..."
            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

$connectionString = Get-AzIotHubConnectionString $ResourceGroupName -Name $iothub.Name -KeyName "iothubowner"
$SubscriptionId = $context.Subscription.Id

Write-Host "Adding/Updating KeyVault-Secret 'PCS-IOTHUB-CONNSTRING' with value '***'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $connectionString.PrimaryConnectionString -AsPlainText -Force
Set-KvSecretWithRetry -VaultName $keyVault.VaultName -Name 'PCS-IOTHUB-CONNSTRING' -SecretValue $secret
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $TenantId -AsPlainText -Force
Set-KvSecretWithRetry -VaultName $keyVault.VaultName -Name 'PCS-AUTH-TENANT' -SecretValue $secret
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $SubscriptionId -AsPlainText -Force
Set-KvSecretWithRetry -VaultName $keyVault.VaultName -Name 'PCS-SUBSCRIPTION-ID' -SecretValue $secret
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $ResourceGroupName -AsPlainText -Force
Set-KvSecretWithRetry -VaultName $keyVault.VaultName -Name 'PCS-RESOURCE-GROUP' -SecretValue $secret

Write-Host "Deployment finished."