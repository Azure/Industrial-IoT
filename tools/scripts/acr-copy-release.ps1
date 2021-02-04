<#
 .SYNOPSIS
    Creates release images with a particular release version in production ACR from the tested development version. 

 .DESCRIPTION
    The script requires az to be installed and already logged on to a 
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    Releases images with a given version number.

 .PARAMETER BuildRegistry
    The name of the source registry where development image is present.

 .PARAMETER ReleaseRegistry
    The name of the destination registry where release images will be created.

 .PARAMETER Subscription
    The subscription to use 

 .PARAMETER BuildVersion
    The build version for the development image that is being released.

 .PARAMETER ReleaseVersion
    The version to release e.g. 2.7.206. This will be the release tag for the release images.
#>

Param(
    [string] $BuildRegistry = "industrialiot",
    [string] $ReleaseRegistry = "industrialiotprod",
    [string] $Subscription = "IOT_GERMANY",
    [Parameter(Mandatory = $true)]
    [string] $BuildVersion,
    [Parameter(Mandatory = $true)]
    [string] $ReleaseVersion
)

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

# set default subscription
if (![string]::IsNullOrEmpty($Subscription)) {
    Write-Debug "Setting subscription to $($Subscription)"
    $argumentList = @("account", "set", "--subscription", $Subscription)
    & "az" $argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# get build registry credentials
$argumentList = @("acr", "credential", "show", "--name", $BuildRegistry)
$sourceCredentials = (& "az" $argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$sourceUser = $sourceCredentials.username
$sourcePassword = $sourceCredentials.passwords[0].value
Write-Host "Using Source User name $($sourceUser) and password ****"

# Copy images from Build ACR to Release ACR
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

    Write-Host "Looking at imageName $($imageName)"

    # Create base image tags list
    # Example: if release version is 2.7.1, then base image tags are "2", "2.7", "2.7.1", "latest"
    $baseImageTags = @("latest")
    $versionParts = $script:ReleaseVersion.Split('.')
    if ($versionParts.Count -gt 0) {
        $versionTag = $versionParts[0]
        $baseImageTags += "$($tagPrefix)$($versionTag)"
        for ($i = 1; $i -lt ($versionParts.Count); $i++) {
            $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
            $baseImageTags += "$($tagPrefix)$($versionTag)"
        }
    }

    $imageTagsMap = @{
        "$($tagPrefix)$($BuildVersion)" = $baseImageTags
    }
    # Add platform specific tags to the tags list to copy
    $platforms.Keys | ForEach-Object {
        $platformInfo = $platforms.Item($_)
        $platformTag = $platformInfo.platformTag.ToLower()
        $imageTagsMap.Add("$($tagPrefix)$($BuildVersion)-$($platformTag)", @("$($tagPrefix)$($ReleaseVersion)-$($platformTag)"))
    }

    $jobs = @()
    # Copy images from Build ACR to Release ACR
    foreach ($sourceTag in $imageTagsMap.Keys) {
        $FullImageName = "$($BuildRegistry).azurecr.io/public/$($imageName):$($sourceTag)"

        $targetImageArgs = @()
        foreach ($targetTag in $imageTagsMap.Item($sourceTag)) {
            $targetImageArgs += "--image"
            $targetImageArgs += "$($imageName):$($targetTag)"
        }
        # Create acr command line 
        $argumentList = @("acr", "import", "--force",
            "--name", $ReleaseRegistry,
            "--source", $FullImageName) + $targetImageArgs +
            @("--username", $sourceUser,
            "--password", $sourcePassword)

        $jobs += Start-Job -Name $FullImageName -ArgumentList @($argumentList, $FullImageName) -ScriptBlock {
            $argumentList = $args[0]
            $FullImageName = $args[1]
            Write-Host "Copying image $($FullImageName)"
            & az $argumentList 2>&1 | ForEach-Object { "$_" }
            if ($LastExitCode -ne 0) {
                Write-Warning "Copy image failed for $($FullImageName) with $($LastExitCode) - 2nd attempt..."
                & az $argumentList 2>&1 | ForEach-Object { "$_" }
                if ($LastExitCode -ne 0) {
                    throw "Error: CopyImage- 2nd attempt failed for $($FullImageName) with $($LastExitCode)."
                }
            }
            Write-Host "Copy image completed for $($FullImageName)."
        }
    }  
    # Wait for copy jobs to finish for this repo.
    if ($jobs.Count -ne 0) {
        Write-Host "Waiting for copy jobs to finish for $($imageName)."
        # Wait until all jobs are completed
        Receive-Job -Job $jobs -WriteEvents -Wait | Out-Host
        $jobs | Out-Host
        $jobs | Where-Object { $_.State -ne "Completed" } | ForEach-Object {
            throw "ERROR: Copying $($_.Name). resulted in $($_.State)."
        }
    }
    Write-Host "All copy jobs completed successfully for $($imageName)."  
}
Write-Host ""