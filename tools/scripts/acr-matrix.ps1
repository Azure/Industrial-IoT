<#
 .SYNOPSIS
    Creates the container build matrix from the container.json files in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with an container.json
    file and populates the matrix to create the individual build jobs.

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from.

 .PARAMETER Registry
    The name of the registry

 .PARAMETER Subscription
    The subscription to use - otherwise uses default

 .PARAMETER Build
    If not set the task generates jobs in azure pipeline.

 .PARAMETER Debug
    Build debug and include debugger into images (where applicable)
#>

Param(
    [string] $BuildRoot,
    [string] $Registry,
    [string] $Subscription,
    [switch] $Build,
    [switch] $Debug
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"
}

$acrMatrix = @{}

# Traverse from build root and find all container.json metadata files to acr matrix
Get-ChildItem $BuildRoot -Recurse -Include "container.json" `
    | ForEach-Object {

    # Get root
    $dockerFolder = $_.DirectoryName.Replace($BuildRoot, "").Substring(1)
    $metadata = Get-Content -Raw -Path $_.FullName | ConvertFrom-Json

    try {
        $jobName = "$($metadata.name)"
        if (![string]::IsNullOrEmpty($metadata.tag)) {
            $jobName = "$($jobName)/$($metadata.tag)"
        }
        if (![string]::IsNullOrEmpty($jobName)) {
            $acrMatrix.Add($jobName, @{ "dockerFolder" = $dockerFolder })
        }
    }
    catch {
        # continue to next
    }
}

if ($Build.IsPresent) {
    $acrMatrix.Values | ForEach-Object {
        & (Join-Path $PSScriptRoot "acr-build.ps1") -Path $_.dockerFolder `
            -Debug:$Debug -Registry $Registry -Subscription $Subscription
    }
}
else {
    # Set pipeline variable
    Write-Host ("##vso[task.setVariable variable=acrMatrix;isOutput=true] {0}" `
        -f ($acrMatrix | ConvertTo-Json -Compress))
}
