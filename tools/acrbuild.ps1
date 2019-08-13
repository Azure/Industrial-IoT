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

 .PARAMETER debug
    Build debug
#>

Param(
    [string] $path = $null,
    [string] $registry = $null,
    [string] $subscription = $null,
    [switch] $debug
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

$imageTag = ""
$configuration = "Release"
if ($debug.IsPresent) {
    $configuration = "Debug"
    $imageTag = "-debug"
}

# Create manifest file
$tags = @()
$versionParts = $sourceTag.Split('.')
if ($versionParts.Count -gt 0) {
    $versionTag = $versionParts[0]
    $tags += $versionTag
    for ($i = 1; $i -lt $versionParts.Count; $i++) {
        $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
        $tags += "$($versionTag)$($imageTag)"
    }
    $tagList = ("'{0}'" -f ($tags -join "', '"))
}

$manifest = @" 
image: $($registry).azurecr.io/$($namespace)$($imageName):latest$($imageTag)
tags: [$($tagList)]
manifests:
"@

$definitions = @()

# Create build job definitions from dotnet project in current folder
$projFile = Get-ChildItem $path -Filter *.csproj | Select-Object -First 1
if ($projFile -ne $null) {
    $output = (join-path $path (join-path "bin" (join-path "publish" $configuration)))
    @("linux-arm", "linux-x64", "win-x64", "win-arm", "win-arm64", "") `
        | ForEach-Object {
        $runtimeId = $_

        # Create dotnet command line 
        $argumentList = @("publish", "-c", $configuration)
        if (![string]::IsNullOrEmpty($runtimeId)) {
            $argumentList += "-r"
            $argumentList += $runtimeId
        }
        else {
            $runtimeId = "portable"
        }
        $argumentList += "-o"
        $argumentList += (join-path $output $runtimeId)
        $argumentList += $projFile.FullName

        Write-Host "Publish $($projFile.FullName) with $($runtimeId) runtime..."
        & dotnet $argumentList 2>&1 | %{ "$_" }
        if ($LastExitCode -ne 0) {
            throw "Error: 'dotnet $($argumentList)' failed with $($LastExitCode)."
        }
    }

    $installLinuxDebugger = @"
RUN apt-get update && apt-get install -y --no-install-recommends unzip curl procps \
    && rm -rf /var/lib/apt/lists/* \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg
ENV PATH="${PATH}:/root/vsdbg/vsdbg"
RUN apt-get update && apt-get install -y --no-install-recommends openssh-server \
    && mkdir /var/run/sshd \
    && echo 'root:Passw0rd' | chpasswd \
    && sed -i 's/PermitRootLogin.*/PermitRootLogin yes/' /etc/ssh/sshd_config \
    && sed 's@session\s*required\s*pam_loginuid.so@session optional \
        pam_loginuid.so@g' -i /etc/pam.d/sshd \
    && echo "export VISIBLE=now" >> /etc/profile
ENV NOTVISIBLE "in users profile"
"@

    # Default platform definitions
    $platforms = @{
        "linux/arm/v7" = @{
            runtimeId = "linux-arm"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:2.2"
            platformTag = "linux-arm32v7"
            debugger = $installLinuxDebugger
        }
        "linux/amd64" = @{
            runtimeId = "linux-x64"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:2.2"
            platformTag = "linux-amd64"
            debugger = $installLinuxDebugger
        }
        "windows/amd64:10.0.17134.885" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1803"
            platformTag = "nanoserver-amd64-1803"
            debugger = $null
        }
        "windows/amd64:10.0.17763.615" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1809"
            platformTag = "nanoserver-amd64-1809"
            debugger = $null
        }
        "windows/amd64" = @{
            runtimeId = "win-arm"
            image = "mcr.microsoft.com/windows/nanoserver:1809-arm32v7"
            platformTag = "nanoserver-arm32v7-1809"
            debugger = $null
        }
        "windows/amd64:10.0.18362.239" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1903"
            platformTag = "nanoserver-amd64-1903"
            debugger = $null
        }
    }

    # Get entry point from project file
    $entryPoint = $projFile.BaseName

    # todo - get actual endpoint


    # Create build definitions
    $platforms.Keys | ForEach-Object {
        $platformInfo = $platforms.Item($_)

        $runtimeId = $platformInfo.runtimeId
        $baseImage = $platformInfo.image
        $platformTag = $platformInfo.platformTag

        # Check for overridden base image name - e.g. aspnet core images
        if (![string]::IsNullOrEmpty($metadata.base)) {
            $baseImage = $metadata.base
            $runtimeId = $null
        }

        # Set where to obtain image content
        $imageContent = (join-path $output "portable")
        if (![string]::IsNullOrEmpty($runtimeId)) {
            $imageContent = (join-path $output $runtimeId)
        }

        $installExtra = ""
        if ($debug.IsPresent) {
            if (![string]::IsNullOrEmpty($platformInfo.debugger)) {
                $installExtra = $platformInfo.debugger
            }
        }
        $exposes = ""
        if ($metadata.exposes -ne $null) {
            $metadata.exposes | ForEach-Object {
                $exposes = "$("EXPOSE $($_)" | Out-String)$($exposes)"
            }
        }
        $dockerFileContent = @"
FROM $($baseImage)
$($exposes)
$($installExtra)
WORKDIR /app
COPY . .
ENTRYPOINT ["$($entryPoint)"]
"@ 
        $dockerFile = (join-path $imageContent "Dockerfile.$($platformTag)")
        $dockerFileContent | Out-File -Encoding ascii -FilePath $dockerFile
        $definitions += @{
            platform = $_
            dockerfile = $dockerFile
            platformTag = $platformTag
            buildContext = $imageContent
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

        # Backcompat folder structure
        if ($platform -eq "linux/arm32v7") {
            $platform = "linux/arm/v7"
        }

        $definitions += @{
            dockerfile = $dockerfile
            platform = $platform
            platformTag = $null
            buildContext = $buildRoot
        }
    }
}

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
    $platform = $_.platform
    $platformTag = $_.platformTag
    $buildContext = $_.buildContext

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

    if ([string]::IsNullOrEmpty($platformTag)) {
        $platformTag = $platform.Replace("/", "-")
        if ($platformTag -eq "linux-arm-v7") {
            # Backcompat
            $platformTag = "linux-arm32v7"
        }
    }

    $image = "$($namespace)$($imageName):$($sourceTag)-$($platformTag)$($imageTag)"
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

