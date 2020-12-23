<#
 .SYNOPSIS
    Builds multiarch containers from the container.json file in the path.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

 .PARAMETER Path
    The folder to build the docker files from

 .PARAMETER Registry
    The name of the registry

 .PARAMETER Subscription
    The subscription to use - otherwise uses default

 .PARAMETER Debug
    Build debug and include debugger into images (where applicable)
#>

Param(
    [string] $Path = $null,
    [string] $Registry = $null,
    [string] $Subscription = $null,
    [switch] $Debug
)

# Check path argument and resolve to full existing path
if ([string]::IsNullOrEmpty($Path)) {
    throw "No docker folder specified."
}
$getroot = (Join-Path $PSScriptRoot "get-root.ps1")
if (!(Test-Path -Path $Path -PathType Container)) {
    $Path = Join-Path (& $getroot -fileName $Path) $Path
}
$Path = Resolve-Path -LiteralPath $Path

# Try to build all code and create dockerfile definitions to build using docker.
$definitions = & (Join-Path $PSScriptRoot "docker-source.ps1") `
    -Path $Path -Debug:$Debug
if ($definitions.Count -eq 0) {
    Write-Host "Nothing to build."
    return
}

# Try get branch name
$branchName = $env:BUILD_SOURCEBRANCH
if (![string]::IsNullOrEmpty($branchName)) {
    if ($branchName.StartsWith("refs/heads/")) {
        $branchName = $branchName.Replace("refs/heads/", "")
    }
    else {
        Write-Warning "'$($branchName)' is not a branch."
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName)) {
    try {
        $argumentList = @("rev-parse", "--abbrev-ref", "HEAD")
        $branchName = (& "git" $argumentList 2>&1 | ForEach-Object { "$_" });
        if ($LastExitCode -ne 0) {
            throw "git $($argumentList) failed with $($LastExitCode)."
        }
    }
    catch {
        Write-Warning $_.Exception
        $branchName = $null
    }
}

if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    Write-Warning "Not building from a branch - skip image build."
    return
}

# Set namespace name based on branch name
$releaseBuild = $false
$namespace = $branchName
if ($namespace.StartsWith("feature/")) {
    $namespace = $namespace.Replace("feature/", "")
}
elseif ($namespace.StartsWith("release/") -or ($namespace -eq "master")) {
    $namespace = "public"
    $releaseBuild = $true
}
$namespace = $namespace.Replace("_", "/").Substring(0, [Math]::Min($namespace.Length, 24))
$namespace = "$($namespace)/"

if (![string]::IsNullOrEmpty($Registry) -and ($Registry -ne "industrialiot")) {
    # if we build from release or from master and registry is provided we leave namespace empty
    if ($releaseBuild) {
        $namespace = ""
    }
}

# get and set build information from gitversion, git or version content
$latestTag = "latest"
$sourceTag = $env:Version_Prefix
$prereleaseTag = $env:Version_Prerelease
if ([string]::IsNullOrEmpty($prereleaseTag))
{
    $prereleaseTag = "-alpha"
}
if ([string]::IsNullOrEmpty($sourceTag)) {
    try {
        $version = & (Join-Path $PSScriptRoot "get-version.ps1")
        $sourceTag = $version.Prefix
        $prereleaseTag = $version.Prerelease
    }
    catch {
        $sourceTag = $null
    }
}
if (![string]::IsNullOrEmpty($sourceTag)) {
    Write-Host "Using version $($sourceTag)$($prereleaseTag) from get-version.ps1"
}
else {
    # Otherwise look at git tag
    if (![string]::IsNullOrEmpty($env:BUILD_SOURCEVERSION)) {
        # Try get current tag
        try {
            $argumentList = @("tag", "--points-at", $env:BUILD_SOURCEVERSION)
            $sourceTag = (& "git" $argumentList 2>&1 | ForEach-Object { "$_" });
            if ($LastExitCode -ne 0) {
                throw "git $($argumentList) failed with $($LastExitCode)."
            }
        }
        catch {
            Write-Error "Error reading tag from $($env:BUILD_SOURCEVERSION)"
            $sourceTag = $null
        }
    }
    if ([string]::IsNullOrEmpty($sourceTag)) {
        throw "Failed getting version from get-version.ps1"
    }
}

# set default subscription
if (![string]::IsNullOrEmpty($Subscription)) {
    Write-Debug "Setting subscription to $($Subscription)"
    $argumentList = @("account", "set", "--subscription", $Subscription)
    & "az" $argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# Check and set registry
if ([string]::IsNullOrEmpty($Registry)) {
    $Registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($Registry)) {
        if ($releaseBuild) {
            # Make sure we do not override latest in release builds - this is done manually later.
            $latestTag = "preview"
            $Registry = "industrialiot"
        }
        else {
            $Registry = "industrialiotdev"
        }
        Write-Warning "No registry specified - using $($Registry).azurecr.io."
    }
}

# get registry information
$argumentList = @("acr", "show", "--name", $Registry)
$RegistryInfo = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$resourceGroup = $RegistryInfo.resourceGroup
Write-Debug "Using resource group $($resourceGroup)"
# get credentials
$argumentList = @("acr", "credential", "show", "--name", $Registry)
$credentials = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$user = $credentials.username
$password = $credentials.passwords[0].value
Write-Debug "Using User name $($user) and passsword ****"

# Get build root - this is the top most folder with .dockerignore
$buildRoot = & $getroot -startDir $Path -fileName ".dockerignore"
# Get meta data
$metadata = Get-Content -Raw -Path (join-path $Path "container.json") `
| ConvertFrom-Json


