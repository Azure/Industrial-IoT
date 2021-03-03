<#
 .SYNOPSIS
    Builds csproj file and returns buildable dockerfile build definitions

 .PARAMETER Path
    The folder containing the container.json file.

 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.  
    Debug also includes debugger into images (where applicable).
#>

Param(
    [string] $Path = $null,
    [switch] $Debug
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
if ($Debug.IsPresent) {
    $configuration = "Debug"
}
$metadata = Get-Content -Raw -Path (Join-Path $Path "container.json") `
    | ConvertFrom-Json

$definitions = @()

# Create build job definitions from dotnet project in current folder
$projFile = Get-ChildItem $Path -Filter *.csproj | Select-Object -First 1
if ($projFile) {

    $output = (Join-Path $Path (Join-Path "bin" (Join-Path "publish" $configuration)))
    $runtimes = @("linux-arm", "linux-arm64", "linux-x64", "win-x64", "")
    if (![string]::IsNullOrEmpty($metadata.base)) {
        # Shortcut - only build portable
        $runtimes = @("")
    }
    $runtimes | ForEach-Object {
        $runtimeId = $_

        # Create dotnet command line 
        $argumentList = @("publish", "-c", $configuration)
        if (![string]::IsNullOrEmpty($runtimeId)) {
            $argumentList += "-r"
            $argumentList += $runtimeId
            $argumentList += "/p:TargetLatestRuntimePatch=true"
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
            runtimeId = "linux-arm"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:3.1"
            platformTag = "linux-arm32v7"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "linux/arm64" = @{
            runtimeId = "linux-arm64"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine-arm64v8"
            platformTag = "linux-arm64v8"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "linux/amd64" = @{
            runtimeId = "linux-x64"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine"
            platformTag = "linux-amd64"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "windows/amd64:10.0.17763.1457" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1809"
            platformTag = "nanoserver-amd64-1809"
            entryPoint = "[`"$($assemblyName).exe`"]"
        }
        "windows/amd64:10.0.18363.1082" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1909"
            platformTag = "nanoserver-amd64-1909"
            entryPoint = "[`"$($assemblyName).exe`"]"
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

        #
        # Check for overridden base image name - e.g. aspnet core images
        # this script only supports portable and defaults to dotnet entry 
        # point
        #
        if (![string]::IsNullOrEmpty($metadata.base)) {
            $baseImage = $metadata.base
            $runtimeId = $null
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
        $dockerFile = (Join-Path $imageContent "Dockerfile.$($platformTag)")
        Write-Host Writing $($dockerFile)
        # $dockerFileContent | Out-Host
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
    # Non-.net - Create job definitions from dockerfile structure in current folder
    Get-ChildItem $Path -Recurse `
        | Where-Object Name -eq "Dockerfile" `
        | ForEach-Object {

        $dockerfile = $_.FullName

        $platformFolder = $_.DirectoryName.Replace($Path, "")
        $platform = $platformFolder.Substring(1).Replace("\", "/")
        $platformTag = $platform.Replace("/", "-")

        $definitions += @{
            dockerfile = $dockerfile
            platform = $platform
            platformTag = $platformTag
            buildContext = $buildRoot
        }
    }
}

return $definitions
