<#
 .SYNOPSIS
    Builds docker images from definition files in folder or the entire tree

 .DESCRIPTION
    The script traverses the build root to find all folders with an container.json
    file builds each one

 .PARAMETER Path
    The root folder to start traversing the repository from (Optional).

 .PARAMETER Debug
    Whether to build debug images.
#>

Param(
    [string] $Path = $null,
    [switch] $Debug
)

$BuildRoot = & (Join-Path $PSScriptRoot "get-root.ps1") -fileName "*.sln"

if ([string]::IsNullOrEmpty($Path)) {
    $Path = $BuildRoot
}

# Traverse from build root and find all container.json metadata files and build
Get-ChildItem $Path -Recurse -Include "container.json" `
    | ForEach-Object {

    # Get root
    $dockerFolder = $_.DirectoryName.Replace($BuildRoot, "")
    if ([string]::IsNullOrEmpty($dockerFolder)) {
        $dockerFolder = "."
    }
    else {
        $dockerFolder = $dockerFolder.Substring(1)
    }
    
    $metadata = Get-Content -Raw -Path (join-path $_.DirectoryName "container.json") `
        | ConvertFrom-Json
    & (Join-Path $PSScriptRoot "docker-build.ps1") `
        -ImageName $metadata.name -Path $dockerFolder -Debug:$Debug
}
