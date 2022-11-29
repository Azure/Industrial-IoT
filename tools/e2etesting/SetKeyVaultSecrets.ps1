Param(
    [string] $KeyVaultName,
    [string] $ImageTag,
    [string] $ImageNamespace,
    [string] $ContainerRegistryServer,
    [string] $ContainerRegistryUsername,
    [string] $ContainerRegistryPassword
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$KeyVaultName) {
    Write-Error "KeyVaultName not set."
}

if (!$ImageTag) {
    $ImageNamespace = "latest"
}

if (!$ImageNamespace) {
    $ImageNamespace = ""
}

if (!$ContainerRegistryServer) {
    $ContainerRegistryServer = "mcr.microsoft.com"
}

if (!$ContainerRegistryUsername) {
    $ContainerRegistryUsername = ""
}

if (!$ContainerRegistryPassword) {
    $ContainerRegistryPassword = ""
}

## Login if required

Write-Host "Getting Azure Context..."
$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Ensure KeyVault
$resourceGroup = (Get-AzResource -Name $KeyVaultName).ResourceGroupName
$keyVault = Get-AzKeyVault -ResourceGroupName $resourceGroup -VaultName $KeyVaultName

Write-Host "Adding/Updating KeyVault-Secrets..."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-DOCKER-SERVER' -SecretValue (ConvertTo-SecureString $ContainerRegistryServer -AsPlainText -Force) | Out-Null
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-DOCKER-USER' -SecretValue (ConvertTo-SecureString $ContainerRegistryUsername -AsPlainText -Force) | Out-Null
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-DOCKER-PASSWORD' -SecretValue (ConvertTo-SecureString $ContainerRegistryPassword -AsPlainText -Force) | Out-Null
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-IMAGES-NAMESPACE' -SecretValue (ConvertTo-SecureString $ImageNamespace -AsPlainText -Force) | Out-Null
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'PCS-IMAGES-TAG' -SecretValue (ConvertTo-SecureString $ImageTag -AsPlainText -Force) | Out-Null

