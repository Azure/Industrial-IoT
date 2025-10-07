Param(
    [string] $BranchName,
    [string] $Region,
    [string] $ImageTag,
    [string] $ContainerRegistryServer,
    [string] $ResourceGroupName,
    [string] $SubscriptionId
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

Write-Host "Looking up credentials for $($registry) registry in KV $KeyVaultName."

if ($registry -eq "mcr.microsoft.com") {
    Write-Host "##vso[task.setvariable variable=ContainerRegistryServer]$($registry)"
}
else {
    if ([string]::IsNullOrWhiteSpace($ResourceGroupName))
    {
        throw "ResourceGroupName not provided."
    }

    if ([string]::IsNullOrWhiteSpace($SubscriptionId))
    {
        throw "SubscriptionId not provided."
    }

    # Get the ACR credentials
    $acrCredentials = Get-AzContainerRegistryCredential `
        -ResourceGroupName $ResourceGroupName `
        -RegistryName $registry `
        -SubscriptionId $SubscriptionId

    Write-Host "##vso[task.setvariable variable=ContainerRegistryPassword]$($acrCredentials.Password)"
    Write-Host "##vso[task.setvariable variable=ContainerRegistryServer]$($registry).azurecr.io"
    Write-Host "##vso[task.setvariable variable=ContainerRegistryUsername]$($acrCredentials.Username)"
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
