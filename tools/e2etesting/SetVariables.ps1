Param(
    [string] $BranchName,
    [string] $Region,
    [string] $ImageTag,
    [string] $ImageNamespace,
    [string] $ContainerRegistryServer
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"
# Set-PSDebug -Trace 2

if (![string]::IsNullOrEmpty($script:BranchName))
{
    if ($script:BranchName.StartsWith("refs/heads/")) 
    {
        $script:BranchName = $script:BranchName.Replace("refs/heads/", "")
    }
}
else
{
    $script:BranchName = "main"
}

if ([string]::IsNullOrEmpty($script:ImageTag))
{
    $script:ImageTag = "$($env:Version_Prefix)$($env:Version_Prerelease)"
}
if ([string]::IsNullOrEmpty($script:ImageTag))
{
    try 
    {
        & dotnet @("tool", "install", "--tool-path", "./tools", "--framework", "net6.0", "nbgv") 2>&1 
    }
    catch  {}
    try
    {
        $props = (& ./tools/nbgv  @("get-version", "-f", "json")) | ConvertFrom-Json
        if ($LastExitCode -ne 0) {
            throw "Error: 'nbgv get-version -f json' failed with $($LastExitCode)."
        }
        $version = $props.CloudBuildAllVars.NBGV_SimpleVersion
        $prerelease = $props.CloudBuildAllVars.NBGV_PrereleaseVersion
        $script:ImageTag = "$($version)$($prerelease)"

        # Remove-Item "./tools" -Recurse
    }
    catch 
    {
        # build as latest if not building from ci/cd pipeline
        Write-Warning "Unable to determine version - use latest."
        $script:ImageTag = "latest"
    }    
}

if ([string]::IsNullOrEmpty($script:ImageNamespace))
{
    # Set namespace name based on branch name
    $script:ImageNamespace = $script:BranchName
    if ($script:ImageNamespace.StartsWith("feature/")) 
    {
        # dev feature builds
        $script:ImageNamespace = $script:ImageNamespace.Replace("feature/", "")
    }
    elseif ($script:ImageNamespace.StartsWith("release/") -or ($script:ImageNamespace -eq "releases")) 
    {
        $script:ImageNamespace = "public"
    }
    $script:ImageNamespace = $script:ImageNamespace.Replace("_", "/").Substring(0, [Math]::Min($script:ImageNamespace.Length, 24))
}

function Get-ContainerRegistrySecret 
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $keyVaultName,

        [Parameter(Mandatory=$true)]
        [string] $secret
    )
    $secretValueSec = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secret
    $ssPtr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secretValueSec.SecretValue)
    try 
    {
        $secretValueText = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ssPtr)
    } 
    finally 
    {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ssPtr)
    }
    Write-Host "##vso[task.setvariable variable=$($secret)]$($secretValueText)"
}

if ([string]::IsNullOrEmpty($script:ContainerRegistryServer))
{
    if ($script:ImageNamespace -eq "public") 
    {
        # Release and Preview builds are in staging
        $registry = "industrialiot"
        $KeyVaultName = "kv-release-pipeline"
    }
    else 
    {
        # Feature builds by default into dev registry
        $registry = "industrialiotdev"
        $KeyVaultName = "kv-developer-pipeline"
    }

    Write-Host "Looking up credentials for $($registry) registry in KV $KeyVaultName."
    Get-ContainerRegistrySecret -keyVaultName $KeyVaultName -secret "ContainerRegistryPassword"
    Get-ContainerRegistrySecret -keyVaultName $KeyVaultName -secret "ContainerRegistryServer"
    Get-ContainerRegistrySecret -keyVaultName $KeyVaultName -secret "ContainerRegistryUsername"
}
else 
{
    $registry = $script:ContainerRegistryServer
}

Write-Host "=============================================================================="
Write-Host "Use $($script:ImageTag) images in namespace $($script:ImageNamespace) from $($registry)."
Write-Host "=============================================================================="
Write-Host ""

Write-Host "##vso[task.setvariable variable=ImageTag]$($script:ImageTag)"
Write-Host "##vso[task.setvariable variable=ImageNamespace]$($script:ImageNamespace)"

if ([string]::IsNullOrEmpty($script:Region)) 
{
    $script:Region = "westus"
}
Write-Host "##vso[task.setvariable variable=Region]$($script:Region)"

