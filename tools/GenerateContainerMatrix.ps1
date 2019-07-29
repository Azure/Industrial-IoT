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

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from
#>

Param(
    [string]
    $BuildRoot = $null
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = Split-Path (Split-Path $script:MyInvocation.MyCommand.Path)
}

# find the top most folder with .dockerignore in it
Function GetTopMostDockerIgnoreFolder() {
    param(
        $startDir
    ) 
    $cur = $startDir
    while ($True) {
        if ([string]::IsNullOrEmpty($cur)) {
            break
        }
        $test = Join-Path $cur ".dockerignore"
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


# get and set build information from git or content
$sourceVersion = $env.BUILD_SOURCEVERSION
# Try get current tag
try {
    $output = cmd /c "tag --points-at $sourceVersion" 2`>`&1
    $sourceTag = ("{0}" -f $output);
}
catch {
    Get-ChildItem $BuildRoot -Recurse `
        | Where-Object Name -eq "version.props" `
        | ForEach-Object {

        if ([string]::IsNullOrEmpty($sourceTag)) {
            # set version number from version.props
            [xml] $props=Get-Content -Path "version.props"
            $sourceTag=$props.Project.PropertyGroup.VersionPrefix
        }
    }
}

# Try get branch name
try {
    $output = cmd /c "git rev-parse --abbrev-ref HEAD" 2`>`&1
    $branchName = ("{0}" -f $output);
    # any build other than from master branch is a developer build.
    $isDeveloperBuild = $branchName -ne "master"
}
catch {
    # set master, but treat as developer initiated build
    $branchName = "master"
    $isDeveloperBuild = true
}

if (?$isDeveloperBuild) {
    $imageTag
}

$buildMatrix = @{}
$manifestMatrix = @{}

# Traverse from build root and find all mcr.json metadata files to build build context
Get-ChildItem $BuildRoot -Recurse `
    | Where-Object Name -eq "mcr.json" `
    | ForEach-Object {

        # Get root
        $dockerFolder = $_.DirectoryName
        $buildContext = GetTopMostDockerIgnoreFolder -startDir $dockerFolder
        $metadata = Get-Content -Raw -Path $_.FullName | ConvertFrom-Json

        # Get all dockerfile
        Get-ChildItem $dockerFolder -Recurse `
        | Where-Object Name -eq "Dockerfile" `
        | ForEach-Object {

            $platformFolder = $_.DirectoryName.Replace($dockerFolder, "").Substring(1)
            $dockerfile = $_.FullName.Replace($dockerFolder, "").Substring(1)
            $platform = $platformFolder.Replace("\", "/")

            # Create name for the job
            $jobName = $metadata.name + "_" + `
                $platformFolder.Replace(" ", "_").Replace("\", "_")

            $job = $metadata.PsObject.Copy()
            $job | Add-Member -NotePropertyName "platform" `
                -NotePropertyValue $platform
            $job | Add-Member -NotePropertyName "buildContext" `
                -NotePropertyValue $buildContext
            $job | Add-Member -NotePropertyName "dockerfile" `
                -NotePropertyValue $dockerfile
            $buildMatrix.Add($jobName, $job)
        }

        # Generate yaml manifest for all

    }


# Output to pipeline task
Write-Host "##vso[task.setVariable variable=buildMatrix;isOutput=true]" `
    + ($buildMatrix | ConvertTo-Json)
Write-Host "##vso[task.setVariable variable=manifestMatrix;isOutput=true]" `
    + ($manifestMatrix | ConvertTo-Json)