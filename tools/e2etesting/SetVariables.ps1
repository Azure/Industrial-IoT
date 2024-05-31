Param(
    [string] $BranchName,
    [string] $Region,
    [string] $ImageTag,
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
        dotnet tool install --global --framework net8.0 nbgv 2>&1
    }
    catch  {}
    try
    {
        $props = $(nbgv get-version -f json) | ConvertFrom-Json
        if ($LastExitCode -ne 0) {
            throw "Error: 'nbgv get-version -f json' failed with $($LastExitCode)."
        }
        $version = $props.CloudBuildAllVars.NBGV_SimpleVersion
        $prerelease = $props.CloudBuildAllVars.NBGV_PrereleaseVersion
        $script:ImageTag = "$($version)$($prerelease)"
    }
    catch
    {
        # build as latest if not building from ci/cd pipeline
        Write-Warning "Unable to determine version - use latest."
        $script:ImageTag = "latest"
    }
}


# Set namespace name based on branch name
$imageNamespace = $script:BranchName
if ($imageNamespace.StartsWith("feature/"))
{
    # dev feature builds
    $imageNamespace = $imageNamespace.Replace("feature/", "")
}
elseif ($imageNamespace.StartsWith("release/") -or ($imageNamespace -eq "releases"))
{
    $imageNamespace = "public"
}
$imageNamespace = $imageNamespace.Replace("_", "/").Substring(0, [Math]::Min($imageNamespace.Length, 24))

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
    if ($imageNamespace -eq "public")
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
Write-Host "Use $($script:ImageTag) images in namespace $($imageNamespace) from $($registry)."
Write-Host "=============================================================================="
Write-Host ""

Write-Host "##vso[task.setvariable variable=ImageTag]$($script:ImageTag)"
Write-Host "##vso[task.setvariable variable=ImageNamespace]$($imageNamespace)"

if ([string]::IsNullOrEmpty($script:Region))
{
    $script:Region = "westus"
}
Write-Host "##vso[task.setvariable variable=Region]$($script:Region)"

