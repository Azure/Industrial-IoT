<#
 .SYNOPSIS
    Creates the build matrix from the specified file names in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with a
    matching a file and populates the matrix to create the individual 
    jobs.

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from

 .PARAMETER FileName
    File pattern to find

 .PARAMETER JobPrefix
    Name prefix for job
#>

Param(
    [string] $BuildRoot = $null,
    [string] $JobPrefix = "",
    [string] $FileName = $null
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = Split-Path (Split-Path $script:MyInvocation.MyCommand.Path)
}

if ([string]::IsNullOrEmpty($FileName)) {
    $FileName = "project.props"
}

if (![string]::IsNullOrEmpty($JobPrefix)) {
    $JobPrefix = "$($JobPrefix)-"
}

$agents = @{
    linux = "Hosted Ubuntu 1604"
    windows = "Hosted Windows 2019 with VS2019"
    mac = "Hosted macOS"
}

$jobMatrix = @{}

# Traverse from build root and find all project.props files for test matrix
Get-ChildItem $BuildRoot -Recurse `
    | Where-Object Name -like $FileName `
    | ForEach-Object {

    $folder = $_.DirectoryName.Replace($BuildRoot, "").TrimStart("/").TrimStart("\\")
    $file = $_.FullName.Replace($BuildRoot, "").TrimStart("/").TrimStart("\\")
    $postFix = $folder
    if ([string]::IsNullOrEmpty($postFix)) {
        $postFix = $file
    }
    $postFix = $postFix.Replace("\", "-").Replace("/", "-")

    $agents.keys | ForEach-Object {
        $jobName = "$($JobPrefix)$($postFix)-$($_)"
        $jobMatrix.Add($jobName, @{ 
            "poolName" = $agents.Item($_)
            "folder" = $folder 
            "file" = $file 
        })
    }
}

# Set pipeline variable
Write-Host ("##vso[task.setVariable variable=jobMatrix;isOutput=true] {0}" `
    -f ($jobMatrix | ConvertTo-Json -Compress))
