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

# Helper: Set-AzKeyVaultSecret -SecretValue with -AsPlainText "" throws
# "Cannot bind argument to parameter 'String' because it is an empty string".
# For GHCR public packages the username/password are intentionally empty.
# We store a single space placeholder so secret-presence checks downstream
# still succeed and SetTestVariables won't see a missing secret.
function Set-OrPlaceholder([string]$Name, [string]$Value) {
    $emit = if ([string]::IsNullOrEmpty($Value)) { ' ' } else { $Value }
    Write-Host "Setting KeyVault secret '$Name'."
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
    $secure = ConvertTo-SecureString $emit -AsPlainText -Force
    Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name $Name -SecretValue $secure | Out-Null
}

Write-Host "Adding/Updating KeyVault-Secrets..."
Set-OrPlaceholder -Name 'PCS-DOCKER-SERVER'     -Value $ContainerRegistryServer
Set-OrPlaceholder -Name 'PCS-DOCKER-USER'       -Value $ContainerRegistryUsername
Set-OrPlaceholder -Name 'PCS-DOCKER-PASSWORD'   -Value $ContainerRegistryPassword
Set-OrPlaceholder -Name 'PCS-IMAGES-NAMESPACE'  -Value $ImageNamespace
Set-OrPlaceholder -Name 'PCS-IMAGES-TAG'        -Value $ImageTag

