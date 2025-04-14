<#
 .SYNOPSIS
    Creates release images with a particular release version in
    production ACR from the tested development version.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    Releases images with a given version number.

 .PARAMETER BuildRegistry
    The name of the source registry where development image is present.
 .PARAMETER BuildSubscription
    The subscription where the build registry is located
 .PARAMETER BuildNamespace
    The namespace in the build registry (optional)

 .PARAMETER ReleaseRegistry
    The name of the destination registry where release images will
    be created.
 .PARAMETER ReleaseSubscription
    The subscription of the release registry is different than build
    registry subscription
 .PARAMETER ResourceGroupName
    The name of the resource group to create if release registry does not
    exist (Optional).
 .PARAMETER ResourceGroupLocation
    The location of the resource group to create (Optional).

 .PARAMETER ReleaseVersion
    The build version for the development image that is being released.
 .PARAMETER RepositoryPattern
    A pattern to filter the repositories through.

 .PARAMETER IsLatest
    Release as latest image
 .PARAMETER IsMajorUpdate
    Release as major update
 .PARAMETER PreviewVersion
    Release only as -ReleaseVersion with -preview{PreviewVersion} appended
    to the tag. This will not update rolling tags like latest and major
    version and allow customers to test the releae image ahead of release.
 .PARAMETER RemoveNamespaceOnRelease
    Remove namespace (e.g. public) on release.
#>

Param(
    [string] $BuildRegistry = "industrialiot",
    [string] $BuildSubscription = "IOT_GERMANY",
    [string] $BuildNamespace = $null,
    [string] $ReleaseRegistry = "industrialiotprod",
    [string] $ReleaseSubscription = "IOT_GERMANY",
    [string] $ResourceGroupName = $null,
    [string] $ResourceGroupLocation = $null,
    [Parameter(Mandatory = $true)] [string] $ReleaseVersion,
    [string] $RepositoryPattern = $null,
    [switch] $IsLatest,
    [switch] $IsMajorUpdate,
    [string] $PreviewVersion,
    [switch] $RemoveNamespaceOnRelease
)

