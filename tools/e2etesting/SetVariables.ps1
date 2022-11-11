Param(
    [string] $BranchName,
    [string] $Region,
    [string] $ImageTag,
    [string] $ImageNamespace,
    [string] $ContainerRegistryServer,
    [string] $ServicePrincipalId
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

if ([string]::IsNullOrEmpty($script:ContainerRegistryServer))
{
    if ($script:ImageNamespace -eq "public") 
    {
        # Release and Preview builds are in staging
        $registry = "industrialiot"
    }
    else 
    {
        # Feature builds by default into dev registry
        $registry = "industrialiotdev"
    }

    Write-Host "Looking up credentials for $($registry) registry."
    Login-AzAccount -Tenant "72f988bf-86f1-41af-91ab-2d7cd011db47" 
    $containerContext = Get-AzContext -ListAvailable | Where-Object { $_.Subscription.Name -eq "IOT_GERMANY" }
    if ($containerContext.Length -gt 1) 
    {
        $containerContext = $containerContext[0]
    }
    if (!$containerContext)
    {
        Write-Host "No context to get container registry"
        Get-AzContext -ListAvailable
    }
    else 
    {
        Set-AzContext $containerContext | Out-Null
        $registry = Get-AzContainerRegistry | Where-Object { $_.Name -eq $registry }
        $creds = Get-AzContainerRegistryCredential -Registry $registry 

        $script:ContainerRegistryServer = $registry.LoginServer
        Write-Host "##vso[task.setvariable variable=ContainerRegistryUsername]$($creds.Username)"
        Write-Host "##vso[task.setvariable variable=ContainerRegistryPassword]$($creds.Password)"
    }
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
Write-Host "##vso[task.setvariable variable=ContainerRegistryServer]$($script:ContainerRegistryServer)"

if ([string]::IsNullOrEmpty($script:Region)) 
{
    $script:Region = "westus"
}
Write-Host "##vso[task.setvariable variable=Region]$($script:Region)"
