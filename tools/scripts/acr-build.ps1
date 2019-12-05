<#
 .SYNOPSIS
    Builds multiarch containers from the container.json file in the path.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    If a csproj file exists in the same folder as the csproj file
    it it publishes it and builds container images with the output 
    as content of the images.  
    
    If there is no project file it finds all the dockerfiles and builds each 
    one. It traverses back up to the closest .dockerignore file. This folder 
    becomes the context of the build.

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

# Get build root - this is the top most folder with .dockerignore
$buildRoot = & $getroot -startDir $Path -fileName ".dockerignore"
# Get meta data
$metadata = Get-Content -Raw -Path (join-path $Path "container.json") `
    | ConvertFrom-Json

# get and set build information from gitversion, git or version content
$sourceTag = $env:Version_Prefix
if ([string]::IsNullOrEmpty($sourceTag)) {
    try {
        $version = & (Join-Path $PSScriptRoot "get-version.ps1")
        $sourceTag = $version.Prefix
    }
    catch {
        $sourceTag = $null
    }
}
if (![string]::IsNullOrEmpty($sourceTag)) {
    Write-Host "Using version $($sourceTag) from get-version.ps1"
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
        $sourceTag = "latest"
    }
}

# Try get branch name
$branchName = $env:BUILD_SOURCEBRANCH
if (![string]::IsNullOrEmpty($branchName)) {
    if ($branchName.StartsWith("refs/heads/")) {
        $branchName = $branchName.Replace("refs/heads/", "")
    }
    else {
        Write-Warning "Error - '$($branchName)' not recognized as branch."
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

# Check and set registry
if ([string]::IsNullOrEmpty($Registry)) {
    $Registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($Registry)) {
        $Registry = "industrialiotdev"
        Write-Warning "No registry specified - using $($Registry).azurecr.io."
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
# Set image name and namespace in acr based on branch and source tag
$imageName = $metadata.name

# Set namespace name
if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    Write-Warning "Error - Branch '$($branchName)' invalid - using default."
    $namespace = "deletemesoon"
    $branchName = "HEAD"
}
else {
    $namespace = $branchName
    if ($namespace.StartsWith("feature/")) {
        $namespace = $namespace.Replace("feature/", "")
    }
    elseif ($namespace.StartsWith("release/")) {
        $namespace = "master"
    }
    $namespace = $namespace.Substring(0, [Math]::Min($namespace.Length, 24))
}
$namespace = "$($namespace)/"
Write-Host "Pushing '$($sourceTag)' build for $($branchName) to $($namespace)."

$tagPostfix = ""
$tagPrefix = ""
if ($Debug.IsPresent) {
    $tagPostfix = "-debug"
}
if (![string]::IsNullOrEmpty($metadata.tag)) {
    $tagPrefix = "$($metadata.tag)-"
}

# Create manifest file
$tags = @()
$versionParts = $sourceTag.Split('.')
if ($versionParts.Count -gt 0) {
    $versionTag = $versionParts[0]
    $tags += "$($tagPrefix)$($versionTag)$($tagPostfix)"
    for ($i = 1; $i -lt $versionParts.Count; $i++) {
        $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
        $tags += "$($tagPrefix)$($versionTag)$($tagPostfix)"
    }
    $tagList = ("'{0}'" -f ($tags -join "', '"))
}

$fullImageName = "$($Registry).azurecr.io/$($namespace)$($imageName):$($tagPrefix)latest$($tagPostfix)"

Write-Host "Full image name: $($fullImageName)"

$manifest = @" 
image: $($fullImageName)
tags: [$($tagList)]
manifests:
"@

# Source definitions
$definitions = & (Join-Path $PSScriptRoot "docker-source.ps1") `
    -Path $Path -Debug:$Debug
if ($definitions.Count -eq 0) {
    Write-Host "Nothing to build."
    return
}

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

    if ($platform -eq "linux/arm32v7") {
        $platform = "linux/arm/v7"
    }
    if ($platformTag -eq "linux-arm-v7") {
        $platformTag = "linux-arm32v7"
    }

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
            # Backcompat for when release version was used as windows variant
            $windowsVariantToOsVersionsTable = @{
                "1803" = "10.0.17134.885"
                "1809" = "10.0.17763.615"
                "1903" = "10.0.18362.239"
            }
            if ($windowsVariantToOsVersionsTable.ContainsKey($osArchArr[2])) {
                $osVersion = $windowsVariantToOsVersionsTable.Item($osArchArr[2])
                $osVersion = ("osversion: {0}" -f $osVersion)
            }
            else {
                $variant = ("variant: {0}" -f $osArchArr[2])
            }
        }
    }

    $image = "$($namespace)$($imageName):$($tagPrefix)$($sourceTag)-$($platformTag)$($tagPostfix)"
    Write-Host "Start build job for $($image)"

    # BUGBUG
    # BUGBUG : ACR fails with arm but it is likely 
    # BUGBUG : that win-arm does not work now either.
    # BUGBUG
    if ($platform -eq "windows/arm") {
        $platform = "windows/amd64"
    }
    # BUGBUG
    # BUGBUG

    # Create acr command line 
    $argumentList = @("acr", "build", "--verbose",
        "--registry", $Registry,
        "--resource-group", $resourceGroup,
        "--platform", $platform,
        "--file", $dockerfile,
        "--image", $image
    )

    $argumentList += $buildContext
    $jobs += Start-Job -Name $platform -ArgumentList $argumentList `
        -ScriptBlock {
        Write-Host "az $($args)"
        & az $args 2>&1 | ForEach-Object { "$_" }
        if ($LastExitCode -ne 0) {
            Write-Warning "az $($args) failed - 2nd attempt..."
            & az $args 2>&1 | ForEach-Object { "$_" }
            if ($LastExitCode -ne 0) {
                throw "Error: 'az $($args)' 2nd attempt failed with $($LastExitCode)."
            }
        }
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

# Wait until all jobs are completed
Receive-Job -Job $jobs -WriteEvents -Wait | Out-Host
$jobs | Out-Host
$jobs | Where-Object { $_.State -ne "Completed" } | ForEach-Object {
    throw "ERROR: Building $($_.Name). resulted in $($_.State)."
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

    (& $manifestToolPath $argumentList) | Out-Host
    if ($LastExitCode -ne 0) {
        Write-Warning "Manifest push failed - 2nd attempt."
        (& $manifestToolPath $argumentList) | Out-Host
        if ($LastExitCode -ne 0) {
            throw "$($manifestToolPath) end attempt failed with $($LastExitCode)."
        }
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
