<#
 .SYNOPSIS
    Builds csproj files with container support into docker 
    images. Linux docker images will be Alpine which we are
    officially supporting due to their attack surface. This
    is compared to the default images when publishing, which 
    are debian.

 .PARAMETER Registry
    The name of the container registry to push to (optional)
 .PARAMETER Push
    Whether to push the image to the registry
 .PARAMETER ImageNamespace
    The namespace to use for the image inside the registry.

 .PARAMETER Os
    Operating system to build for. Defaults to Linux
 .PARAMETER Arch
    Architecture to build. Defaults to x64
 .PARAMETER ImageTag
    Tag to publish under. Defaults "latest"

 .PARAMETER NoBuid
    Whether to not build before publishing.
 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.  
#>

Param(
    [string] $Registry = $null,
    [switch] $Push,
    [string] $ImageNamespace = $null,
    [string] $Os = "linux",
    [string] $Arch = "x64",
    [string] $ImageTag = "latest",
    [switch] $NoBuild,
    [switch] $Debug
)

$ErrorActionPreference = "Stop"

$Path = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "Industrial-IoT.sln"

$configuration = "Release"
if ($script:Debug.IsPresent) {
    $configuration = "Debug"
}

# Find all container projects, publish them and then push to container registry
Get-ChildItem $Path -Filter *.csproj -Recurse | ForEach-Object {
    $projFile = $_
    $properties = ([xml] (Get-Content -Path $projFile.FullName)).Project.PropertyGroup `
        | Where-Object { ![string]::IsNullOrWhiteSpace($_.ContainerImageName) } `
        | Select-Object -First 1
    if ($properties) {
        $fullName = ""
        if ($script:Registry) {
            $fullName = "$($script:Registry)/"
        }
        if ($script:ImageNamespace) {
            $fullName = "$($fullName)$($script:ImageNamespace)/"
        }
        $fullName = "$($fullName)$($properties.ContainerImageName)"

        $fullTag = "$($script:ImageTag)-$($script:Os)-$($script:Arch)"
        if ($script:Debug.IsPresent) {
            $fullTag = "$($fullTag)-debug"
        }

        Write-Host "Publish $($projFile.FullName) as $($fullName):$($fullTag)..."
        $extra = @()
        if ($script:NoBuild) {
            $extra += "--no-build"
        }
        
        $baseImage = "$($properties.ContainerBaseImage)"
        if ($os -eq "linux") {
            $architecture = $arch
            # see architecture tags e.g., here https://hub.docker.com/_/microsoft-dotnet-aspnet
            if ($arch -eq "x64"){
                $architecture = "amd64"
            }
            if ($arch -eq "arm64") {
                $architecture = "arm64v8"
            }
            if ($arch -eq "arm") {
                $architecture = "arm32v7"
            }

            # repoint to alpine images for all builds
            # https://github.com/dotnet/sdk-container-builds/blob/main/docs/ContainerCustomization.md
            if ($baseImage -like "*-alpine") {
                $baseImage = "$($baseImage)-$($architecture)"
            }
            else {
                $baseImage = "$($baseImage)-alpine-$($architecture)"
            }
            $runtimeId = "$($os)-musl-$($arch)"
        }
        else {
            if ($os -eq "windows") {
                $runtimeId = "win10"
            }
            $runtimeId = "portable"
        }

        # add -r to select musl runtime?
        dotnet publish $projFile.FullName -c $configuration --self-contained `
            -r $runtimeId /p:TargetLatestRuntimePatch=true `
            /p:ContainerImageName=$fullName /p:ContainerBaseImage=$baseImage `
            /p:ContainerImageTag=$fullTag `
            /t:PublishContainer $extra
        if ($LastExitCode -ne 0) {
            throw "Failed to publish container."
        }
        if ($script:Registry -and $script:Push.IsPresent) {
            Write-Host "Pushing $($fullName):$($fullTag) to registry..."
            docker push "$($fullName):$($fullTag)"
            if ($LastExitCode -ne 0) {
                throw "Failed to push container image."
            }
        }
        Write-Host "$($fullName):$($fullTag) published."
    }
}
