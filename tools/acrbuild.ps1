<#
 .SYNOPSIS
    Creates the container build matrix from the mcr.json files in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with an mcr.json
    file.  
    
    In each folder with mcr.json it finds the dockerfiles for the platform 
    specific build.   
    
    It then traverses back up to the closest .dockerignore file. This folder 
    becomes the context of the build.
    
    Finally it stores the container build matrix and the manifest build matrix and 
    echos each as VSO variables to create the individual build jobs.

 .PARAMETER dockerFolder
    The folder to build the docker files from

 .PARAMETER subscription
    The Subscription where the ACR instance resides

 .PARAMETER resourceGroup
    The Resource group of the ACR instance

 .PARAMETER registry
    The name of the registry in ACR

 .PARAMETER user
    The registry user name to log on with

 .PARAMETER password
    The registry credentials
#>

Param(
    [string] $dockerFolder = $null,
    [string] $registry = $null,
    [string] $resourceGroup = $null,
    [string] $subscription = $null,
    [string] $user = $null,
    [string] $password = $null
)

# find the top most folder with .dockerignore in it
Function GetTopMostFolder() {
    param(
        [string] $startDir,
        [string] $fileName
    ) 
    $cur = $startDir
    while ($True) {
        if ([string]::IsNullOrEmpty($cur)) {
            break
        }
        $test = Join-Path $cur $fileName
        if (Test-Path $test) {
            $found = $cur
        }
        elseif (![string]::IsNullOrEmpty($found)) {
            break
        }
        $cur = Split-Path $cur
    }
    if (![string]::IsNullOrEmpty($found)) {
        return $found
    }
    return $startDir
}

# Do some argument checking
if ([string]::IsNullOrEmpty($dockerFolder)) {
    throw "No folder specified - exiting prematurely."
}
if ([string]::IsNullOrEmpty($registry)) {
    $registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($registry)) {
        Write-Warning "No registry specified - exiting prematurely."
        Exit 0
    }
}
if ([string]::IsNullOrEmpty($subscription)) {
    $subscription = $env.BUILD_SUBSCRIPTION
}
if ([string]::IsNullOrEmpty($resourceGroup)) {
    $resourceGroup = $env.BUILD_RESOURCEGROUP
}

