<#
    .SYNOPSIS
        Build a debug version of OPC Publisher and make it available
        in the local AIO cluster.
    .NOTES
        DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
        development and testing purposes only.

    .PARAMETER Configuration
        The build configuration to use. Default is "Debug".
    .PARAMETER ClusterType
        The type of Kubernetes cluster to use. Default is "microk8s".
#>

param(
    [string] [ValidateSet(
        "Debug",
        "Release"
    )] $Configuration = "Debug",
    [string] [ValidateSet(
        "kind",
        "minikube",
        "k3d",
        "microk8s"
    )] $ClusterType = "microk8s",
    [switch] $StartDebugger
)

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path
Import-Module $(Join-Path $(Join-Path $(Split-Path $scriptDirectory) "common") `
    "cluster-utils.psm1") -Force

$projFile = "Azure.IIoT.OpcUa.Publisher.Module"
$projFile = "../../../src/$($projFile)/src/$($projFile).csproj"

$containerName = "iotedge/opc-publisher"
$containerTag = Get-Date -Format "MMddHHmmss"
$containerImage = "$($containerName):$($containerTag)"
Write-Host "Publishing $Configuration OPC Publisher as $containerImage..." `
    -ForegroundColor Cyan
dotnet clean $projFile -c $Configuration
dotnet restore $projFile -s https://api.nuget.org/v3/index.json
dotnet publish $projFile -c $Configuration --self-contained false --no-restore `
    /t:PublishContainer -r linux-x64 /p:ContainerImageTag=$($containerTag)
if (-not $?) {
    Write-Host "Error building opc publisher connector." -ForegroundColor Red
    exit -1
}
Write-Host "$Configuration container image $containerImage published successfully." `
    -ForegroundColor Green

Import-ContainerImage -ClusterType $script:ClusterType -ContainerImage $containerImage
if ($StartDebugger) {
    ./debug.ps1 -Image $containerImage
}
