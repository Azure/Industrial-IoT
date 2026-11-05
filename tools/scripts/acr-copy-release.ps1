<#
 .SYNOPSIS
    Creates release images with a particular release version in
    production ACR from the tested development version.

 .DESCRIPTION
    The script requires az to be installed and already logged on to a
    subscription.  This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

    Releases images with a given version number.

    By default the script releases the images that the "Push multi-arch
    images to GHCR" job in .github/workflows/ci.yml publishes to
    ghcr.io/azure, e.g. ghcr.io/azure/iotedge/opc-publisher:2.9.19.  For
    each repository it reads the digest of the image tagged with the release
    version and imports that digest into the release registry in the public
    namespace, e.g. public/iotedge/opc-publisher:2.9.19.  The import
    includes all platforms of a multi-arch image.  Importing the digest
    instead of the tag releases the image that was found, even if the tag
    moves before the import runs.

    The script reads a build registry given as a login server without
    credentials, so on GHCR the packages must be public.  It does not
    distinguish an image that does not exist from one that needs
    credentials, and skips both with a message.  Any other failure, such as
    an unreachable registry, ends the script with an error.  If no image is
    found, the script reports that nothing was copied.  Cosign signatures
    are separate tags (sha256-<digest>.sig) and are not copied.

    To release from an Azure container registry instead, pass its name as
    -BuildRegistry.  The script then releases every repository of that
    registry that has the release version.

 .PARAMETER BuildRegistry
    The registry that holds the images to release, ghcr.io/azure by default.
    A value that contains a dot or slash is a login server with an optional
    namespace, e.g. ghcr.io/azure.  Any other value is the name of an Azure
    container registry, e.g. industrialiot.
 .PARAMETER BuildSubscription
    The subscription of the build registry if that is an Azure container
    registry.  For a build registry given as a login server the script works
    in ReleaseSubscription and uses this value only if ReleaseSubscription
    is empty.
 .PARAMETER BuildNamespace
    The namespace in the build registry (optional)
 .PARAMETER BuildRepositories
    The repositories in the build registry to release (optional).  The
    default for an Azure container registry is all of its repositories.  The
    script does not list the repositories of a build registry given as a
    login server, because registries such as GHCR have no catalog.  It
    releases iotedge/opc-publisher and iot/opc-ua-test-server instead, the
    two images that CI builds.  Pass this parameter to release another
    repository.

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
    Remove namespace (e.g. public) on release.  An image from a build
    registry given as a login server is released in the public namespace,
    e.g. public/iotedge/opc-publisher, unless this is set.
#>

Param(
    [string] $BuildRegistry = "ghcr.io/azure",
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
    [switch] $RemoveNamespaceOnRelease,
    [string[]] $BuildRepositories = $null
)

# A build registry name without a dot or slash is an Azure container registry.
# Anything else is the login server and optional namespace of another registry.
$IsBuildAcr = $script:BuildRegistry -notmatch '[./]'