# Get build context
$buildContext = GetTopMostFolder -startDir $dockerFolder -fileName ".dockerignore"
$metadata = Get-Content -Raw -Path (join-path $dockerFolder "mcr.json") `
    | ConvertFrom-Json

# get and set build information from git or content
$sourceVersion = $env.BUILD_SOURCEVERSION

# Try get current tag
try {
    $sourceTag = ("{0}" -f (cmd /c "git tag --points-at $sourceVersion" 2`>`&1));
}
catch {
    $sourceTag = $null
}
if ([string]::IsNullOrEmpty($sourceTag)) {
    try {
        # set version number from first encountered version.props
        [xml] $props=Get-Content -Path (join-path `
            GetTopMostFolder -startDir $dockerFolder -fileName "version.props" `
                "version.props")
        $sourceTag=$props.Project.PropertyGroup.VersionPrefix
    }
    catch {
        $sourceTag = $null
    }
    if ([string]::IsNullOrEmpty($sourceTag)) {
        $sourceTag = "latest"
    }
}

# Try get branch name
try {
    $branchName = ("{0}" -f (cmd /c "git rev-parse --abbrev-ref HEAD" 2`>`&1));
    # any build other than from master branch is a developer build.
    $isDeveloperBuild = $branchName -ne "master"
}
catch {
    # set master, but treat as developer initiated build
    $branchName = "master"
    $isDeveloperBuild = true
}

# Set image name and namespace in acr based on branch and source tag
$imageName = $metadata.name
$namespace = "public/"
if (?$isDeveloperBuild) {
    $namespace = ("internal/{0}/" -f $branchName)
}

# Create manifest file
$tags = @()
$versionParts = $sourceTag.Split('.')
if ($versionParts.Count -gt 0) {
    $versionTag = $versionParts[0]
    $tags += $versionTag
    for ($i = 1; $i -lt $versionParts.Count; $i++) {
        $versionTag = ("$(versionTag).{0}" -f $versionParts[$i])
        $tags += $versionTag
    }
    $tagList = ("'{0}'" -f ($tags -join "', '"))
}

$manifest = @" 
image: $(registry).azurecr.io/$(namespace)$(imageName):latest
tags: [$(tagList)]
manifests:
"@

# Build all dockerfiles
$jobs = @()
Get-ChildItem $dockerFolder -Recurse `
    | Where-Object Name -eq "Dockerfile" `
    | ForEach-Object {

    $platformFolder = $_.DirectoryName.Replace($dockerFolder, "").Substring(1)
    $dockerfile = $_.FullName.Replace($dockerFolder, "").Substring(1)
    $platform = $platformFolder.Replace("\", "/")
    if ($platform -eq "linux/arm32v7") {
        # Backcompat
        $platform = "linux/arm/v7"
    }
    $platformTag = $platform.Replace("/", "-")
    if ($platformTag -eq "linux-arm-v7") {
        # Backcompat
        $platformTag = "linux-arm32v7"
    }

    $image = "$(namespace)$(imageName):$(sourceTag)-$(platformTag)"

    # Create acr command line - TODO: Consider az powershell module?
    $cmd = "az acr build --verbose"
    $cmd += " --registry $(registry)"
    if (![string]::IsNullOrEmpty($subscription)) {
        $cmd += " --subscription $(subscription)"
    }
    if (![string]::IsNullOrEmpty($resourceGroup)) {
        $cmd += " --resource-group $(resourceGroup)"
    }
    $cmd += " --platform $(platform)"
    $cmd += " --file $(dockerfile)"
    $cmd += " --image $(image)"
    $cmd += " $(buildContext)"

    Write-Host "Start building $(image)"
    $jobs += Start-Job -Name $platform -ArgumentList @($cmd ) -ScriptBlock {
        Write-Host "Calling {0}" -f $args[0]
        cmd /c $args[0]
    }

    # Append image with platform information to manifest
    $os = ""
    $osArchArr = $platform.Split('/')
    if ($osArchArr.Count -gt 1) {
        $os = ("os: {0}" -f $osArchArr[0])
        $arch = ("architecture: {0}" -f $osArchArr[1])
        $variant = ""
        if ($osArchArr.Count -gt 2) {
            $variant = ("variant: {0}" -f $osArchArr[2])
        }
    }
    if (![string]::IsNullOrEmpty($os)) {
        $manifest +=
@" 
  - 
    image: $(registry).azurecr.io/$(image)
    platform: 
      $(os)
      $(architecture)
      $(variant)
"@
    }
}

# Wait until all jobs are completed
$results = Receive-Job -Job $jobs -Wait -WriteJobInResults
for ($i = 0; $i -le $results.Count; $i += 2) {
    $job = $results[$i]
    Write-Host ("Build result for {0}" -f $job.Name)
    Write-Host ("{0}" -f $results[$i + 1])
    if ($job.State -ne "Completed") {
        throw "ERROR: Building $(job.Name). resulted in $(job.State)."
    }
}

# Build manifest using the manifest tool
if ([string]::IsNullOrEmpty($user)) {
    Write-Warning "No User - Exiting before pushing manifest..."
    Exit 0        
}
if ([string]::IsNullOrEmpty($password)) {
    $password = $env.REGISTRY_PASSWORD
    if ([string]::IsNullOrEmpty($password)) {
        Write-Warning "No Password - Exiting before pushing manifest..."
        Exit 0        
    }
}
$manifest | Out-File -Encoding ascii -FilePath "manifest.yml"
try {
    Write-Host "Pushing manifest"
    cmd /c "--username $(user) --password $(password) push from-spec manifest.yml"
    Remove-Object "manifest.yml"
}
catch {
    if (?$isDeveloperBuild) {
        Write-Error "Could not push manifest"
    }
    else {
        throw $_
    }
}

