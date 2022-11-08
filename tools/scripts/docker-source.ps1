<#
 .SYNOPSIS
    Builds csproj file and returns buildable dockerfile build 
    definitions

 .PARAMETER Path
    The folder containing the container.json file.

 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.  
    Debug also includes debugger into images (where applicable).

 .PARAMETER Fast
    Perform a fast build.  This will only build what is needed for 
    the system to operate.
#>

Param(
    [string] $Path = $null,
    [string] $BuildRoot = $null,
    [switch] $Debug,
    [switch] $Fast
)

# Get meta data
if ([string]::IsNullOrEmpty($Path)) {
    throw "No docker folder specified."
}
if (!(Test-Path -Path $Path -PathType Container)) {
    $Path = Join-Path (& (Join-Path $PSScriptRoot "get-root.ps1") `
        -fileName $Path) $Path
}
$Path = Resolve-Path -LiteralPath $Path
$configuration = "Release"
if ($script:Debug.IsPresent) {
    $configuration = "Debug"
}
$metadata = Get-Content -Raw -Path (Join-Path $Path "container.json") `
    | ConvertFrom-Json

$definitions = @()

# set build root
if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = $Path
}
else {
    $BuildRoot = Join-Path (Resolve-Path -LiteralPath $BuildRoot) `
        $metadata.name
}
# Create build job definitions from dotnet project in current folder
$projFile = Get-ChildItem $Path -Filter *.csproj | Select-Object -First 1
if ($projFile) {

    # Create dotnet command line 
    $output = (Join-Path $BuildRoot `
        (Join-Path "bin" (Join-Path "publish" $configuration)))

    $argumentList = @("clean", $projFile.FullName)
    & dotnet $argumentList 2>&1 | ForEach-Object { $_ | Out-Null }
    Remove-Item $output -Recurse -ErrorAction SilentlyContinue

    $runtimes = @(
        "linux-musl-arm",
        "linux-musl-arm64",
        "linux-musl-x64",
        ""
    )
    if (![string]::IsNullOrEmpty($metadata.base)) {
        # Shortcut - only build portable
        $runtimes = @("")
    }
    $runtimes | ForEach-Object {
        $runtimeId = $_

        # Only build portable, windows and linux in fast mode
        if ($script:Fast.IsPresent -and ![string]::IsNullOrEmpty($runtimeId)) {
            if (($runtimeId -ne "win-x64") -and ($runtimeId -ne "linux-musl-x64")) {
                return;
            }
            # if not iot edge, just build linux in addition to portable.
            if ((!$metadata.iotedge) -and ($runtimeId -ne "linux-musl-x64")) {
                return;
            }
        }

        # Create dotnet command line 
        $argumentList = @("publish", "-c", $configuration, "--force")
        if (![string]::IsNullOrEmpty($runtimeId)) {
            $argumentList += "--self-contained"
            $argumentList += "-r"
            $argumentList += $runtimeId
            $argumentList += "/p:TargetLatestRuntimePatch=true"
            $argumentList += "/p:PublishSingleFile=true"
        }
        else {
            $runtimeId = "portable"
        }
        $argumentList += "-o"
        $argumentList += (Join-Path $output $runtimeId)
        $argumentList += $projFile.FullName

        Write-Host "Publish $($projFile.FullName) with $($runtimeId) runtime..."
        & dotnet $argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
        if ($LastExitCode -ne 0) {
            throw "Error: 'dotnet $($argumentList)' failed with $($LastExitCode)."
        }
    }

    # Get project's assembly name to create entry point entry in dockerfile
    $assemblyName = $null
    ([xml] (Get-Content -Path $projFile.FullName)).Project.PropertyGroup `
        | Where-Object { ![string]::IsNullOrWhiteSpace($_.AssemblyName) } `
        | Foreach-Object { $assemblyName = $_.AssemblyName }
    if ([string]::IsNullOrWhiteSpace($assemblyName)) {
        $assemblyName = $projFile.BaseName
    }

    # Default platform definitions
    $platforms = @{
        "linux/arm" = @{
            runtimeId = "linux-musl-arm"
            image = "mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine"
            platformTag = "linux-arm32v7"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "linux/arm64" = @{
            runtimeId = "linux-musl-arm64"
            image = "mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine"
            platformTag = "linux-arm64v8"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "linux/amd64" = @{
            runtimeId = "linux-musl-x64"
            image = "mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine"
            platformTag = "linux-amd64"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            entryPoint = "[`"./$($assemblyName)`"]"
        }
    }

    # Create dockerfile in publish output and build definitions
    $platforms.Keys | ForEach-Object {
        $platformInfo = $platforms.Item($_)

        $runtimeId = $platformInfo.runtimeId
        $baseImage = $platformInfo.image
        $platformTag = $platformInfo.platformTag
        $entryPoint = $platformInfo.entryPoint
        $environmentVars = @("ENV DOTNET_RUNNING_IN_CONTAINER=true")

        if ($script:Fast.IsPresent) {
            # Just build linux images.
            if ($_ -ne "linux/amd64") {
                return;
            }
        }

        #
        # Check for overridden base image name - e.g. aspnet core images
        # this script only supports portable and defaults to dotnet entry 
        # point
        #
        if (![string]::IsNullOrEmpty($metadata.base)) {
            $baseImage = $metadata.base
            $runtimeId = $null
        }
        else {
            # TODO Remove after moving to latest messaging nugets
            $environmentVars += "RUN apk add icu-libs"
            $environmentVars += "ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false"
        }

        if ([string]::IsNullOrEmpty($runtimeId)) {
            $runtimeId = "portable"
        }

        $runtimeOnly = ""
        if (![string]::IsNullOrEmpty($platformInfo.runtimeOnly)) {
            $runtimeOnly = $platformInfo.runtimeOnly
        }

        if ($runtimeId -eq "portable") {
            $runtimeOnly = ""
            $entryPoint = "[`"dotnet`", `"$($assemblyName).dll`"]"
        }

        $exposes = ""
        if ($metadata.exposes -ne $null) {
            $metadata.exposes | ForEach-Object {
                $exposes = "$("EXPOSE $($_)" | Out-String)$($exposes)"
            }
            $environmentVars += "ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"
        }
        $workdir = ""
        if ($metadata.workdir -ne $null) {
            $workdir = "WORKDIR /$($metadata.workdir)"
        }
        if ([string]::IsNullOrEmpty($workdir)) {
            $workdir = "WORKDIR /app"
        }
        $dockerFileContent = @"
FROM $($baseImage)

$($exposes)

$($workdir)
COPY . .
$($runtimeOnly)

$($environmentVars | Out-String)

ENTRYPOINT $($entryPoint)

"@ 
        $imageContent = (Join-Path $output $runtimeId)
        $dockerFile = (Join-Path $output "Dockerfile.$($platformTag)")
        Write-Host Writing $($dockerFile)
        # $dockerFileContent | Out-Host
        $dockerFileContent | Out-File -Encoding ascii -FilePath $dockerFile

        $definitions += @{
            platform = $_
            dockerfile = $dockerFile
            baseImage = $baseImage
            platformTag = $platformTag
            buildContext = $imageContent
        }
    }
}

return $definitions
