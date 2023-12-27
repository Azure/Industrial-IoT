<#
 .SYNOPSIS
    Builds a manifest list from images produced as part of
    a full build of all csproj files. A full build includes
    building linux-arm, linux-arm64, linux-x64, etc.

 .PARAMETER Registry
    The name of the container registry to push to (optional)
 .PARAMETER User
    The user name to use when pushing.
 .PARAMETER Pw
    The password used when pushing.
 .PARAMETER ImageNamespace
    The namespace to use for the image inside the registry.
 .PARAMETER ImageTag
    Image tags to combine into manifest. Defaults to "latest"
 .PARAMETER PublishTags
    Comma seperated tags to publish. Defaults to Tag

 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.
 .PARAMETER NoBuid
    If set does not build but just packages the images into a
    manifest list
#>

Param(
    [string] $Registry = $null,
    [string] $User = $null,
    [string] $Pw = $null,
    [string] $ImageNamespace = $null,
    [string] $ImageTag = "latest",
    [string] $PublishTags = $null,
    [switch] $Debug,
    [switch] $NoBuild
)

$ErrorActionPreference = "Stop"

if (!$script:PublishTags) {
    $script:PublishTags = "latest"
    if ($script:ImageTag -ne "latest") {
        $script:PublishTags = "$($script:PublishTags),$($script:ImageTag)"
    }
}
$platforms = @{
    "linux" = @( "x64", "arm64", "arm")
}

$Path = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "Industrial-IoT.sln"

# Build manifest using the manifest tool
$manifestFile = New-TemporaryFile
$url = "https://github.com/estesp/manifest-tool/releases/download/v1.0.0/"
if ($env:OS -eq "windows_nt") {
    $manifestTool = "manifest-tool-windows-amd64.exe"
}
else {
    $manifestTool = "manifest-tool-linux-amd64"
}
$manifestToolPath = Join-Path $PSScriptRoot $manifestTool
while ($true) {
    try {
        # Download and verify manifest tool
        $wc = New-Object System.Net.WebClient
        $url += $manifestTool
        Write-Host "Downloading $($manifestTool)..."
        $wc.DownloadFile($url, $manifestToolPath)

        if (Test-Path $manifestToolPath) {
            break
        }
    }
    catch {
        Write-Warning "Failed to download $($manifestTool) - try again..."
        Start-Sleep -s 3
    }
}

if (-not $script:NoBuild.IsPresent) {
    $loggedIn = $false
    if ($script:Registry -and ($script:User -and $script:Pw)) {
        Write-Host "Logging into $($script:Registry) with $($script:Pw)..."
        (& docker login $script:Registry -u $script:User -p $script:Pw) | Out-Null
        $loggedIn = ($LastExitCode -ne 0)
        Write-Host "Pushing to $($script:Registry)..."
    }

    # Build all platforms
    $platforms.Keys | ForEach-Object {
        $os = $_
        $platforms.Item($_) | ForEach-Object {
            $arch = $_

            Write-Host "Publish containers for $os-$arch..."
            # Build the docker images and push them to acr
            & (Join-Path $PSScriptRoot "publish.ps1") -Registry $($script:Registry) `
                -ImageNamespace $script:ImageNamespace -ImageTag $script:ImageTag `
                -Os $os -Arch $arch -Debug:$script:Debug
            if ($LastExitCode -ne 0) {
                throw "Failed to publish containers for $os-$arch."
            }
        }
    }
    if ($loggedIn) {
        docker logout $script:Registry
    }
}

try {
    # Find all container projects, publish them and then push to container registry
    Get-ChildItem $Path -Filter *.csproj -Recurse | ForEach-Object {
        $projFile = $_
        $properties = ([xml] (Get-Content -Path $projFile.FullName)).Project.PropertyGroup `
            | Where-Object { ![string]::IsNullOrWhiteSpace($_.ContainerRepository) } `
            | Select-Object -First 1
        if ($properties) {
            $fullName = ""
            if ($script:Registry) {
                $fullName = "$($script:Registry)/"
            }
            if ($script:ImageNamespace) {
                $fullName = "$($fullName)$($script:ImageNamespace)/"
            }
            $fullName = "$($fullName)$($properties.ContainerRepository)"
            $tagPostfix = ""
            if ($script:Debug.IsPresent) {
                $tagPostfix = "-debug"
            }

            $manifest = @"
image: $($fullName)
tags: [$($script:PublishTags)]
manifests:
"@
            $platforms.Keys | ForEach-Object {
                $os = $_
                $platforms.Item($_) | ForEach-Object {
                    $arch = $_
                    $architecture = $arch
                    if ($architecture -eq "x64") {
                        $architecture = "amd64"
                    }

        # Append to manifest
        if (![string]::IsNullOrEmpty($os)) {
            $manifest += @"

  - 
    image: $($fullName):$($script:ImageTag)-$($os)-$($arch)$($tagPostfix)
    platform: 
      os: $($os)
      architecture: $($architecture)
"@
    }
                }
            }

            Write-Host "Building and pushing manifest file:"
            Write-Host
            $manifest | Out-Host
            $manifest | Out-File -Encoding ascii -FilePath $manifestFile.FullName
            $argumentList = @()
            if ($script:User) {
                $argumentList += "--username"
                $argumentList += $script:User
            }
            if ($script:Pw) {
                $argumentList += "--password"
                $argumentList += $script:Pw
            }
            $argumentList += "push"
            $argumentList += "from-spec"
            $argumentList += $manifestFile.FullName
            while ($true) {
                (& $manifestToolPath $argumentList) | Out-Host
                if ($LastExitCode -eq 0) {
                    break
                }
                Write-Warning "Manifest push failed - try again."
                Start-Sleep -s 2
            }
    Write-Host "Manifest $($fullName) successfully pushed with tags $($script:PublishTags)."
        }
    }
}
catch {
    throw $_.Exception
}
finally {
    Remove-Item -Force -Path $manifestFile.FullName
    Remove-Item -Force -Path $manifestToolPath
}
