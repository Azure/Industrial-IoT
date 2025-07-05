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

Write-Host "Importing $containerImage into $($script:ClusterType) cluster..." `
    -ForegroundColor Cyan
if ($script:ClusterType -eq "microk8s") {
    # Windows only, support on linux is not implemented yet
    $imageTar = Join-Path $env:TEMP "image.tar"
    docker image save $containerImage -o $imageTar
    multipass transfer $imageTar microk8s-vm:/tmp/image.tar
    microk8s ctr image import /tmp/image.tar
    Remove-Item -Path $imageTar -Force
    docker image rm -f $containerImage
    $containerImage = "docker.io/$containerImage"
}
elseif ($script:ClusterType -eq "k3d") {
    # import in all clusters which avoids asking for the cluster name
    $clusters = $(& { k3d cluster list --no-headers } -split ")`n") `
        | ForEach-Object { $($_ -split " ")[0].Trim() }
    k3d image import $containerImage `
        --mode auto `
        --cluster $($clusters -join ",")
}
elseif ($script:ClusterType -eq "kind") {
    kind load docker-image $containerImage
}
Write-Host "Container $containerImage imported into $($script:ClusterType) cluster." `
    -ForegroundColor Green

if ($StartDebugger) {
    ./debug.ps1 -ReplaceImage $containerImage
}
