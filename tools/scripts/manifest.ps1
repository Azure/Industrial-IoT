<#
 .SYNOPSIS
    Builds csproj files with container support into docker images

 .PARAMETER Registry
    The name of the container registry to push to (optional)
 .PARAMETER User
    The user name to use when pushing.
 .PARAMETER Pw
    The password used when pushing.
 .PARAMETER ImageNamespace
    The namespace to use for the image inside the registry.
 .PARAMETER Tag
    Tag to publish under. Defaults to version or "latest"
 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.  
#>

Param(
    [string] $Registry = $null,
    [string] $User = $null,
    [string] $Pw = $null,
    [string] $ImageNamespace = $null,
    [string] $Tag = $null,
    [switch] $Debug
)

$platforms = @{
    "linux" = @( "x64", "arm", "arm64")
}

$Path = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "Industrial-IoT.sln"

if ([string]::IsNullOrEmpty($script:Tag)) {
    try {
        $version = & (Join-Path $PSScriptRoot "get-version.ps1")
        $script:Tag = $version.Prefix
    }
    catch {
        $script:Tag = "latest"
    }
}

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

try {
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
            $tagPostfix = ""
            if ($script:Debug.IsPresent) {
                $tagPostfix = "-debug"
            }

            $manifest = @"
image: $($fullName)
tags: [$($script:Tag)$($tagPostfix)]
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
    image: $($fullName):$($script:Tag)-$($os)-$($arch)$($tagPostfix)
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
            Write-Host "Manifest push $($fullName):$($script:Tag)$($tagPostfix) successful."
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
