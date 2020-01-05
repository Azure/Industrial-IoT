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

# log into registry
$argumentList = @("acr", "login", "--name", $Registry)
& az $argumentList 
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}

# Create list of release tags
$tags = @()
$versionParts = $Version.Split('.')
if ($versionParts.Count -gt 0) {
    $versionTag = $versionParts[0]
    $tags += "$($tagPrefix)$($versionTag)$($tagPostfix)"
    for ($i = 1; $i -lt $versionParts.Count - 1; $i++) {
        $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
        $tags += "$($tagPrefix)$($versionTag)$($tagPostfix)"
    }
    $tags += "latest"
}

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

    $releaseImageName = "$($Registry).azurecr.io/$($imageName):$($tagPrefix)$($Version)"
    Write-Host "Pulling $($releaseImageName)..."
    $argumentList = @("pull", $releaseImageName)
    & docker $argumentList
    if ($LastExitCode -ne 0) {
        throw "docker $($argumentList) failed with $($LastExitCode)."
    }

    # tag release image with release tags
    $tags | ForEach-Object {
        $taggedImageName = "$($Registry).azurecr.io/$($imageName):$($tagPrefix)$($_)"

        Write-Host "Tagging $($releaseImageName) as $($_) and pushing..."
        $argumentList = @("tag", $releaseImageName, $taggedImageName)
        Write-Host "docker $($argumentList)"
        & docker $argumentList
        if ($LastExitCode -ne 0) {
            throw "docker $($argumentList) failed with $($LastExitCode)."
        }
        $argumentList = @("push", $taggedImageName)
        & docker $argumentList
        if ($LastExitCode -ne 0) {
            throw "docker $($argumentList) failed with $($LastExitCode)."
        }
    }

    Write-Host "Removing $($releaseImageName)..."
    $argumentList = @("image", "rm", "-f", $releaseImageName)
    & docker $argumentList
    if ($LastExitCode -ne 0) {
        throw "docker $($argumentList) failed with $($LastExitCode)."
    }
}