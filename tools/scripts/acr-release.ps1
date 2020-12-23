<#
 .SYNOPSIS
    Tags all images with a particular version as release images

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    Releases images with a given version number.

 .PARAMETER Registry
    The name of the registry

 .PARAMETER Subscription
    The subscription to use 

 .PARAMETER Version
    The version to release 
#>

Param(
    [string] $Registry = "industrialiot",
    [string] $Subscription = "IOT_GERMANY",
    [Parameter(Mandatory = $true)]
    [string] $Version
)

# set default subscription
if (![string]::IsNullOrEmpty($Subscription)) {
    Write-Debug "Setting subscription to $($Subscription)"
    $argumentList = @("account", "set", "--subscription", $Subscription)
    & "az" $argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# get credentials
$argumentList = @("acr", "credential", "show", "--name", $Registry)
$credentials = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$user = $credentials.username
$password = $credentials.passwords[0].value
Write-Host "Using User name $($user) and passsword ****"

# Default platform definitions - keep in sync with docker-source.ps1
$platforms = @{
    "linux/arm" = @{
        platformTag = "linux-arm32v7"
    }
    "linux/arm64" = @{
        platformTag = "linux-arm64v8"
    }
    "linux/amd64" = @{
        platformTag = "linux-amd64"
    }
    "windows/amd64:10.0.17763.1457" = @{
        platformTag = "nanoserver-amd64-1809"
    }
    "windows/amd64:10.0.18363.1082" = @{
        platformTag = "nanoserver-amd64-1909"
    }
}

# Get manifest tool
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
    $BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"
    # Traverse from build root and find all container.json metadata files for images to release
    Get-ChildItem $BuildRoot -Recurse -Include "container.json" | ForEach-Object {

        $metadata = Get-Content -Raw -Path $_.FullName | ConvertFrom-Json

        # Identify release image
        $imageName = $metadata.name
        $tagPrefix = ""

        if (![string]::IsNullOrEmpty($metadata.tag)) {
            $tagPrefix = "$($metadata.tag)-"
        }

        # Create manifest file
        $tags = @()
        $versionParts = $script:Version.Split('.')
        if ($versionParts.Count -gt 0) {
            $versionTag = $versionParts[0]
            $tags += "$($tagPrefix)$($versionTag)"
            # do not re-push version tag
            for ($i = 1; $i -lt ($versionParts.Count - 1); $i++) {
                $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
                $tags += "$($tagPrefix)$($versionTag)"
            }
            $tagList = ("'{0}'" -f ($tags -join "', '"))
        }

        $fullImageName = "$($Registry).azurecr.io/public/$($imageName):$($tagPrefix)latest"
        Write-Host "Building release manifest for $($fullImageName)..."

        $manifest = @" 
image: $($fullImageName)
tags: [$($tagList)]
manifests:
"@
        # Create new manifest file
        $platforms.Keys | ForEach-Object {
            $platformInfo = $platforms.Item($_)
        
            $platform = $_.ToLower()
            $platformTag = $platformInfo.platformTag.ToLower()
        
            $os = ""
            $osVersion = ""
            $osVerArr = $platform.Split(':')
            if ($osVerArr.Count -gt 1) {
                # --platform argument must be without os version
                $platform = $osVerArr[0]
                $osVersion = ("osversion: {0}" -f $osVerArr[1])
            }
        
            $osArchArr = $platform.Split('/')
            if ($osArchArr.Count -gt 1) {
                $os = ("os: {0}" -f $osArchArr[0])
                $architecture = ("architecture: {0}" -f $osArchArr[1])
                $variant = ""
                if ($osArchArr.Count -gt 2) {
                    $variant = ("variant: {0}" -f $osArchArr[2])
                }
            }
            # Append to manifest
            if (![string]::IsNullOrEmpty($os)) {
                $manifest +=
                @"

  - 
    image: $($Registry).azurecr.io/public/$($imageName):$($tagPrefix)$($Version)-$($platformTag)
    platform: 
    $($os)
    $($architecture)
    $($osVersion)
    $($variant)
"@
            }
        }

        Write-Host
        $manifest | Out-Host
        $manifest | Out-File -Encoding ascii -FilePath $manifestFile.FullName
        Write-Host
        try {
            $argumentList = @( 
                "--username", $user,
                "--password", $password,
                "push",
                "from-spec", $manifestFile.FullName
            )
            while ($true) {
                Write-Host "Pushing release manifest for $($fullImageName)..."
                (& $manifestToolPath $argumentList) | Out-Host
                if ($LastExitCode -eq 0) {
                    break   
                }
                Write-Warning "Manifest push failed - try again."
                Start-Sleep -s 2
            }
            Write-Host "Manifest pushed successfully."
        }
        catch {
            throw $_.Exception
        }
        finally {
            Remove-Item -Force -Path $manifestFile.FullName
        }
    }
}
finally {
    Remove-Item -Force -Path $manifestToolPath
}