# Gets the digest of an image, e.g. ghcr.io/azure/iotedge/opc-publisher:2.9.19,
# without sending credentials.  Returns $null if the image does not exist or is
# not readable anonymously, and the caller cannot tell which.  Any other failure
# throws, so that a network error is not taken for a missing image.
function Get-ImageDigest {
    Param(
        [Parameter(Mandatory = $true)] [string] $Image
    )
    $ErrorActionPreference = "Stop"
    if ($Image -notmatch '^(?<server>[^/]+)/(?<name>.+):(?<tag>[^:/]+)$') {
        throw "$Image is not a fully qualified image name with a tag."
    }
    $manifestUri = "https://$($Matches.server)/v2/$($Matches.name)/manifests/$($Matches.tag)"
    $accept = @(
        "application/vnd.oci.image.index.v1+json",
        "application/vnd.oci.image.manifest.v1+json",
        "application/vnd.docker.distribution.manifest.list.v2+json",
        "application/vnd.docker.distribution.manifest.v2+json"
    ) -join ", "

    Add-Type -AssemblyName System.Net.Http
    $client = [System.Net.Http.HttpClient]::new()
    try {
        $token = $null
        for ($attempt = 1; $attempt -le 2; $attempt++) {
            $request = [System.Net.Http.HttpRequestMessage]::new(
                [System.Net.Http.HttpMethod]::Head, $manifestUri)
            [void] $request.Headers.TryAddWithoutValidation("Accept", $accept)
            if ($token) {
                $request.Headers.Authorization = `
                    [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $token)
            }
            $response = $client.SendAsync($request).GetAwaiter().GetResult()
            if ($response.IsSuccessStatusCode) {
                $digests = $null
                if (!$response.Headers.TryGetValues("Docker-Content-Digest", [ref] $digests)) {
                    throw "The response for $manifestUri has no Docker-Content-Digest header."
                }
                return $digests | Select-Object -First 1
            }

            # An unauthorized response carries the location of the token service
            # that issues the anonymous pull token for the repository.
            $challenge = $response.Headers.WwwAuthenticate `
                | Where-Object { $_.Scheme -eq "Bearer" } | Select-Object -First 1
            if ([int] $response.StatusCode -ne 401 -or !$challenge -or $token) {
                break
            }
            $parameters = @{}
            foreach ($match in [regex]::Matches($challenge.Parameter, '(\w+)="([^"]*)"')) {
                $parameters[$match.Groups[1].Value] = $match.Groups[2].Value
            }
            $tokenUri = "$($parameters["realm"])?service=" `
                + "$([uri]::EscapeDataString($parameters["service"]))" `
                + "&scope=$([uri]::EscapeDataString($parameters["scope"]))"
            $tokenResponse = $client.GetAsync($tokenUri).GetAwaiter().GetResult()
            if (!$tokenResponse.IsSuccessStatusCode) {
                if ((401, 403, 404) -contains [int] $tokenResponse.StatusCode) {
                    # Also the answer for a repository that does not exist
                    return $null
                }
                throw "Request for $tokenUri failed with $([int] $tokenResponse.StatusCode) $($tokenResponse.ReasonPhrase)."
            }
            $tokenJson = $tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() `
                | ConvertFrom-Json
            $token = $tokenJson.token
            if (!$token) {
                $token = $tokenJson.access_token
            }
        }
        if ((401, 403, 404) -contains [int] $response.StatusCode) {
            return $null
        }
        throw "Request for $manifestUri failed with $([int] $response.StatusCode) $($response.ReasonPhrase)."
    }
    finally {
        $client.Dispose()
    }
}

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
            Write-Host "Created new Resource group $ResourceGroupName." -ForegroundColor Green
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
Write-Host "Created container registry $($registry.name) in $script:ResourceGroupName." `
    -ForegroundColor Green
    }
}

# Work in the subscription of the build registry if that is an Azure container
# registry, otherwise in the release one if it is set.
$AccountSubscription = $script:BuildSubscription
if (!$IsBuildAcr -and ![string]::IsNullOrEmpty($script:ReleaseSubscription)) {
    $AccountSubscription = $script:ReleaseSubscription
}
if (![string]::IsNullOrEmpty($AccountSubscription)) {
    Write-Debug "Setting subscription to $($AccountSubscription)"
    $argumentList = @("account", "set",
        "--subscription", $AccountSubscription, "-ojson")
    & "az" @argumentList 2>&1 | ForEach-Object { Write-Host "$_" }
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
}

# Get build repositories
if ($script:BuildRepositories) {
    $BuildRepositories = $script:BuildRepositories
}
elseif ($IsBuildAcr) {
    $argumentList = @("acr", "repository", "list",
        "--name", $script:BuildRegistry, "-ojson",
        "--subscription", $script:BuildSubscription)
    $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
    if ($LastExitCode -ne 0) {
        throw "az $($argumentList) failed with $($LastExitCode)."
    }
    $BuildRepositories = $result | ConvertFrom-Json
}
else {
    # A build registry given as a login server is not listed, because registries
    # such as GHCR have no catalog.  These are the ContainerRepository values of
    # the projects that ci.yml pushes to GHCR.  Add a repository here when CI
    # starts to publish another image.
    $BuildRepositories = @("iotedge/opc-publisher", "iot/opc-ua-test-server")
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

# Only publish metadata to industialiotprod releaseRegistry
if ($script:ReleaseRegistry -ne "industrialiotprod") {
    Write-Host "Skipping publishing connector metadata to $($script:ReleaseRegistry)." `
        -ForegroundColor Yellow
}
else {
    # Check oras tool is installed
    & "oras" version 2>$null
    if ($LastExitCode -ne 0) {
        Write-Host "The 'oras' tool is not installed." -ForegroundColor Yellow
        Write-Host "Install it from https://github.com/oras-project/oras." -ForegroundColor Yellow
        Write-Host "Skipping publishing connector metadata." -ForegroundColor Yellow
    }
    else {
        # Find the root path which contains the .git folder
        $script:ScriptPath = $MyInvocation.MyCommand.Path
        $script:ScriptDir = Split-Path -Path $script:ScriptPath -Parent
        $RootDir = $script:ScriptDir
        while (!(Test-Path -Path (Join-Path -Path $RootDir -ChildPath ".git"))) {
            $parent = Split-Path -Path $RootDir -Parent
            if ($parent -eq $RootDir) {
                throw "Could not find .git folder in any parent folder of $($script:ScriptDir)."
            }
            $RootDir = $parent
        }
        $metadataFileName = "connector-metadata.json"
        $connectorRepositoryName = "public/iotedge/opc-publisher"
        $connectorMetadataFilePath = "$($RootDir)/deploy/kubernetes/iotops/opc-publisher-connector-metadata.json"
        if (!(Test-Path -Path $connectorMetadataFilePath)) {
            Write-Host "Connector metadata file $connectorMetadataFilePath not found." -ForegroundColor Yellow
            Write-Host "Skipping publishing." -ForegroundColor Yellow
        }
        else {
            # get build registry credentials
            $argumentList = @("acr", "credential", "show", "--name", $script:ReleaseRegistry, "-ojson")
            $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
            if ($LastExitCode -ne 0) {
                throw "az $($argumentList) failed with $($LastExitCode)."
            }
            $dockerCredentials = $result | ConvertFrom-Json

            $dockerUser = $dockerCredentials.username
            $dockerPassword = $dockerCredentials.passwords[0].value
            $dockerServer = "$($script:ReleaseRegistry).azurecr.io"

            try {
                # create an empty file to make sure oras push works even if no config is provided
                $emptyFile = "null"
                if (Test-Path -Path $emptyFile) {
                    Remove-Item $emptyFile -Force
                }
                if (Test-Path -Path $metadataFileName) {
                    Remove-Item $metadataFileName -Force
                }
                Set-Content -Path $emptyFile -Value ""

                # login to release registry
                $argumentList = @("login", $dockerServer,
                    "--username", $dockerUser, "--password", $dockerPassword)
                $result = (& "oras" @argumentList 2>&1 | ForEach-Object { "$_" })
                if ($LastExitCode -ne 0) {
                    throw "oras $($argumentList) failed with $($LastExitCode)."
                }
                # Publish the connector manifest along side publisher image
                foreach ($ReleaseTag in $ReleaseTags) {
                    # Create a temp file and copy the metadata file to it with the word "latest" replaced
                    # with the actual release tag. This ensures that the metadata always points to the correct
                    # publisher image tag.  Also replace the ""version": "2.9.0" with the actual release version.
                    # This ensures that the metadata always points to the correct publisher image version."
                    $tag = '"tag": "' + $ReleaseTag + '"'
                    $version = '"version": "' + $script:ReleaseVersion + '"'
                    Write-Host "Creating temp file $metadataFileName with tag $tag and version $version."
                    # Replace tag and version in the metadata file
                    (Get-Content $connectorMetadataFilePath) `
                        | ForEach-Object { $_ -replace '"tag": "latest"', $tag } `
                        | ForEach-Object { $_ -replace '"version": "2.9.0"', $version } `
                        | Set-Content $metadataFileName

                    # Push the metadata file as a config file to the publisher image with suffix -metadata
                    $FullImageName = "$($dockerServer)/$($connectorRepositoryName):$($ReleaseTag)-metadata"
                    $argumentList = @(
                        "push",
                        "--config",
                        "$($emptyFile):application/vnd.microsoft.akri-connector.v1+json"
                        $FullImageName,
                        "$($metadataFileName):application/json"
                    )
                    $result = (& "oras" @argumentList 2>&1 | ForEach-Object { "$_" })

                    if ($LastExitCode -ne 0) {
                        throw "oras $($argumentList) failed with $($LastExitCode)."
                    }
                    # Remove the temp file
                    Remove-Item $metadataFileName -Force
                    Write-Host "Published connector metadata for opc-publisher to $($FullImageName)." `
                        -ForegroundColor Green
                }
            } finally {
                if (Test-Path -Path $emptyFile) {
                    Remove-Item $emptyFile -Force
                }
                if (Test-Path -Path $metadataFileName) {
                    Remove-Item $metadataFileName -Force
                }
            }
        }
    }
}

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
    if ($IsBuildAcr) {
        $argumentList = @("acr", "repository", "show",
            "--name", $script:BuildRegistry,
            "--subscription", $script:BuildSubscription,
            "-t", $BuildTag,
            "-ojson"
        )
        $result = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" })
        if ($LastExitCode -ne 0) {
            Write-Host "Image $BuildTag not found..." -ForegroundColor Yellow
            continue
        }
        $Image = $result | ConvertFrom-Json
        if ([string]::IsNullOrEmpty($Image.digest)) {
            Write-Host "Image $BuildTag not found..." -ForegroundColor Yellow
            continue
        }
        $ImageDigest = $Image.digest
        $FullImageName = "$($script:BuildRegistry).azurecr.io/$($BuildTag)"

        # Create acr command line
        # --force is needed to replace existing tags like "latest" with new images
        $argumentList = @("acr", "import", "-ojson", "--force",
            "--name", $script:ReleaseRegistry,
            "--source", $BuildTag,
            "--registry", $script:BuildRegistry
        )
    }
    else {
        $SourceRegistry = $script:BuildRegistry.TrimEnd('/')
        $FullImageName = "$($SourceRegistry)/$($BuildTag)"
        $ImageDigest = Get-ImageDigest -Image $FullImageName
        if ([string]::IsNullOrEmpty($ImageDigest)) {
            Write-Host "Image $FullImageName not found or not readable without credentials..." `
                -ForegroundColor Yellow
            continue
        }

        # Import by digest so the image that was found is the image that is released
        $argumentList = @("acr", "import", "-ojson", "--force",
            "--name", $script:ReleaseRegistry,
            "--source", "$($SourceRegistry)/$($Repository)@$($ImageDigest)"
        )
    }
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
    if (!$IsBuildAcr) {
        # CI pushes images without a namespace, and the release registry has them in public.
        if ($script:RemoveNamespaceOnRelease.IsPresent) {
            $TargetRepository = $Repository
        }
        else {
            $TargetRepository = "public/$($Repository)"
        }
    }
    elseif ($script:RemoveNamespaceOnRelease.IsPresent `
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

    $ConsoleOutput = "Copying $FullImageName $ImageDigest with tags '$($ReleaseTags -join ", ")' to release $script:ReleaseRegistry as $TargetRepository"
    Write-Host "Starting Job $ConsoleOutput..." -ForegroundColor Cyan
    $jobs += Start-Job -Name $FullImageName -ArgumentList @($argumentList, $ConsoleOutput) -ScriptBlock {
        $argumentList = $args[0]
        $ConsoleOutput = $args[1]
        Write-Host "$($ConsoleOutput)..."  -ForegroundColor Yellow
        & az @argumentList 2>&1 | ForEach-Object { "$_" }
        if ($LastExitCode -ne 0) {
            Write-Warning "$($ConsoleOutput) failed with $($LastExitCode) - 2nd attempt..."  -ForegroundColor Yellow
            & "az" @argumentList 2>&1 | ForEach-Object { "$_" }
            if ($LastExitCode -ne 0) {
                throw "Error: $($ConsoleOutput) - 2nd attempt failed with $($LastExitCode)."
            }
        }
        Write-Host "$($ConsoleOutput) completed." -ForegroundColor Green
    }
}

# Wait for copy jobs to finish for this repo.
if ($jobs.Count -ne 0) {
    Write-Host "Waiting for copy jobs to finish for $($script:ReleaseRegistry)." -ForegroundColor Cyan
    # Wait until all jobs are completed
    Receive-Job -Job $jobs -WriteEvents -Wait | Out-Host
    $jobs | Out-Host
    $jobs | Where-Object { $_.State -ne "Completed" } | ForEach-Object {
        throw "ERROR: Copying $($_.Name). resulted in $($_.State)."
    }
    Write-Host "All copy jobs completed successfully for $($script:ReleaseRegistry)." -ForegroundColor Green
}
else {
    Write-Host "No images tagged $($script:ReleaseVersion) found in $($script:BuildRegistry) - nothing was copied to $($script:ReleaseRegistry)." `
        -ForegroundColor Yellow
}