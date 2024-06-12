Param(
    [string] $BranchName,
    [string] $Region,
    [string] $ImageTag,
    [string] $ContainerRegistryServer
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"
# Set-PSDebug -Trace 2

$registry = $script:ContainerRegistryServer
if ([string]::IsNullOrWhiteSpace($registry))
{
    Write-Host "No container registry provided, using default."
    $registry = "industrialiotdev"
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

$KeyVaultName = $null
if ($registry -eq "industrialiot")
{
    $KeyVaultName = "kv-release-pipeline"
}
if ($registry -eq "industrialiotdev")
{
    $KeyVaultName = "kv-developer-pipeline"
}
if ($registry -eq "industrialiotprod")
{
   # $KeyVaultName = "kv-release-pipeline" #todo
}
if ($KeyVaultName)
{
    Write-Host "Looking up credentials for $($registry) registry in KV $KeyVaultName."
    Get-ContainerRegistrySecret -keyVaultName $KeyVaultName -secret "ContainerRegistryPassword"
    Get-ContainerRegistrySecret -keyVaultName $KeyVaultName -secret "ContainerRegistryServer"
    Get-ContainerRegistrySecret -keyVaultName $KeyVaultName -secret "ContainerRegistryUsername"
}

if ([string]::IsNullOrWhiteSpace($script:ImageTag))
{
    $script:ImageTag = "$($env:PlatformVersion)"
}
if ([string]::IsNullOrWhiteSpace($script:ImageTag))
{
    $script:ImageTag = "$($env:Version_Prefix)"
    if (![string]::IsNullOrWhiteSpace($env:Version_Suffix))
    {
        Write-Host "##vso[task.setvariable variable=PlatformVersion]$($script:ImageTag)"
    }
}
if ([string]::IsNullOrWhiteSpace($script:ImageTag))
{
    try
    {
        dotnet tool install --global --framework net8.0 nbgv 2>&1
    }
    catch  {}
    try
    {
        Write-Host "Using get-version to determine version."
        $props = $(nbgv get-version -f json) | ConvertFrom-Json
        if ($LastExitCode -ne 0) {
            throw "Error: 'nbgv get-version -f json' failed with $($LastExitCode)."
        }
        $version = $props.CloudBuildAllVars.NBGV_SimpleVersion
        $script:ImageTag = "$($version)"
        Write-Host "##vso[task.setvariable variable=PlatformVersion]$($script:ImageTag)"
    }
    catch
    {
        $script:ImageTag = $null
    }
}
if ([string]::IsNullOrWhiteSpace($script:ImageTag))
{
    # build as latest if not building from ci/cd pipeline
    Write-Warning "Unable to determine version - use latest."
    $script:ImageTag = "latest"
}

# Set namespace name based on branch name
if ($registry -eq "industrialiotdev")
{
    if (![string]::IsNullOrWhiteSpace($script:BranchName))
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

    $imageNamespace = $script:BranchName
    if ($imageNamespace.StartsWith("feature/"))
    {
        # dev feature builds
        $imageNamespace = $imageNamespace.Replace("feature/", "")
    }
    $imageNamespace = $imageNamespace.Replace("_", "/")
    $imageNamespace = $imageNamespace.Substring(0, [Math]::Min($imageNamespace.Length, 24))
}
elseif ($registry -eq "mcr.microsoft.com")
{
    $imageNamespace = ""
}
else 
{
    $imageNamespace = "public"
}

Write-Host "=============================================================================="
Write-Host "Selected $($script:ImageTag) images in namespace $($imageNamespace) from $($registry)."
Write-Host "=============================================================================="
Write-Host ""

Write-Host "##vso[task.setvariable variable=ImageTag]$($script:ImageTag)"
Write-Host "##vso[task.setvariable variable=ImageNamespace]$($imageNamespace)"
if ([string]::IsNullOrEmpty($script:Region))
{
    $script:Region = "westus"
}
Write-Host "##vso[task.setvariable variable=Region]$($script:Region)"

Write-Host "##vso[build.addbuildtag]$($script:ImageTag)"
Write-Host "##vso[build.addbuildtag]$($registry)"
if (![string]::IsNullOrWhiteSpace($imageNamespace)) 
{
    Write-Host "##vso[build.addbuildtag]$($imageNamespace)"
}