if (![string]::IsNullOrEmpty($script:ResourceGroupName)) {
    # check if release registry exists and if not create it
    $argumentList = @("acr", "show", "--name", $script:ReleaseRegistry,
        "--subscription", $script:ReleaseSubscription)
    $registry = & "az" @argumentList | ConvertFrom-Json
    if (!$registry) {
        # create registry - check if group exists and if not create it.
        $argumentList = @("group", "show", "-g", $script:ResourceGroupName,
            "--subscription", $script:ReleaseSubscription)
        $group = & "az" @argumentList 2>$null | ConvertFrom-Json
        if (!$group) {
            if ([string]::IsNullOrEmpty($script:ResourceGroupLocation)) {
                throw "Need a resource group location to create the resource group."
            }
            $argumentList = @("group", "create", "-g", $script:ResourceGroupName, `
                "-l", $script:ResourceGroupLocation,
                "--subscription", $script:ReleaseSubscription)
            $group = & "az" @argumentList | ConvertFrom-Json
            if ($LastExitCode -ne 0) {
                throw "az $($argumentList) failed with $($LastExitCode)."
            }
            Write-Host "Created new Resource group $ResourceGroupName."
        }
        if ([string]::IsNullOrEmpty($script:ResourceGroupLocation)) {
            $script:ResourceGroupLocation = $group.location
        }

        $argumentList = @("acr", "create", "-g", $script:ResourceGroupName, "-n", `
            $script:ReleaseRegistry, "-l", $script:ResourceGroupLocation, `
            "--sku", "Basic", "--admin-enabled", "true",
            "--subscription", $script:ReleaseSubscription)
        $registry = & "az" @argumentList | ConvertFrom-Json
        if ($LastExitCode -ne 0) {
            throw "az $($argumentList) failed with $($LastExitCode)."
        }
Write-Host "Created container registry $($registry.name) in $script:ResourceGroupName."
    }
}

# Set build subscription if provided
if (![string]::IsNullOrEmpty($script:BuildSubscription)) {
    Write-Debug "Setting subscription to $($script:BuildSubscription)"
    $argumentList = @("account", "set",
        "--subscription", $script:BuildSubscription, "-ojson")
    & "az" @argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# Get build repositories
$argumentList = @("acr", "repository", "list",
    "--name", $script:BuildRegistry, "-ojson",
    "--subscription", $script:BuildSubscription)
$result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$BuildRepositories = $result | ConvertFrom-Json

# In each repo - check whether a release tag exists
$jobs = @()
foreach ($Repository in $BuildRepositories) {

    $BuildTag = "$($Repository):$($script:ReleaseVersion)"
    if (![string]::IsNullOrEmpty($script:BuildNamespace) -and `
        !$Repository.StartsWith($script:BuildNamespace)) {
        continue
    }
    if (![string]::IsNullOrEmpty($script:RepositoryPattern) -and `
        ($Repository -notlike $script:RepositoryPattern)) {
        continue
    }

    if ($Repository -like "*/opc-plc") {
        # do not touch/break the opc plc releases
        continue
    }

    # see if build tag exists
    $argumentList = @("acr", "repository", "show",
        "--name", $script:BuildRegistry,
        "--subscription", $script:BuildSubscription,
        "-t", $BuildTag,
        "-ojson"
    )
    $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
    if ($LastExitCode -ne 0) {
        Write-Host "Image $BuildTag not found..."
        continue
    }
    $Image = $result | ConvertFrom-Json
    if ([string]::IsNullOrEmpty($Image.digest)) {
        Write-Host "Image $BuildTag not found..."
        continue
    }

    $ReleaseTags = @()
    if ($script:PreviewVersion) {
        if ($script:IsMajorUpdate.IsPresent -or $script:IsLatest.IsPresent) {
            throw "IsMajorUpdate and IsLatest is not allowed when PreviewVersion is specified."
        }
        $versionTag = "$($script:ReleaseVersion)-preview$($script:PreviewVersion)"
        $ReleaseTags += $versionTag
    }
    else {
        if ($script:IsLatest.IsPresent) {
            $ReleaseTags += "latest"
        }

        # Example: if release version is 2.8.1, then base image tags are "2", "2.8", "2.8.1"
        $versionParts = $script:ReleaseVersion.Split('-')[0].Split('.')
        if ($versionParts.Count -gt 0) {
            $versionTag = $versionParts[0]
            if ($script:IsMajorUpdate.IsPresent -or $script:IsLatest.IsPresent) {
                $ReleaseTags += $versionTag
            }
            for ($i = 1; $i -lt ($versionParts.Count); $i++) {
                $versionTag = ("$($versionTag).{0}" -f $versionParts[$i])
                $ReleaseTags += $versionTag
            }
        }
    }

    # Create acr command line
    # --force is needed to replace existing tags like "latest" with new images
    $argumentList = @("acr", "import", "-ojson", "--force",
        "--name", $script:ReleaseRegistry,
        "--source", $BuildTag,
        "--registry", $script:BuildRegistry
    )
    # set release subscription
    if (![string]::IsNullOrEmpty($script:ReleaseSubscription)) {
        $argumentList += "--subscription"
        $argumentList += $script:ReleaseSubscription
    }
    else {
        $argumentList += "--subscription"
        $argumentList += $script:BuildSubscription
    }

    # add the output / release image tags
    if ($script:RemoveNamespaceOnRelease.IsPresent `
        -and (!$Repository.StartsWith("iot/")) `
        -and (!$Repository.StartsWith("iotedge/"))) {
        $TargetRepository = $Repository.Substring($Repository.IndexOf('/') + 1)
    }
    else {
        $TargetRepository = $Repository
    }
    foreach ($ReleaseTag in $ReleaseTags) {
        $argumentList += "--image"
        $argumentList += "$($TargetRepository):$($ReleaseTag)"
    }

    $FullImageName = "$($script:BuildRegistry).azurecr.io/$($BuildTag)"
    $ConsoleOutput = "Copying $FullImageName $($Image.digest) with tags '$($ReleaseTags -join ", ")' to release $script:ReleaseRegistry"
    Write-Host "Starting Job $ConsoleOutput..."
    $jobs += Start-Job -Name $FullImageName -ArgumentList @($argumentList, $ConsoleOutput) -ScriptBlock {
        $argumentList = $args[0]
        $ConsoleOutput = $args[1]
        Write-Host "$($ConsoleOutput)..."
        & az @argumentList 2>&1 | ForEach-Object { "$_" }
        if ($LastExitCode -ne 0) {
            Write-Warning "$($ConsoleOutput) failed with $($LastExitCode) - 2nd attempt..."
            & "az" @argumentList 2>&1 | ForEach-Object { "$_" }
            if ($LastExitCode -ne 0) {
                throw "Error: $($ConsoleOutput) - 2nd attempt failed with $($LastExitCode)."
            }
        }
        Write-Host "$($ConsoleOutput) completed."
    }
}

# Wait for copy jobs to finish for this repo.
if ($jobs.Count -ne 0) {
    Write-Host "Waiting for copy jobs to finish for $($script:ReleaseRegistry)."
    # Wait until all jobs are completed
    Receive-Job -Job $jobs -WriteEvents -Wait | Out-Host
    $jobs | Out-Host
    $jobs | Where-Object { $_.State -ne "Completed" } | ForEach-Object {
        throw "ERROR: Copying $($_.Name). resulted in $($_.State)."
    }
}
Write-Host "All copy jobs completed successfully for $($script:ReleaseRegistry)."