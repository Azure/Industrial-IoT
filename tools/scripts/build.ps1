<#
 .SYNOPSIS
    Builds csproj files with container support into docker images

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
 .PARAMETER Tag
    Tag to publish under. Defaults to version or "latest"

 .PARAMETER NoBuid
    Whether to build before publishing.
 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.  
#>

Param(
    [string] $Registry = $null,
    [switch] $Push,
    [string] $ImageNamespace = $null,
    [string] $Os = "linux",
    [string] $Arch = "x64",
    [string] $Tag = $null,
    [switch] $NoBuild,
    [switch] $Debug
)

$Path = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "Industrial-IoT.sln"

$configuration = "Release"
if ($script:Debug.IsPresent) {
    $configuration = "Debug"
}

if ([string]::IsNullOrEmpty($script:Tag)) {
    try {
        $version = & (Join-Path $PSScriptRoot "get-version.ps1")
        $script:Tag = $version.Prefix
    }
    catch {
        $script:Tag = "latest"
    }
}

$runtimes = @{
    "linux" = @{
        "arm" = "linux-musl-arm"
        "arm64" = "linux-musl-arm64"
        "x64" = "linux-musl-x64"
    }
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

        $fullTag = "$($script:Tag)-$($script:Os)-$($script:Arch)"
        if ($script:Debug.IsPresent) {
            $fullTag = "$($fullTag)-debug"
        }

        Write-Host "Publish $($projFile.FullName) as $($fullName):$($fullTag)..."
        $extra = @()
        if ($script:NoBuild) {
            $extra += "--no-build"
        }
        # add -r to select musl runtime?
        dotnet publish $projFile.FullName -c $configuration `
            --os $script:Os --arch $script:Arch `
            /p:TargetLatestRuntimePatch=true `
            /p:ContainerImageName=$fullName `
            /p:ContainerImageTag=$fullTag `
            /t:PublishContainer $extra

        if ($script:Registry -and $script:Push.IsPresent) {
            docker push "$($fullName):$($fullTag)"
        }
        Write-Host "$($fullName):$($fullTag) published."
    }
}