# Set image name and namespace in acr based on branch and source tag
$imageName = $metadata.name

$tagPostfix = ""
$tagPrefix = ""
if ($Debug.IsPresent) {
    $tagPostfix = "-debug"
}
if (![string]::IsNullOrEmpty($metadata.tag)) {
    $tagPrefix = "$($metadata.tag)-"
}

$fullImageName = "$($Registry).azurecr.io/$($namespace)$($imageName):$($tagPrefix)$($sourceTag)$($prereleaseTag)$($tagPostfix)"
Write-Host "Full image name: $($fullImageName)"

$manifest = @" 
image: $($fullImageName)
tags: [$($tagPrefix)$($latestTag)$($tagPostfix)]
manifests:
"@

Write-Host "Building $($definitions.Count) images in $($Path) in $($buildRoot)"
Write-Host " and pushing to $($Registry)/$($namespace)$($imageName)..."

$definitions | Out-Host

# Create build jobs from build definitions
$jobs = @()
$definitions | ForEach-Object {

    $dockerfile = $_.dockerfile
    $buildContext = $_.buildContext
    $platform = $_.platform.ToLower()
    $platformTag = $_.platformTag.ToLower()

    $os = ""
    $osVersion = ""
    $osVerArr = $platform.Split(':')
    if ($osVerArr.Count -gt 1) {
        # --platform argument must be without os version
        $platform = $osVerArr[0]
        $osVersion = ("osversion: {0}" -f $osVerArr[1])
    }

    $osArchArr = $platform.Split('/')
    if ($osArchArr.Count -gt 1) {
        $os = ("os: {0}" -f $osArchArr[0])
        $architecture = ("architecture: {0}" -f $osArchArr[1])
        $variant = ""
        if ($osArchArr.Count -gt 2) {
            $variant = ("variant: {0}" -f $osArchArr[2])
        }
    }

    $image = "$($namespace)$($imageName):$($tagPrefix)$($sourceTag)$($prereleaseTag)-$($platformTag)$($tagPostfix)"
    Write-Host "Start build job for $($image)"

    # acr does not support arm64 as platform
    $platform = $platform.Replace("arm64", "arm")

    # Create acr command line 
    $argumentList = @("acr", "build", "--verbose",
        "--registry", $Registry,
        "--resource-group", $resourceGroup,
        "--platform", $platform,
        "--file", $dockerfile,
        "--image", $image
    )
    $argumentList += $buildContext

    $jobs += Start-Job -Name $image -ArgumentList $argumentList -ScriptBlock {
        $argumentList = $args
        Write-Host "Building ... az $($argumentList | Out-String) for $($image)..."
        & az $argumentList 2>&1 | ForEach-Object { "$_" }
        if ($LastExitCode -ne 0) {
            Write-Warning "az $($argumentList | Out-String) failed for $($image) with $($LastExitCode) - 2nd attempt..."
            & az $argumentList 2>&1 | ForEach-Object { "$_" }
            if ($LastExitCode -ne 0) {
                throw "Error: 'az $($argumentList | Out-String)' 2nd attempt failed for $($image) with $($LastExitCode)."
            }
        }
        Write-Host "... az $($argumentList | Out-String) completed for $($image)."
    }
    
    # Append to manifest
    if (![string]::IsNullOrEmpty($os)) {
        $manifest +=
        @"

  - 
    image: $($Registry).azurecr.io/$($image)
    platform: 
      $($os)
      $($architecture)
      $($osVersion)
      $($variant)
"@
    }
}

if ($jobs.Count -ne 0) {
    # Wait until all jobs are completed
    Receive-Job -Job $jobs -WriteEvents -Wait | Out-Host
    $jobs | Out-Host
    $jobs | Where-Object { $_.State -ne "Completed" } | ForEach-Object {
        throw "ERROR: Building $($_.Name). resulted in $($_.State)."
    }
}
Write-Host "All build jobs completed successfully."
Write-Host ""

# Build manifest using the manifest tool
$manifestFile = New-TemporaryFile
$url = "https://github.com/estesp/manifest-tool/releases/download/v1.0.0/"
if ($env:OS -eq "windows_nt") {
    $manifestTool = "manifest-tool-windows-amd64.exe"
}
else {
    $manifestTool = "manifest-tool-linux-amd64"
}
$manifestToolPath = Join-Path $PSScriptRoot $manifestTool
while ($true) {
    try {
        # Download and verify manifest tool
        $wc = New-Object System.Net.WebClient
        $url += $manifestTool
        Write-Host "Downloading $($manifestTool)..."
        $wc.DownloadFile($url, $manifestToolPath)

        if (Test-Path $manifestToolPath) {
            break
        }
    }
    catch {
        Write-Warning "Failed to download $($manifestTool) - try again..."
        Start-Sleep -s 3
    }
}

try {
    Write-Host "Building and pushing manifest file:"
    Write-Host
    $manifest | Out-Host
    $manifest | Out-File -Encoding ascii -FilePath $manifestFile.FullName
    $argumentList = @( 
        "--username", $user,
        "--password", $password,
        "push",
        "from-spec", $manifestFile.FullName
    )
    while ($true) {
        (& $manifestToolPath $argumentList) | Out-Host
        if ($LastExitCode -eq 0) {
            break   
        }
        Write-Warning "Manifest push failed - try again."
        Start-Sleep -s 2
    }
    Write-Host "Manifest pushed successfully."
}
catch {
    throw $_.Exception
}
finally {
    Remove-Item -Force -Path $manifestFile.FullName
    Remove-Item -Force -Path $manifestToolPath
}
