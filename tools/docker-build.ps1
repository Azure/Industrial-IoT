<#
 .SYNOPSIS
    Build docker container for a docker file in the tree.

 .PARAMETER image
    The folder to build the docker files from which also contains
    the mcr.json file.

 .PARAMETER path
    The folder to build the docker files from which also contains
    the mcr.json file.

 .PARAMETER debug
    Build debug images
#>

Param(
    [string] $path = ".",
    [string] $image,
    [switch] $debug
)

if ([string]::IsNullOrEmpty($image)) {
    throw "Must specifiy an image name"
}

Write-Host "Building $($image)"

# Source definitions
$configuration = "Release"
if ($debug.IsPresent) {
    $configuration = "Debug"
}
$scriptPath = (Join-Path $PSScriptRoot "docker-source.ps1")
$definitions = & $scriptPath -path $path -configuration $configuration

# Get currently active platform 
$dockerversion = &docker @("version") 2>&1 | %{ "$_" } `
    | ConvertFrom-Csv -Delimiter ':' -Header @("Key", "Value") `
    | Where-Object { $_.Key -eq "OS/Arch" } `
    | ForEach-Object { $platform = $_.Value }

if ([string]::IsNullOrEmpty($platform)) {
    $platform = "linux/amd64"
}
if ($platform -eq "windows/amd64") {
    $osVerToPlatform = @{
        "10.0.17134" = "windows/amd64:10.0.17134.885" 
        "10.0.17763" = "windows/amd64:10.0.17763.615" 
        "10.0.18362" = "windows/amd64:10.0.17134.885"
    }
    $osver = (Get-WmiObject Win32_OperatingSystem).Version
    $platform = $osVerToPlatform.Item($osver)
}

# Select build definition
$def = $definitions `
    | Where-Object { $_.platform -eq $platform } `
    | Select-Object

# Build docker image from definition
$dockerfile = $def.dockerfile
$buildContext = $def.buildContext

# Create docker build command line 
$argumentList = @("build", 
    "-f", $dockerfile,
    "-t", "$($image):latest"
)
$argumentList += $buildContext
& docker $argumentList 2>&1 | %{ "$_" }
if ($LastExitCode -ne 0) {
    throw "Error: 'docker $($args)' failed with $($LastExitCode)."
}
