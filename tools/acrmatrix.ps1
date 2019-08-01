<#
 .SYNOPSIS
    Creates the container build matrix from the mcr.json files in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with an mcr.json
    file and populates the matrix to create the individual build jobs.

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from

 .PARAMETER Pipelined
    The task generates jobs in azure pipeline.  Default is $true.
#>

Param(
    [string] $BuildRoot = $null,
    [switch] $Pipelined = $true
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = Split-Path (Split-Path $script:MyInvocation.MyCommand.Path)
}

$acrMatrix = @{}

# Traverse from build root and find all mcr.json metadata files to acr matrix
Get-ChildItem $BuildRoot -Recurse `
    | Where-Object Name -eq "mcr.json" `
    | ForEach-Object {

    # Get root
    $dockerFolder = $_.DirectoryName.Replace($BuildRoot, "").Substring(1)
    $metadata = Get-Content -Raw -Path $_.FullName | ConvertFrom-Json

    try {
        $jobName = "Build and Push " + $metadata.name
        if (![string]::IsNullOrEmpty($metadata.name)) {
            $acrMatrix.Add($jobName, @{ "dockerFolder" = $dockerFolder })
        }
    }
    catch {
        # continue to next
    }
}

if ($Pipelined -eq $false) {
    $acrMatrix.Values | ForEach-Object {
        & (Join-Path $PSScriptRoot "acrbuild.ps1") -path $_.dockerFolder
    }
}
else {
    # Set pipeline variable
    Write-Host ("##vso[task.setVariable variable=acrMatrix;isOutput=true] {0}" `
        -f ($acrMatrix | ConvertTo-Json -Compress))
}
