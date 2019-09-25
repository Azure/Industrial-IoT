<#
 .SYNOPSIS
    Builds multiarch containers from the mcr.json file in the path.

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

 .PARAMETER path
    The folder to build the docker files from

 .PARAMETER registry
    The name of the registry

 .PARAMETER subscription
    The subscription to use - otherwise uses default

 .PARAMETER debug
    Build debug and include debugger into images (where applicable)
#>

Param(
    [string] $path = $null,
    [string] $registry = $null,
    [string] $subscription = $null,
    [switch] $debug
)

#
# find the top most folder with file in it and return the path
#
Function GetTopMostFolder() {
    param(
        [string] $startDir,
        [string] $fileName
    ) 
    $cur = $startDir
    while (![string]::IsNullOrEmpty($cur)) {
        if (Test-Path -Path (Join-Path $cur $fileName) -PathType Leaf) {
            return $cur
        }
        $cur = Split-Path $cur
    }
    return $startDir
}

# Check path argument and resolve to full existing path
if ([string]::IsNullOrEmpty($path)) {
    throw "No docker folder specified."
}
if (!(Test-Path -Path $path -PathType Container)) {
    $cur = Join-Path `
        (Split-Path $script:MyInvocation.MyCommand.Path) $path
    if (!(Test-Path -Path $cur -PathType Container)) {
    $cur = Join-Path (Split-Path `
        (Split-Path $script:MyInvocation.MyCommand.Path)) $path
        if (!(Test-Path -Path $cur -PathType Container)) {
            throw "$($path) does not exist."
        }
    }
    $path = $cur
}
$path = Resolve-Path -LiteralPath $path

# Get build root - this is the top most folder with .dockerignore
$buildRoot = GetTopMostFolder -startDir $path `
    -fileName ".dockerignore"
# Get meta data
$metadata = Get-Content -Raw -Path (join-path $path "mcr.json") `
    | ConvertFrom-Json

# get and set build information from gitversion, git or version content
$sourceTag = $env:GITVERSION_MajorMinorPatch
if ([string]::IsNullOrEmpty($sourceTag)) {
    try {
        # Call getversion.ps1 directly here.
        $version = & ./getversion.ps1
        $sourceTag = $version
    }
    catch {
        $sourceTag = $null
    }
}
if (![string]::IsNullOrEmpty($sourceTag)) {
    Write-Host "Using version $($sourceTag) from getversion.ps1"
}
else {
    # Otherwise look at git tag
    if (![string]::IsNullOrEmpty($env:BUILD_SOURCEVERSION)) {
        # Try get current tag
        try {
            $argumentList = @("tag", "--points-at", $env:BUILD_SOURCEVERSION)
            $sourceTag = (& "git" $argumentList 2>&1 | %{ "$_" });
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
$isDeveloperBuild = [string]::IsNullOrEmpty($env:RELEASE_BUILD)
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
        $branchName = (& "git" $argumentList 2>&1 | %{ "$_" });
    }
    catch {
        Write-Warning $_.Exception
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    Write-Warning "Error - Branch '$($branchName)' invalid - using default."
    $branchName = "deletemesoon"
}

# Check and set registry
$namespacePrefix = ""
if ([string]::IsNullOrEmpty($registry)) {
    $registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($registry)) {
        $registry = "industrialiot"
        if ($isDeveloperBuild -eq $true) {
            $registry = "industrialiotdev"
            $namespacePrefix = ""
        }
        Write-Warning "No registry specified - using $($registry).azurecr.io."
    }
}

# set default subscription
if (![string]::IsNullOrEmpty($subscription)) {
    Write-Debug "Setting subscription to $($subscription)"
    $argumentList = @("account", "set", "--subscription", $subscription)
    & "az" $argumentList 2`>`&1 | %{ "$_" }
}

# get registry information
$argumentList = @("acr", "show", "--name", $registry)
$registryInfo = (& "az" $argumentList 2>&1 | %{ "$_" }) | ConvertFrom-Json
$resourceGroup = $registryInfo.resourceGroup
Write-Debug "Using resource group $($resourceGroup)"
# get credentials
$argumentList = @("acr", "credential", "show", "--name", $registry)
$credentials = (& "az" $argumentList 2>&1 | %{ "$_" }) | ConvertFrom-Json
$user = $credentials.username
$password = $credentials.passwords[0].value
Write-Debug "Using User name $($user) and passsword ****"
# Set image name and namespace in acr based on branch and source tag
$imageName = $metadata.name
# Set namespace name
if ($isDeveloperBuild -eq $true) {
    $namespace = "$($namespacePrefix)$($branchName)/"
    Write-Host "Pushing '$($sourceTag)' developer build for $($branchName)."
}
else {
    $namespace = "public/"
    Write-Host "Pushing release build '$($sourceTag)' to public."
}

$tagPostfix = ""
$tagPrefix = ""
$configuration = "Release"
if ($debug.IsPresent) {
    $configuration = "Debug"
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

$manifest = @" 
image: $($registry).azurecr.io/$($namespace)$($imageName):$($tagPrefix)latest$($tagPostfix)
tags: [$($tagList)]
manifests:
"@

# Source definitions
$scriptPath = (Join-Path $PSScriptRoot "docker-source.ps1")
$definitions = & $scriptPath -path $path -configuration $configuration
if ($definitions.Count -eq 0) {
    Write-Host "Nothing to build."
    return
}

Write-Host "Building $($definitions.Count) images in $($path) in $($buildRoot)"
Write-Host " and pushing to $($registry)/$($namespace)$($imageName)..."

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
        "--registry", $registry,
        "--resource-group", $resourceGroup,
        "--platform", $platform,
        "--file", $dockerfile,
        "--image", $image
    )

    $argumentList += $buildContext
    $jobs += Start-Job -Name $platform -ArgumentList $argumentList `
        -ScriptBlock {
            Write-Host "az $($args)"
            & az $args 2>&1 | %{ "$_" }
            if ($LastExitCode -ne 0) {
                throw "Error: 'az $($args)' failed with $($LastExitCode)."
            }
        }
    # Append to manifest
    if (![string]::IsNullOrEmpty($os)) {
        $manifest +=
@"

  - 
    image: $($registry).azurecr.io/$($image)
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
$url = "https://github.com/estesp/manifest-tool/releases/download/v0.9.0/"
if ($env:OS -eq "windows_nt") {
    $manifestTool = "manifest-tool-windows-amd64.exe"
}
else {
    $manifestTool = "manifest-tool-linux-amd64"
}
$manifestToolPath = Join-Path $PSScriptRoot $manifestTool
try {
    # Download and verify manifest tool
    $wc = New-Object System.Net.WebClient
    $url += $manifestTool
    Write-Host "Downloading $($manifestTool)..."
    $wc.DownloadFile($url, $manifestToolPath)
    Write-Host "Downloading $($manifestTool).asc..."
    $url = $url + ".asc"
    $wc.DownloadFile($url, "$($manifestToolPath).asc")

    # TODO: validate 0F386284C03A1162

    Write-Host "Building and pushing manifest file:"
    Write-Host ""
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
        throw "$($manifestToolPath) failed with $($LastExitCode)."
    }

    Write-Host ""
    Write-Host "Manifest pushed successfully."
}
catch {
    if ($isDeveloperBuild -eq $true) {
        Write-Error "Could not push manifest"
        Write-Error $_.Exception.Message
    }
    else {
        throw $_.Exception
    }
}
finally {
    Remove-Item -Force -Path $manifestFile.FullName
    Remove-Item -Force -Path $manifestToolPath
    Remove-Item -Force -Path "$($manifestToolPath).asc"
}
