<#
 .SYNOPSIS
    Builds csproj file and returns buildable dockerfile build definitions

 .PARAMETER path
    The folder to build the docker files from

 .PARAMETER debug
    Build debug and include debugger into images (where applicable)
#>

Param(
    [string] $path = $null,
    [string] $configuration = "Release"
)

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

# Get meta data
$metadata = Get-Content -Raw -Path (join-path $path "mcr.json") `
    | ConvertFrom-Json

$definitions = @()

# Create build job definitions from dotnet project in current folder
$projFile = Get-ChildItem $path -Filter *.csproj | Select-Object -First 1
if ($projFile -ne $null) {

    $output = (join-path $path (join-path "bin" (join-path "publish" $configuration)))
    $runtimes = @("linux-arm", "linux-x64", "win-x64", "win-arm", "win-arm64", "")
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
"@
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
        "linux/arm/v7" = @{
            runtimeId = "linux-arm"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:2.2"
            platformTag = "linux-arm32v7"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            debugger = $installLinuxDebugger
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "linux/amd64" = @{
            runtimeId = "linux-x64"
            image = "mcr.microsoft.com/dotnet/core/runtime-deps:2.2"
            platformTag = "linux-amd64"
            runtimeOnly = "RUN chmod +x $($assemblyName)"
            debugger = $installLinuxDebugger
            entryPoint = "[`"./$($assemblyName)`"]"
        }
        "windows/amd64:10.0.17134.885" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1803"
            platformTag = "nanoserver-amd64-1803"
            debugger = $null
            entryPoint = "[`"$($assemblyName).exe`"]"
        }
        "windows/amd64:10.0.17763.615" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1809"
            platformTag = "nanoserver-amd64-1809"
            debugger = $null
            entryPoint = "[`"$($assemblyName).exe`"]"
        }
        "windows/arm" = @{
            runtimeId = "win-arm"
            image = "mcr.microsoft.com/windows/nanoserver:1809-arm32v7"
            platformTag = "nanoserver-arm32v7-1809"
            debugger = $null
            entryPoint = "[`"$($assemblyName).exe`"]"
        }
        "windows/amd64:10.0.18362.239" = @{
            runtimeId = "win-x64"
            image = "mcr.microsoft.com/windows/nanoserver:1903"
            platformTag = "nanoserver-amd64-1903"
            debugger = $null
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

        $debugger = ""
        if ($configuration -eq "Debug") {
            if (![string]::IsNullOrEmpty($platformInfo.debugger)) {
                $debugger = $platformInfo.debugger
            }
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
        }
        $dockerFileContent = @"
FROM $($baseImage)
$($exposes)

WORKDIR /app
COPY . .
$($runtimeOnly)

$($debugger)

ENTRYPOINT $($entryPoint)
"@ 
        $imageContent = (join-path $output $runtimeId)
        $dockerFile = (join-path $imageContent "Dockerfile.$($platformTag)")
        Write-Host Writing $($dockerFile)
        $dockerFileContent | Out-Verbose
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
    Get-ChildItem $path -Recurse `
        | Where-Object Name -eq "Dockerfile" `
        | ForEach-Object {

        $dockerfile = $_.FullName

        $platformFolder = $_.DirectoryName.Replace($path, "")
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
