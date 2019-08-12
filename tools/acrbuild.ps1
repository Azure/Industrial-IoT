<#
 .SYNOPSIS
    Creates the container build matrix from the mcr.json files in the tree.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    The script traverses the build root to find all folders with an mcr.json
    file.  
    
    In each folder with mcr.json it finds the dockerfiles for the platform 
    specific build. 
    
    It then traverses back up to the closest .dockerignore file. This folder 
    becomes the context of the build.
    
    Finally it stores the container build matrix and the manifest build matrix and 
    echos each as VSO variables to create the individual build jobs.

 .PARAMETER path
    The folder to build the docker files from

 .PARAMETER registry
    The name of the registry in ACR

 .PARAMETER subscription
    The subscription to use - otherwise uses default

 .PARAMETER configuration
    The build configuration - defaults to "release"
#>

Param(
    [string] $path = $null,
    [string] $registry = $null,
    [string] $subscription = $null,
    [string] $configuration = "Release"
)


# find the top most folder with file in it and return the path
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

# Check path argument
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

# Check and set registry
if ([string]::IsNullOrEmpty($registry)) {
    $registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($registry)) {
        Write-Warning "No registry specified - using default name."
        $registry = "industrialiot"
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

# Get build root - this is the top most folder with .dockerignore
$buildRoot = GetTopMostFolder -startDir $path `
    -fileName ".dockerignore"

# Get meta data
$metadata = Get-Content -Raw -Path (join-path $path "mcr.json") `
    | ConvertFrom-Json

# get and set build information from git or content
$sourceTag = $null
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
    try {
        $buildRoot = GetTopMostFolder -startDir $path `
            -fileName "version.props"
        # set version number from first encountered version.props
        [xml] $props=Get-Content -Path (Join-Path $buildRoot "version.props")
        $sourceTag="$($props.Project.PropertyGroup.VersionPrefix)".Trim()
    }
    catch {
        Write-Warning $_.Exception
        $sourceTag = $null
    }
}
if ([string]::IsNullOrEmpty($sourceTag)) {
    $sourceTag = "latest"
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
        # any build other than from master branch is a developer build.
    }
    catch {
        Write-Warning $_.Exception
        $branchName = $null
    }
}
if ([string]::IsNullOrEmpty($branchName) -or ($branchName -eq "HEAD")) {
    Write-Warning "Error - Branch '$($branchName)' invalid - fall back to default."
    $branchName = "deletemesoon"
}

# Set image name and namespace in acr based on branch and source tag
$imageName = $metadata.name
$namespace = "public/"
if ($isDeveloperBuild -eq $true) {
    $namespace = "internal/$($branchName)/"
    Write-Host "Pushing '$($sourceTag)' developer build for $($branchName)."
}
else {
    Write-Host "Pushing release build '$($sourceTag)' to public."
}

# Create manifest file
$tags = @()
$versionParts = $sourceTag.Split('.')
if ($versionParts.Count -gt 0) {
    $versionTag = $versionParts[0]
    $tags += $versionTag
    for ($i = 1; $i -lt $versionParts.Count; $i++) {
        $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
        $tags += $versionTag
    }
    $tagList = ("'{0}'" -f ($tags -join "', '"))
}

$manifest = @" 
image: $($registry).azurecr.io/$($namespace)$($imageName):latest
tags: [$($tagList)]
manifests:
"@

$definitions = @()

# Create job definitions from dotnet project in current folder
$projFile = Get-ChildItem $path -Filter *.csproj | Select-Object -First 1
if ($projFile -ne $null) {
    $runtimes = @{
        "linux-arm" = @{
            platform = "linux/arm/v7"
        }
        "linux-x64" =  @{
            platform = "linux/amd64"
        }
        "win-x64" =  @{
            platform = "windows/amd64"
        }
    }
    $runtimes.Keys | ForEach-Object {
        $runtimeId = $_

        Write-Host "Build and publish for $($runtimeId)"
        $output = (join-path "publish" $configuration $runtimeId);
        # Create dotnet command line 
        $argumentList = @("publish", 
            "-r", $runtimeId,
            "-o", $output,
            "-c", $configuration,
            $projFile.FullName
        )
        & dotnet $argumentList

        $rtc = $runtimes.Item($_)



        $dockerFile = join-path $output "Dockerfile"
@"
FROM mcr.microsoft.com/dotnet/core/runtime:2.2 
WORKDIR /app
COPY . .
ENTRYPOINT ["$($entryPoint)"]
"@ | Out-File $dockerFile
        $definitions += @{
            dockerfile = $dockerFile
            platform = $rtc.platform
            platformTag = $rtc.platformTag
            buildContext = $output
            osVersion = $null
        }
    }

}

if ($definitions.Count -eq 0) {
    # Create job definitions from dockerfile structure in current folder
    Get-ChildItem $path -Recurse `
        | Where-Object Name -eq "Dockerfile" `
        | ForEach-Object {

        $dockerfile = $_.FullName

        $platformFolder = $_.DirectoryName.Replace($path, "")
        $platform = $platformFolder.Substring(1).Replace("\", "/")

        if ($platform -eq "linux/arm32v7") {
            # Backcompat
            $platform = "linux/arm/v7"
        }

        $platformTag = $platform.Replace("/", "-")
        if ($platformTag -eq "linux-arm-v7") {
            # Backcompat
            $platformTag = "linux-arm32v7"
        }

        $definitions += @{
            dockerfile = $dockerfile
            platform = $platform
            platformTag = $platformTag
            buildContext = $buildRoot
            osVersion = $null
        }
    }

    if ($definitions.Count -eq 0) {
        Write-Host "Nothing to build."
        return
    }
}

Write-Host "Building $($definitions.Count) images in $($path) in $($buildRoot)"
Write-Host " and pushing to $($registry)/$($namespace)$($imageName)..."

# Create build jobs from build definitions
$jobs = @()
$definitions | ForEach-Object {

    $dockerfile = $_.dockerfile
    $platform = $_.platform
    $platformTag = $_.platformTag
    $osVersion = $_.osVersion
    $buildContext = $_.buildContext

    $image = "$($namespace)$($imageName):$($sourceTag)-$($platformTag)"

    Write-Host "Start build job for $($image)"
    # Create acr command line - TODO: Consider az powershell module?
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

    $os = ""
    $osArchArr = $platform.Split('/')
    if ($osArchArr.Count -gt 1) {
        $os = ("os: {0}" -f $osArchArr[0])
        if (![string]::IsNullOrEmpty($osVersion)) {
            $osVersion = ("osversion: {0}" -f $osVersion)
        }
        else {
            $osVersion = ""
        }
        $architecture = ("architecture: {0}" -f $osArchArr[1])
        $variant = ""
        if ($osArchArr.Count -gt 2) {
            $variant = ("variant: {0}" -f $osArchArr[2])
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
      $($osVersion)
      $($architecture)
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

