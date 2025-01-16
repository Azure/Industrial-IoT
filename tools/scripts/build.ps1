<#
 .SYNOPSIS
    Builds csproj files with container support into docker
    images. Linux docker images will be Alpine which we are
    officially supporting due to their attack surface. This
    is compared to the default images when publishing, which
    are debian.

 .PARAMETER ContainerRegistry
    The name of the container registry to push to (optional)
 .PARAMETER Os
    Operating system to build for. Defaults to Linux
 .PARAMETER Arch
    Architecture to build. Defaults to x64
 .PARAMETER ImageTag
    Tag to publish under. Defaults "latest"

 .PARAMETER BranchName
    The branch to use for the repo inside the registry.
 .PARAMETER NoBuild
    Whether to not build before publishing.
 .PARAMETER NoPublish
    Whether to not publish but build.
 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.
 .PARAMETER TarFileOutput
    Create tar.gz file instead of container
#>

Param(
    [string] $ContainerRegistry = $null,
    [string] $BranchName,
    [string] $Os = "linux",
    [string] $Arch = "x64",
    [string] $ImageTag = "latest",
    [switch] $NoBuild,
    [switch] $NoPublish,
    [switch] $Debug,
    [string] $TarFileOutput
)

$ErrorActionPreference = "Stop"

$Path = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "Industrial-IoT.sln"

$configuration = "Release"
if ($script:Debug.IsPresent) {
    $configuration = "Debug"
}

$imageNamespace = $null
if (![string]::IsNullOrWhiteSpace($script:ContainerRegistry)) {
    $imageNamespace = "public"

    if ($script:ContainerRegistry -eq "industrialiotdev") { 
        if (![string]::IsNullOrWhiteSpace($script:BranchName)) {
            # Set namespace name based on branch name
            $namespace = $script:BranchName
            if ($namespace.StartsWith("feature/")) {
                # dev feature builds
                $namespace = $namespace.Replace("feature/", "")
            }
            $namespace = $namespace.Replace("_", "/")
            $imageNamespace = $namespace.Substring(0, [Math]::Min($namespace.Length, 24))
        }
    } 
}

$env:DOTNET_CONTAINER_REGISTRY_CHUNKED_UPLOAD = $true
$env:DOTNET_CONTAINER_REGISTRY_CHUNKED_UPLOAD_SIZE_BYTES = 131072
$env:DOTNET_CONTAINER_REGISTRY_PARALLEL_UPLOAD = $false

# legacy variables for dotnet SDK version < 8.0.400
$env:SDK_CONTAINER_REGISTRY_CHUNKED_UPLOAD = $true
$env:SDK_CONTAINER_REGISTRY_CHUNKED_UPLOAD_SIZE_BYTES = 131072
$env:SDK_CONTAINER_REGISTRY_PARALLEL_UPLOAD = $false

# Find all container projects, publish them and then push to container registry
Get-ChildItem $Path -Filter *.csproj -Recurse | ForEach-Object {
    $projFile = $_
    $properties = ([xml] (Get-Content -Path $projFile.FullName)).Project.PropertyGroup `
        | Where-Object { ![string]::IsNullOrWhiteSpace($_.ContainerRepository) } `
        | Select-Object -First 1
    if ($properties) {
        $runtimeId = "$($script:Os)-$($script:Arch)"
        if ($script:Arch -eq "arm") {
            # Because of alpine
	        $runtimeId = "$($script:Os)-musl-$($script:Arch)"
	    }

        if (!$script:NoBuild.IsPresent) {
            Write-Host "Build $($projFile.FullName) ..."

            dotnet clean $projFile.FullName -c $configuration `
                -r $runtimeId | Out-Null
            dotnet build $projFile.FullName -c $configuration `
                -r $runtimeId /p:TargetLatestRuntimePatch=true

            if ($LastExitCode -ne 0) {
                throw "Failed to build container."
            }
        }
        if ($script:NoPublish.IsPresent) {
            return
        }

        $fullName = ""
        $extra = @()

        if ($imageNamespace) {
            $fullName = "$($fullName)$($imageNamespace)/"
        }
        $fullName = "$($fullName)$($properties.ContainerRepository)"

        $fullTag = "$($script:ImageTag)-$($script:Os)-$($script:Arch)"
        if ($script:Debug.IsPresent) {
            $fullTag = "$($fullTag)-debug"
        }

        Write-Host "Publish $($projFile.FullName) as $($fullName):$($fullTag)..."
        $baseImage = $($properties.ContainerBaseImage -split "-")[0]

        # see architecture tags e.g., here https://hub.docker.com/_/microsoft-dotnet-aspnet
        if ($script:Arch -eq "x64") {
	        $baseImage = "$($baseImage)-azurelinux3.0-amd64"
	    }
	    if ($script:Arch -eq "arm64") {
	        $baseImage = "$($baseImage)-azurelinux3.0-arm64v8"
	    }
	    if ($script:Arch -eq "arm") {
	        $baseImage = "$($baseImage)-alpine-arm32v7"
	    }

        if (![string]::IsNullOrWhiteSpace($script:TarFileOutput)) {
            Write-Host "Publish as tarball to $($script:TarFileOutput)/$($fullName).tar.gz..."
            $extra += "/p:ContainerArchiveOutputPath=$($script:TarFileOutput)/$($fullName).tar.gz"
        }
        elseif ($script:ContainerRegistry) {
            Write-Host "Publish to container registry $($script:ContainerRegistry)..."
            $extra += "/p:ContainerRegistry=$($script:ContainerRegistry)"
        }

        dotnet publish $projFile.FullName -c $configuration --self-contained false --no-build `
            -r $runtimeId /p:TargetLatestRuntimePatch=true `
            /p:RuntimeIdentifiers= `
            /p:ContainerBaseImage=$baseImage `
            /p:ContainerRepository=$($fullName) `
            /p:ContainerImageTag=$($fullTag) `
            $extra /t:PublishContainer
        if ($LastExitCode -ne 0) {
            throw "Failed to publish container."
        }

        Write-Host "$($fullName):$($fullTag) published."
    }
}
