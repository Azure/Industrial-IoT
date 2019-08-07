<#
 .SYNOPSIS
    Creates buildjob matrix based on the specified file names in the tree.

 .DESCRIPTION
    The script traverses the build root to find all folders with a matching 
    file and populates the matrix.  The matrix is used to spawn jobs that
    run on multiple different environments for each file.  E.g. build all
    solution files on all platforms, run tests per particular folder of 
    the tree, etc.

 .PARAMETER BuildRoot
    The root folder to start traversing the repository from.

 .PARAMETER FileName
    File patterns to match defining the folders and files in the matrix

 .PARAMETER JobPrefix
    Optional name prefix for each job
#>

Param(
    [string] $BuildRoot = $null,
    [string] $FileName = $null,
    [string] $JobPrefix = ""
)

if ([string]::IsNullOrEmpty($BuildRoot)) {
    $BuildRoot = Split-Path (Split-Path $script:MyInvocation.MyCommand.Path)
}

if ([string]::IsNullOrEmpty($FileName)) {
    $FileName = "Directory.Build.props"
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

# Traverse from build root and find all files to create job matrix
Get-ChildItem $BuildRoot -Recurse `
    | Where-Object Name -like $FileName `
    | ForEach-Object {

    $folder = $_.DirectoryName.Replace($BuildRoot, "").Replace("\", "/").TrimStart("/")
    $file = $_.FullName.Replace($BuildRoot, "").Replace("\", "/").TrimStart("/")
    $postFix = $folder
    if ([string]::IsNullOrEmpty($postFix)) {
        $postFix = $file
    }
    $postFix = $postFix.Replace("/", "-")

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
