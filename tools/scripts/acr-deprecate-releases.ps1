<#
 .SYNOPSIS
    Adds lifecycle annotations to all images int the registry that do
    not have any yet.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    It also requires the oras cli to be installed and available in the
    path.

 .PARAMETER Registry
    The name of the registry where the images need to be annotated.
 .PARAMETER Subscription
    The subscription of the registry to use - otherwise uses default
 #>

Param(
    [string] $Registry = "industrialiotprod",
    [string] $Subscription = "IOT_GERMANY",
    [Parameter(Mandatory = $true)] [string] $ReleaseVersion,
    [String] $Repository
)

# Set build subscription if provided
if (![string]::IsNullOrEmpty($script:Subscription)) {
    Write-Debug "Setting subscription to $($script:Subscription)"
    $argumentList = @("account", "set",
        "--subscription", $script:Subscription, "-ojson")
    & "az" @argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# get build registry credentials
$argumentList = @("acr", "credential", "show", "--name", $script:Registry, "-ojson")
$result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$dockerCredentials = $result | ConvertFrom-Json
$dockerUser = $dockerCredentials.username
$dockerPassword = $dockerCredentials.passwords[0].value
$dockerServer = "$($script:Registry).azurecr.io"

$argumentList = @("login", $dockerServer,
    "--username", $dockerUser, "--password", $dockerPassword)
$result = (& "oras" @argumentList 2>&1 | ForEach-Object { "$_" })
if ($LastExitCode -ne 0) {
    throw "oras $($argumentList) failed with $($LastExitCode)."
}

# Get build repositories
$argumentList = @("acr", "repository", "list",
    "--name", $script:Registry, "-ojson",
    "--subscription", $script:Subscription)
$result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$repositories = $result | ConvertFrom-Json

$ReleaseTags = @()
if (![string]::IsNullOrEmpty($script:ReleaseVersion)) {
    $ReleaseTags += "latest"
    # Example: if release version is 2.8.1, then base image tags are "2", "2.8", "2.8.1"
    $versionParts = $script:ReleaseVersion.Split('-')[0].Split('.')
    if ($versionParts.Count -gt 0) {
        $versionTag = $versionParts[0]
        $ReleaseTags += $versionTag
        for ($i = 1; $i -lt ($versionParts.Count); $i++) {
            $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
            $ReleaseTags += $versionTag
        }
    }
}
# Perform for each repo
foreach ($Repository in $repositories) {
    if (![string]::IsNullOrEmpty($script:Repository) -and $script:Repository -ne $Repository) {
        Write-Host "Skipping repository $($Repository) ..."
        continue
    }
    # Get all tags in the repository
    $argumentList = @("acr", "repository", "show-tags",
        "--name", $script:Registry,
        "--subscription", $script:Subscription,
        "--repository", $Repository,
        "--orderby", "time_asc",
        "--detail",
        "-ojson"
    )
    $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }

    $images = $result | ConvertFrom-Json
    # Remove entries with the same digest and keep the latest one
    $images = $images | Sort-Object -Property createdTime -Descending |
        Select-Object -Unique -Property digest, name, createdTime
    if ($images.Count -eq 0) {
        Write-Host "No images found in $($script:Registry)/$($Repository)."
        continue
    }

    if (![string]::IsNullOrEmpty($script:ReleaseVersion)) {
        # Check if the release version is in the list of tags
        $containsReleaseTag = $images.name -contains $script:ReleaseVersion
    }
    else {
        $containsReleaseTag = $false
    }
    if ($containsReleaseTag) {
        Write-Host "Found release version $($script:ReleaseVersion) in $($script:Registry)/$($Repository)."
    }

    # For each image in the list, update its expiration date to the "createdTime"
    # date of the next image in the list except for the last image
    $imageCount = $images.Count
    for ($i = 0; $i -lt $imageCount; $i++) {
        $digest = $images[$i].digest
        $tag = $images[$i].name
        $imageName = "$($Repository)@$($digest)"
        if ($containsReleaseTag) {
            $isReleaseTag = $ReleaseTags -contains $tag
        }
        else {
            $isReleaseTag = $false
        }
        # Check eol artifact referrers exist
        $argumentList = @("discover", "--artifact-type",
            "application/vnd.microsoft.artifact.lifecycle",
            "$($dockerServer)/$($imageName)",
            "-v", "--output", "json")
        $result = (& "oras" @argumentList 2>&1 | ForEach-Object { "$_" })
        if ($LastExitCode -ne 0) {
            throw "oras $($argumentList) failed with $($LastExitCode)."
        }
        $eol = $result | ConvertFrom-Json
        $eolDate = `
            $eol.manifests.annotations."vnd.microsoft.artifact.lifecycle.end-of-life.date" `
                | Select-Object -Unique
        if ($eolDate.Count -eq 1) {
            if (!$isReleaseTag) {
                # Write-Host "Image $($imageName) ($($tag)) has expiration date $($eolDate | ConvertTo-Json)."
                continue
            }
            # delete the eol artifact if it is not a release tag
            Write-Warning "Removing expiration date $($eolDate | ConvertTo-Json) from released image $($imageName) ($($tag)) ..."
        }
        elseif ($eolDate.Count -ne 0) {
            Write-Warning "$($imageName) ($($tag)) has multiple EOL dates: $($eolDate | ConvertTo-Json)..."
            Write-Host "Resetting image $($imageName) ($($tag)) expiration date ..."
        }
        if ($eol.manifests.Count -gt 0) {
            # delete all eol artifacts and recreate a correct one
            foreach ($manifest in $eol.manifests) {
                $digest = $manifest.digest
                $eolManifest = "$($Repository)@$($digest)"
                Write-Host "   ... Removing EOL referrer $($dockerServer)/$($eolManifest)."
                $argumentList = @("acr", "manifest", "delete",
                    "--registry", $script:Registry,
                    "--subscription", $script:Subscription,
                    "--name", $($eolManifest), "--yes")

                $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
                if ($LastExitCode -ne 0) {
                    throw "az $($argumentList) failed with $($LastExitCode)."
                }
            }
        }
        if ($isReleaseTag) {
            continue
        }
        elseif ($i -eq ($imageCount - 1)) {
            # Set expiration date to 1 month from now for last image
            $expirationDate = (Get-Date).AddMonths(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
        }
        else {
            # Set the expiration date to the created time of the next image
            $nextImage = $images[$i + 1]
            if (![string]::IsNullOrEmpty($nextImage.createdTime)) {
                # parse the date string to a DateTime object and reformat it as string
                $date = [datetime]::Parse($nextImage.createdTime)
                $expirationDate = $date.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
            else {
                $expirationDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        }
        $argumentList = @("attach", "--artifact-type",
            "application/vnd.microsoft.artifact.lifecycle", "--annotation",
            "vnd.microsoft.artifact.lifecycle.end-of-life.date=$($expirationDate)",
            "$($dockerServer)/$($imageName)")
        $result = (& "oras" @argumentList 2>&1 | ForEach-Object { "$_" })
        if ($LastExitCode -ne 0) {
            throw "oras $($argumentList) failed with $($LastExitCode)."
        }
        Write-Host "EOL date for $($imageName) ($($tag)) was set to $($expirationDate)."
    }
}
